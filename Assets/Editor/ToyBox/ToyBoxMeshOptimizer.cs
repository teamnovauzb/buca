using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ToyBoxMeshOptimizer
{
    const string Output="output/geometry-optimization";
    internal static readonly Regex TextKey=new Regex(@"^Text_(.*?)_([0-9]+\.[0-9]+)(?:_Bevel_([0-9]+\.[0-9]+)_Weight_([0-9]+\.[0-9]+))?$");
    static string backup;
    static void Backup(string path)
    {
        string dest=Path.Combine(backup,path);Directory.CreateDirectory(Path.GetDirectoryName(dest));
        if(!File.Exists(dest))File.Copy(path,dest);
    }
    static string Signature(Mesh mesh)
    {
        using(var hash=SHA256.Create())
        using(var data=Mesh.AcquireReadOnlyMeshData(mesh))
        {
            var raw=data[0];
            for(int s=0;s<mesh.vertexBufferCount;s++)
            {var bytes=raw.GetVertexData<byte>(s).ToArray();hash.TransformBlock(bytes,0,bytes.Length,null,0);}
            var indices=raw.GetIndexData<byte>().ToArray();hash.TransformFinalBlock(indices,0,indices.Length);
            return mesh.vertexCount+":"+mesh.subMeshCount+":"+mesh.indexFormat+":"+Convert.ToBase64String(hash.Hash);
        }
    }
    [MenuItem("RealBuca/Optimize Saved Toy Box Geometry")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Clean edit mode required.");
        Directory.CreateDirectory(Output);backup=Output+"/backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var paths=AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/ToyBoxMenu"}).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p=>p.EndsWith(".asset")).Distinct().ToArray();
        var meshes=paths.ToDictionary(p=>p,p=>AssetDatabase.LoadAssetAtPath<Mesh>(p));
        var groups=new Dictionary<string,List<string>>();
        long beforeVertices=0,beforeTriangles=0;
        foreach(var pair in meshes)
        {
            var m=pair.Value;if(m==null || !m.isReadable)continue;
            beforeVertices+=m.vertexCount;for(int i=0;i<m.subMeshCount;i++)beforeTriangles+=(long)m.GetIndexCount(i)/3;
            string key=Signature(m);if(!groups.TryGetValue(key,out var list))groups[key]=list=new List<string>();list.Add(pair.Key);
        }
        int letters=0,aliases=0,welded=0,compressed=0;
        ToyBoxGeometry.Initialize();
        foreach(var group in groups.Values)
        {
            string original=group.FirstOrDefault(p=>p.StartsWith(ToyBoxGeometry.Folder+"/Text_"));
            if(original==null)continue;
            var match=TextKey.Match(Path.GetFileNameWithoutExtension(original));if(!match.Success)continue;
            string text=match.Groups[1].Value.Replace('_',' ');
            float depth=float.Parse(match.Groups[2].Value,CultureInfo.InvariantCulture);
            float bevel=match.Groups[3].Success?float.Parse(match.Groups[3].Value,CultureInfo.InvariantCulture):0;
            float weight=match.Groups[4].Success?float.Parse(match.Groups[4].Value,CultureInfo.InvariantCulture):.025f;
            foreach(string path in group)Backup(path);
            var generated=ToyBoxGeometry.Text(text,depth,bevel,weight);letters++;
            foreach(string path in group)
            {
                var existing=meshes[path];if(existing==generated)continue;
                string name=existing.name;EditorUtility.CopySerialized(generated,existing);existing.name=name;EditorUtility.SetDirty(existing);aliases++;
            }
        }
        foreach(var pair in meshes)
        {
            var mesh=pair.Value;if(mesh==null || !mesh.isReadable || mesh.blendShapeCount>0 || mesh.bindposes.Length>0)continue;
            Backup(pair.Key);
            if(Weld(mesh))welded++;
            MeshUtility.SetMeshCompression(mesh,ModelImporterMeshCompression.Low);compressed++;
            EditorUtility.SetDirty(mesh);
        }
        AssetDatabase.SaveAssets();
        long afterVertices=0,afterTriangles=0;
        foreach(var mesh in meshes.Values)
        {if(mesh==null || !mesh.isReadable)continue;afterVertices+=mesh.vertexCount;for(int i=0;i<mesh.subMeshCount;i++)afterTriangles+=(long)mesh.GetIndexCount(i)/3;}
        string report=$"Text meshes regenerated: {letters}; exact copied text meshes updated: {aliases}\nMeshes with duplicate vertices removed: {welded}; low mesh compression: {compressed}\nVertices: {beforeVertices:N0} -> {afterVertices:N0}\nTriangles: {beforeTriangles:N0} -> {afterTriangles:N0}\nBackups: {backup}\n";
        File.WriteAllText(Output+"/mesh-results.txt",report);Debug.Log(report);return report;
    }
    // Byte-for-byte welding across ALL vertex streams: UV seams and hard normals
    // cannot be merged accidentally. No triangle is removed or moved here.
    internal static bool Weld(Mesh mesh)
    {
        int count=mesh.vertexCount;if(count==0)return false;
        var attributes=mesh.GetVertexAttributes();var streams=new byte[mesh.vertexBufferCount][];var strides=new int[streams.Length];
        using(var data=Mesh.AcquireReadOnlyMeshData(mesh))for(int s=0;s<streams.Length;s++){streams[s]=data[0].GetVertexData<byte>(s).ToArray();strides[s]=mesh.GetVertexBufferStride(s);}
        var buckets=new Dictionary<ulong,List<int>>();var unique=new List<int>();var remap=new int[count];
        for(int i=0;i<count;i++)
        {
            ulong hash=14695981039346656037UL;
            unchecked{for(int s=0;s<streams.Length;s++)for(int b=0;b<strides[s];b++)hash=(hash^streams[s][i*strides[s]+b])*1099511628211UL;}
            if(!buckets.TryGetValue(hash,out var candidates))buckets[hash]=candidates=new List<int>();
            int found=-1;
            foreach(int candidate in candidates)
            {
                bool same=true;int source=unique[candidate];
                for(int s=0;s<streams.Length&&same;s++)for(int b=0;b<strides[s];b++)if(streams[s][i*strides[s]+b]!=streams[s][source*strides[s]+b]){same=false;break;}
                if(same){found=candidate;break;}
            }
            if(found<0){found=unique.Count;unique.Add(i);candidates.Add(found);}remap[i]=found;
        }
        if(unique.Count==count)return false;
        var indices=new int[mesh.subMeshCount][];var topologies=new MeshTopology[indices.Length];
        for(int s=0;s<indices.Length;s++){indices[s]=mesh.GetIndices(s);topologies[s]=mesh.GetTopology(s);for(int i=0;i<indices[s].Length;i++)indices[s][i]=remap[indices[s][i]];}
        Bounds bounds=mesh.bounds;
        mesh.Clear();mesh.indexFormat=unique.Count<=65535?IndexFormat.UInt16:IndexFormat.UInt32;
        mesh.SetVertexBufferParams(unique.Count,attributes);
        for(int s=0;s<streams.Length;s++)
        {
            if(mesh.GetVertexBufferStride(s)!=strides[s])throw new Exception("Vertex layout changed: "+mesh.name);
            var compact=new byte[unique.Count*strides[s]];
            for(int i=0;i<unique.Count;i++)Buffer.BlockCopy(streams[s],unique[i]*strides[s],compact,i*strides[s],strides[s]);
            mesh.SetVertexBufferData(compact,0,0,compact.Length,s,MeshUpdateFlags.DontRecalculateBounds);
        }
        mesh.subMeshCount=indices.Length;
        for(int s=0;s<indices.Length;s++)mesh.SetIndices(indices[s],topologies[s],s,false);
        mesh.bounds=bounds;return true;
    }
}
