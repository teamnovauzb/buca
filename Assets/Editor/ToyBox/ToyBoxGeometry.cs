using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Editor-only solid geometry. All output is persisted as mesh assets.</summary>
internal static partial class ToyBoxGeometry
{
    [Serializable] internal class FontData { public Glyph[] glyphs; }
    [Serializable] internal class Glyph { public string character; public float width; public Contour[] contours; }
    [Serializable] internal class Contour { public Vector2[] points; }
    internal static readonly Dictionary<char, Glyph> Glyphs = new Dictionary<char, Glyph>();
    static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();
    internal const string Folder = "Assets/ToyBoxMenu/Meshes";

    internal static void Initialize()
    {
        Cache.Clear(); Glyphs.Clear();
        var source = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Editor/ToyBox/RoundedGlyphContours.json");
        if (source == null) throw new InvalidOperationException("RoundedGlyphContours.json is missing beside the Toy Box editor scripts.");
        foreach (var glyph in JsonUtility.FromJson<FontData>(source.text).glyphs)
        {
            foreach (var contour in glyph.contours)
                contour.points = ToyBoxGlyphSimplifier.Simplify(contour.points);
            Glyphs.Add(glyph.character[0], glyph);
        }
    }

    internal static T Save<T>(T value, string path) where T : UnityEngine.Object
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null) { AssetDatabase.CreateAsset(value, path); return value; }
        EditorUtility.CopySerialized(value, existing);
        UnityEngine.Object.DestroyImmediate(value);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    static Mesh Finish(string key, List<Vector3> vertices, List<int> triangles, List<Vector2> uv = null)
    {
        var mesh = new Mesh { name = key, indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
        if (uv != null) mesh.SetUVs(0, uv);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        if (uv != null) mesh.RecalculateTangents();
        mesh = Save(mesh, Folder + "/" + key + ".asset");
        Cache[key] = mesh;
        return mesh;
    }

    static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        bool forward = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) > 0;
        if (forward) t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        else t.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
    }

    internal static Mesh RoundedBox(Vector3 size, float radius)
    {
        string key = $"Box_{size.x:F3}_{size.y:F3}_{size.z:F3}_{radius:F3}";
        if (Cache.TryGetValue(key, out Mesh cached)) return cached;
        radius = Mathf.Min(radius, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.499f);
        Vector3 half = size * 0.5f, inner = half - Vector3.one * radius;
        var vertices = new List<Vector3>(); var normals = new List<Vector3>();
        var triangles = new List<int>(); var uv = new List<Vector2>();
        var directions = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        const int n = 12;
        foreach (Vector3 normal in directions)
        {
            Vector3 u = normal.x != 0 ? Vector3.forward : Vector3.right;
            Vector3 w = Vector3.Cross(normal, u);
            int first = vertices.Count;
            for (int y = 0; y <= n; y++) for (int x = 0; x <= n; x++)
            {
                // Concentrate samples on the rounded edges, leaving broad planar faces.
                float sx = EdgeSample(x, n, radius / Vector3.Dot(half, Abs(u)));
                float sy = EdgeSample(y, n, radius / Vector3.Dot(half, Abs(w)));
                Vector3 p = Vector3.Scale(normal + u * sx + w * sy, half);
                Vector3 q = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x),
                    Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                Vector3 outward = (p - q).normalized;
                vertices.Add(q + outward * radius); normals.Add(outward);
                uv.Add(new Vector2(Vector3.Dot(p, u), Vector3.Dot(p, w)) * 0.2f);
                if (x < n && y < n)
                {
                    int a = first + y * (n + 1) + x;
                    triangles.AddRange(new[] { a, a + 1, a + n + 2, a, a + n + 2, a + n + 1 });
                }
            }
        }
        var mesh = Finish(key, vertices, triangles, uv);
        mesh.SetNormals(normals); mesh.RecalculateTangents(); EditorUtility.SetDirty(mesh);
        return mesh;
    }

    static Vector3 Abs(Vector3 value) => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    static float EdgeSample(int i, int n, float fraction)
    {
        if (i <= 5) return -1f + fraction * i / 5f;
        if (i >= n - 5) return 1f - fraction * (n - i) / 5f;
        return 0f;
    }

    internal static Mesh Disc(float radius, float height, float bevel)
    {
        string key = $"Disc_{radius:F3}_{height:F3}_{bevel:F3}";
        if (Cache.TryGetValue(key, out Mesh cached)) return cached;
        var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
        const int segments = 64;
        bevel = Mathf.Min(bevel, height * 0.49f);
        var rings = new List<Vector2> { new Vector2(0, -height / 2), new Vector2(radius - bevel, -height / 2) };
        for (int i = 1; i <= 5; i++)
        {
            float a = i / 5f * Mathf.PI / 2;
            rings.Add(new Vector2(radius - bevel + Mathf.Sin(a) * bevel, -height / 2 + bevel - Mathf.Cos(a) * bevel));
        }
        for (int i = 0; i <= 5; i++)
        {
            float a = i / 5f * Mathf.PI / 2;
            rings.Add(new Vector2(radius - bevel + Mathf.Cos(a) * bevel, height / 2 - bevel + Mathf.Sin(a) * bevel));
        }
        rings.Add(new Vector2(0, height / 2));
        for (int row = 0; row < rings.Count; row++) for (int i = 0; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2 / segments;
            v.Add(new Vector3(Mathf.Cos(a) * rings[row].x, rings[row].y, Mathf.Sin(a) * rings[row].x));
            uv.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rings[row].x * 0.2f);
            if (row > 0 && i < segments)
            {
                int p = row * (segments + 1) + i, b = p - segments - 1;
                t.AddRange(new[] { b, p, p + 1, b, p + 1, b + 1 });
            }
        }
        return Finish(key, v, t, uv);
    }

    internal static Mesh Ring(float radius, float width, float height, float arc = 360)
    {
        string key = $"Ring_{radius:F3}_{width:F3}_{height:F3}_{arc:F0}";
        if (Cache.TryGetValue(key, out Mesh cached)) return cached;
        var v = new List<Vector3>(); var t = new List<int>();
        int n = Mathf.CeilToInt(arc / 5f);
        for (int i = 0; i < n; i++)
        {
            float a = i * arc / n * Mathf.Deg2Rad, b = (i + 1) * arc / n * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), q = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
            float inner = radius - width / 2, outer = radius + width / 2;
            Vector3 up = Vector3.up * height;
            Quad(v,t,p*inner,q*inner,q*outer,p*outer,Vector3.up);
            Quad(v,t,p*inner-up,p*outer-up,q*outer-up,q*inner-up,Vector3.down);
            Quad(v,t,p*outer,p*outer-up,q*outer-up,q*outer,p+q);
            Quad(v,t,p*inner,q*inner,q*inner-up,p*inner-up,-p-q);
        }
        return Finish(key,v,t);
    }

    internal static Mesh Torus(float radius, float tube)
    {
        string key=$"Torus_{radius:F3}_{tube:F3}";
        if(Cache.TryGetValue(key,out Mesh cached)) return cached;
        var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var indices=new List<int>();
        const int segments=72, sides=12;
        for(int i=0;i<=segments;i++) for(int j=0;j<=sides;j++)
        {
            float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides;
            Vector3 radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            Vector3 normal=radial*Mathf.Cos(b)+Vector3.up*Mathf.Sin(b);
            vertices.Add(radial*radius+normal*tube); normals.Add(normal);
            if(i<segments&&j<sides)
            {
                int p=i*(sides+1)+j,q=p+sides+1;
                indices.AddRange(new[]{p,p+1,q+1,p,q+1,q});
            }
        }
        var mesh=Finish(key,vertices,indices); mesh.SetNormals(normals); EditorUtility.SetDirty(mesh); return mesh;
    }

    internal static Mesh Text(string text, float depth = 0.10f, float bevel = 0, float weight = .025f)
    {
        string key = "Text_" + text.Replace(' ', '_') + "_" + depth.ToString("F3");
        if (bevel > 0) key += "_Bevel_" + bevel.ToString("F3") + "_Weight_" + weight.ToString("F3");
        if (Cache.TryGetValue(key, out Mesh cached)) return cached;
        var v = new List<Vector3>(); var t = new List<int>();
        float cursor = 0;
        foreach (char ch in text)
        {
            if (ch == ' ') { cursor += 0.45f; continue; }
            if (!Glyphs.TryGetValue(ch, out Glyph glyph)) continue;
            AddGlyph(glyph, cursor, depth, v, t, bevel, weight);
            cursor += glyph.width + 0.13f;
        }
        float center = (cursor - 0.13f) * 0.5f;
        for (int i = 0; i < v.Count; i++) v[i] -= new Vector3(center, 0.5f, 0);
        var uv = new List<Vector2>();
        foreach (var point in v) uv.Add(new Vector2(point.x, point.y) * .7f);
        var mesh=Finish(key, v, t, uv);
        if(bevel>0)
        {
            var sums=new Dictionary<Vector3Int,Vector3>();
            var normals=mesh.normals;
            for(int i=0;i<v.Count;i++)
            {
                Vector3Int p=Vector3Int.RoundToInt(v[i]*100000);
                sums.TryGetValue(p,out Vector3 n); sums[p]=n+normals[i];
            }
            for(int i=0;i<v.Count;i++) normals[i]=sums[Vector3Int.RoundToInt(v[i]*100000)].normalized;
            mesh.normals=normals; mesh.RecalculateTangents(); EditorUtility.SetDirty(mesh);
        }
        return mesh;
    }

    struct Edge { public Vector2 a, b; public float X(float y) => Mathf.LerpUnclamped(a.x,b.x,(y-a.y)/(b.y-a.y)); }
    static Glyph OffsetGlyph(Glyph glyph, float distance)
    {
            var bold = new Contour[glyph.contours.Length];
            for (int loop=0;loop<bold.Length;loop++)
            {
                Vector2[] source=glyph.contours[loop].points;
                var points=new Vector2[source.Length];
                for(int i=0;i<points.Length;i++)
                {
                    Vector2 before=(source[i]-source[(i+source.Length-1)%source.Length]).normalized;
                    Vector2 after=(source[(i+1)%source.Length]-source[i]).normalized;
                    Vector2 first=new Vector2(-before.y,before.x),second=new Vector2(-after.y,after.x);
                    Vector2 outward=(first+second).normalized;
                    points[i]=source[i]+outward*(distance/Mathf.Max(.6f,Vector2.Dot(outward,first)));
                }
                bold[loop]=new Contour { points=points };
            }
            return new Glyph { character=glyph.character,width=glyph.width,contours=bold };
    }

    static void AddGlyph(Glyph glyph, float offset, float depth, List<Vector3> v, List<int> t, float bevel, float weight)
    {
        if (depth >= .15f) glyph = OffsetGlyph(glyph, weight);
        bevel = Mathf.Min(bevel, depth * .45f);
        Glyph face = bevel > 0 ? OffsetGlyph(glyph, -bevel) : glyph;
        // Rounded shoulders connect inset front/back faces to the full outline.
        // These are actual solid surfaces, including the counters inside B/A.
        if (bevel > 0)
        {
            const int steps = 5;
            for (int band = 0; band < steps; band++)
            {
                float a = band * Mathf.PI * .5f / steps, b = (band + 1) * Mathf.PI * .5f / steps;
                Glyph first = OffsetGlyph(glyph, -bevel + Mathf.Sin(a) * bevel);
                Glyph second = OffsetGlyph(glyph, -bevel + Mathf.Sin(b) * bevel);
                float za = bevel * (1 - Mathf.Cos(a)), zb = bevel * (1 - Mathf.Cos(b));
                for (int loop = 0; loop < first.contours.Length; loop++)
                {
                    var p = first.contours[loop].points; var q = second.contours[loop].points;
                    for (int i = 0; i < p.Length; i++)
                    {
                        int j = (i + 1) % p.Length;
                        Vector3 pa = new Vector3(p[i].x + offset, p[i].y, za), pb = new Vector3(p[j].x + offset, p[j].y, za);
                        Vector3 qa = new Vector3(q[i].x + offset, q[i].y, zb), qb = new Vector3(q[j].x + offset, q[j].y, zb);
                        Vector2 edge = p[j] - p[i];
                        Vector3 outward = new Vector3(-edge.y, edge.x, 0).normalized;
                        Quad(v, t, pa, pb, qb, qa, outward + Vector3.back);
                        pa.z = depth - za; pb.z = depth - za; qa.z = depth - zb; qb.z = depth - zb;
                        Quad(v, t, pa, qa, qb, pb, outward + Vector3.forward);
                    }
                }
            }
        }
        var edges = new List<Edge>(); var heights = new SortedSet<float>();
        foreach (Contour contour in glyph.contours)
        {
            for (int i = 0; i < contour.points.Length; i++)
            {
                Vector2 a = contour.points[i], b = contour.points[(i + 1) % contour.points.Length];
                Vector3 va = new Vector3(a.x+offset,a.y,bevel), vb = new Vector3(b.x+offset,b.y,bevel);
                Vector2 mid = (a+b)*0.5f, left = new Vector2(-(b-a).y,(b-a).x).normalized;
                Vector2 test = mid + left*0.00005f;
                bool inside = false;
                foreach (Contour other in glyph.contours)
                    for (int k=0,j=other.points.Length-1;k<other.points.Length;j=k++)
                    {
                        Vector2 p=other.points[k],q=other.points[j];
                        if ((p.y>test.y)!=(q.y>test.y) && test.x < (q.x-p.x)*(test.y-p.y)/(q.y-p.y)+p.x) inside=!inside;
                    }
                Vector3 normal = new Vector3(left.x,left.y,0)*(inside?-1:1);
                Quad(v,t,va,vb,vb+Vector3.forward*(depth-2*bevel),va+Vector3.forward*(depth-2*bevel),normal);
            }
        }
        foreach (var contour in face.contours)
            for (int i = 0; i < contour.points.Length; i++)
            {
                Vector2 a = contour.points[i], b = contour.points[(i + 1) % contour.points.Length];
                heights.Add(a.y);
                if (Mathf.Abs(a.y-b.y) > .000001f) edges.Add(new Edge { a=a, b=b });
            }
        // Even-odd scanline trapezoids tessellate concave letters AND their holes.
        var ys = new List<float>(heights);
        for (int row=0;row<ys.Count-1;row++)
        {
            float low=ys[row],high=ys[row+1],middle=(low+high)*0.5f;
            if (high-low<0.000001f) continue;
            var crossing = edges.FindAll(e => middle > Mathf.Min(e.a.y,e.b.y) && middle < Mathf.Max(e.a.y,e.b.y));
            crossing.Sort((a,b)=>a.X(middle).CompareTo(b.X(middle)));
            for (int i=0;i+1<crossing.Count;i+=2)
            {
                Vector3 a=new Vector3(crossing[i].X(low)+offset,low,0),b=new Vector3(crossing[i+1].X(low)+offset,low,0);
                Vector3 c=new Vector3(crossing[i+1].X(high)+offset,high,0),d=new Vector3(crossing[i].X(high)+offset,high,0);
                Quad(v,t,a,b,c,d,Vector3.back);
                Vector3 back=Vector3.forward*depth;
                Quad(v,t,a+back,b+back,c+back,d+back,Vector3.forward);
            }
        }
    }
}
