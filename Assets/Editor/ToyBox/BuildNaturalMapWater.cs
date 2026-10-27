#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Bakes numerical flow fields for the current workshop illustration, never the UI.</summary>
public static class BuildNaturalMapWater
{
    const int W=1672,H=941;
    const string Folder="Assets/TwilightMap/WorkshopRailway";
    const string Output="output/natural-water";
    static Vector2 P(float x,float y)=>new Vector2(x,y);
    static readonly Vector2[][] Areas={
        new[]{P(752,202),P(1115,202),P(1120,228),P(1062,246),P(974,267),P(915,282),P(888,311),P(842,326),P(837,273),P(805,249),P(770,225)},
        new[]{P(840,291),P(916,269),P(1037,287),P(1087,320),P(1166,365),P(1518,331),P(1539,311),P(1576,359),P(1614,463),P(1648,562),P(1669,637),P(1671,758),P(990,770),P(947,740),P(925,660),P(935,606),P(921,550),P(896,495),P(868,454),P(866,402),P(849,353)},
        new[]{P(421,498),P(461,491),P(490,512),P(507,542),P(538,559),P(570,579),P(604,585),P(634,604),P(640,623),P(572,623),P(533,609),P(499,594),P(475,565),P(445,558),P(425,536)}
    };
    static readonly Vector2[][] Falls={
        new[]{P(839,254),P(882,253),P(887,307),P(867,328),P(844,311)},
        new[]{P(883,372),P(939,367),P(951,401),P(976,431),P(963,451),P(900,454),P(886,430)}
    };
    static readonly Vector2[] Seeds={P(828,216),P(961,216),P(1027,220),P(866,279),P(904,389),P(941,431),P(967,480),P(1095,419),P(1219,431),P(1370,488),P(1418,558),P(1273,565),P(1100,719),P(1360,744),P(1615,739),P(1608,540),P(1556,372),P(457,538),P(514,576),P(584,602)};
    static bool Inside(Vector2 p,Vector2[] poly)
    {
        bool inside=false;
        for(int i=0,j=poly.Length-1;i<poly.Length;j=i++)
            if((poly[i].y>p.y)!=(poly[j].y>p.y)&&p.x<(poly[j].x-poly[i].x)*(p.y-poly[i].y)/(poly[j].y-poly[i].y)+poly[i].x)inside=!inside;
        return inside;
    }
    static float Coverage(Vector2 p,Vector2[] poly,float feather)
    {
        if(!Inside(p,poly))return 0;
        float d=10000;
        for(int i=0,j=poly.Length-1;i<poly.Length;j=i++)
        {
            var edge=poly[i]-poly[j];var q=poly[j]+edge*Mathf.Clamp01(Vector2.Dot(p-poly[j],edge)/edge.sqrMagnitude);
            d=Mathf.Min(d,Vector2.Distance(p,q));
        }
        return Mathf.SmoothStep(0,1,d/feather);
    }
    static Texture2D Bake(Texture2D source)
    {
        var readable=new Texture2D(2,2,TextureFormat.RGBA32,false);
        readable.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(source)));
        if(readable.width!=W||readable.height!=H)throw new Exception("Flow data requires the approved 1672x941 artwork.");
        var colors=readable.GetPixels32();Object.DestroyImmediate(readable);
        var candidate=new bool[W*H];var water=new bool[W*H];var queue=new Queue<int>();
        for(int y=0;y<H;y++)for(int x=0;x<W;x++)
        {
            var p=P(x+.5f,H-y-.5f);bool area=false;foreach(var polygon in Areas)area|=Inside(p,polygon);
            if(!area)continue;
            Color c=colors[y*W+x];
            float key=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.012f,.11f,c.b-c.r))
                *Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.015f,.045f,c.b-c.g));
            candidate[y*W+x]=key>.24f;
        }
        // Only water-connected components are kept. Blue roofs, benches and table edges are excluded.
        foreach(var seed in Seeds)
        {
            int best=-1;float dist=10000;
            for(int dy=-12;dy<=12;dy++)for(int dx=-12;dx<=12;dx++)
            {
                int x=(int)seed.x+dx,y=H-1-(int)seed.y+dy;
                if(x<1||x>=W-1||y<1||y>=H-1)continue;
                int index=y*W+x;float d=dx*dx+dy*dy;
                if(candidate[index]&&d<dist){best=index;dist=d;}
            }
            if(best>=0&&!water[best]){water[best]=true;queue.Enqueue(best);}
        }
        while(queue.Count>0)
        {
            int at=queue.Dequeue(),x=at%W,y=at/W;
            foreach(int n in new[]{at-1,at+1,at-W,at+W})
            {
                if(x<=0||x>=W-1||y<=0||y>=H-1||water[n]||!candidate[n])continue;
                water[n]=true;queue.Enqueue(n);
            }
        }
        // Small enclosed gaps are painted foam and lamp reflections, which must travel with the surface.
        var visited=new bool[W*H];var island=new List<int>();
        for(int start=0;start<water.Length;start++)
        {
            if(water[start]||visited[start])continue;
            island.Clear();queue.Enqueue(start);visited[start]=true;bool boundary=false;
            while(queue.Count>0)
            {
                int at=queue.Dequeue(),x=at%W,y=at/W;island.Add(at);
                if(x==0||x==W-1||y==0||y==H-1){boundary=true;continue;}
                foreach(int n in new[]{at-1,at+1,at-W,at+W})
                    if(!water[n]&&!visited[n]){visited[n]=true;queue.Enqueue(n);}
            }
            if(!boundary&&island.Count<=160)foreach(int at in island)water[at]=true;
        }
        var distance=new float[W*H];for(int i=0;i<distance.Length;i++)distance[i]=water[i]?1000:0;
        for(int y=1;y<H;y++)for(int x=1;x<W;x++)
        {
            int at=y*W+x;
            distance[at]=Mathf.Min(distance[at],Mathf.Min(distance[at-1]+1,Mathf.Min(distance[at-W]+1,distance[at-W-1]+1.4142f)));
        }
        for(int y=H-2;y>=0;y--)for(int x=W-2;x>=0;x--)
        {
            int at=y*W+x;
            distance[at]=Mathf.Min(distance[at],Mathf.Min(distance[at+1]+1,Mathf.Min(distance[at+W]+1,distance[at+W+1]+1.4142f)));
        }
        var pixels=new Color32[W*H];int count=0;
        for(int y=0;y<H;y++)for(int x=0;x<W;x++)
        {
            int at=y*W+x;if(!water[at])continue;count++;
            var p=P(x+.5f,H-y-.5f);float fall=0;foreach(var polygon in Falls)fall=Mathf.Max(fall,Coverage(p,polygon,6));
            float stream=x<700?1:(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(978,1090,x)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(275,355,p.y));
            pixels[at]=new Color(Mathf.SmoothStep(0,1,distance[at]/3),Mathf.Clamp01(distance[at]/32),fall,stream);
        }
        if(count<100000)throw new Exception("Incomplete water coverage: "+count);
        var texture=new Texture2D(W,H,TextureFormat.RGBA32,false,true){name="Workshop water flow data",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        texture.SetPixels32(pixels);texture.Apply();
        const string path=Folder+"/NaturalWaterData.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(existing==null)AssetDatabase.CreateAsset(texture,path);
        else{EditorUtility.CopySerialized(texture,existing);Object.DestroyImmediate(texture);texture=existing;EditorUtility.SetDirty(texture);}
        return texture;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only.");
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null||map.gameObject.scene.path!="Assets/Scenes/MainMenu.unity")throw new Exception("MainMenu required.");
        var image=map.illustratedMenu.artwork.GetComponent<UnityEngine.UI.RawImage>();
        if(image.material.shader.name!="BUCA/Workshop Misty Harbor"||ShaderUtil.ShaderHasError(image.material.shader))throw new Exception("Misty Harbor shader required without errors.");
        Directory.CreateDirectory(Output);
        var data=Bake((Texture2D)image.texture);var material=image.material;
        material.SetTexture("_WaterData",data);material.SetFloat("_WaterMotion",1);material.SetFloat("_WaterPhaseOffset",0);
        var driver=image.GetComponent<ForestHarborWaterFlow>();driver.Configure(image);driver.enabled=true;
        EditorUtility.SetDirty(material);EditorUtility.SetDirty(driver);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene);AssetDatabase.SaveAssets();
        EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        string result="Saved natural swells, continuous currents, shoreline foam and downward waterfall flow across both worlds.";
        File.WriteAllText(Output+"/applied.txt",result);return result;
    }
}
#endif
