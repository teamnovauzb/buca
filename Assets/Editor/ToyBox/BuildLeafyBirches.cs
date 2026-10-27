#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class BuildLeafyBirches
{
    const string Folder="Assets/TwilightMap/ForestHarbor/Birches/";
    static readonly Vector3[] Trees={
        // Root coordinates in artwork pixels; size is tree height.
        new(24,93,85),new(69,91,75),new(118,86,78),new(165,80,66),new(209,83,72),new(260,97,66),new(305,100,65),
        new(19,171,70),new(53,151,66),new(283,159,58),new(331,174,58),new(460,146,68),new(504,156,65),new(549,160,68),new(594,162,63),
        new(377,216,62),new(429,228,64),new(489,241,61),new(550,248,62),new(607,253,58),new(654,254,59),
        new(162,296,78),new(207,297,81),new(261,308,72),
        new(320,381,83),new(366,389,81),new(418,395,79),new(470,401,77),new(518,399,72),new(565,405,76),
        new(346,461,70),new(395,472,72),new(448,482,76),new(498,479,71),new(548,471,73),new(594,456,72),new(650,457,82),new(705,453,72),
        new(20,532,89),new(66,547,80),new(264,577,60),new(311,580,65),new(355,591,63),new(410,615,69),new(458,627,74),new(508,631,67),new(558,640,72),new(610,631,65),new(673,619,79),new(722,596,67),
        new(24,678,87),new(65,681,78),new(47,794,90),new(93,839,91),new(159,913,94),new(22,911,88),new(225,794,64),new(342,822,68),new(733,765,77),new(781,758,74)
    };
    static Texture2D Import(string path,bool readable=false)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var im=(TextureImporter)AssetImporter.GetAtPath(path);im.textureType=TextureImporterType.Default;
        im.npotScale=TextureImporterNPOTScale.None;im.mipmapEnabled=false;im.maxTextureSize=4096;
        im.textureCompression=TextureImporterCompression.Uncompressed;im.alphaIsTransparency=true;im.isReadable=readable;
        im.wrapMode=TextureWrapMode.Clamp;im.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only");
        var map=UnityEngine.Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if(map==null||map.gameObject.scene.path!="Assets/Scenes/MainMenu.unity")throw new Exception("MainMenu map required");
        var art=map.illustratedMenu.artwork;
        var sheet=Import(Folder+"LeafyBirches.png",true);var plate=Import(Folder+"ForestClearing.png");
        // Procedural coverage mask protects the original rails, controls and buildings.
        const int w=836,h=471;var values=new float[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)values[y*w+x]=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(785,810,x*2));
        Action<float,float,float> protect=(cx,cy,r)=>{
            for(int y=Mathf.Max(0,(int)((cy-r)/2));y<Mathf.Min(h,(int)((cy+r)/2)+1);y++)
            for(int x=Mathf.Max(0,(int)((cx-r)/2));x<Mathf.Min(w,(int)((cx+r)/2)+1);x++){
                float d=Vector2.Distance(new Vector2(x*2,y*2),new Vector2(cx,cy));
                values[y*w+x]*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(r-5,r,d));
            }};
        for(float distance=0;distance<=map.stops[30].distance;distance+=.20f){var p=map.Point(distance);protect(p.x*20+836,470.5f-p.z*20,32);}
        Rect[] protectedRects={new(658,0,360,175)};
        foreach(var box in protectedRects)for(int y=Mathf.Max(0,(int)box.y/2);y<Mathf.Min(h,box.yMax/2);y++)for(int x=Mathf.Max(0,(int)box.x/2);x<Mathf.Min(w,box.xMax/2);x++)values[y*w+x]=0;
        var mask=new Texture2D(w,h,TextureFormat.RGBA32,false);var pixels=new Color32[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++){byte v=(byte)(values[y*w+x]*255);pixels[(h-1-y)*w+x]=new Color32(v,v,v,255);}
        mask.SetPixels32(pixels);mask.Apply();File.WriteAllBytes(Folder+"ForestCoverage.png",mask.EncodeToPNG());UnityEngine.Object.DestroyImmediate(mask);
        var coverage=Import(Folder+"ForestCoverage.png");
        var image=art.GetComponent<UnityEngine.UI.RawImage>();var mat=image.material;
        if(!mat.HasProperty("_ForestPlate"))throw new Exception("Forest shader has not imported");
        mat.SetTexture("_ForestPlate",plate);mat.SetTexture("_ForestReplaceMask",coverage);mat.SetFloat("_UseForestPlate",1);EditorUtility.SetDirty(mat);
        var parent=art.Find("LeafyBirchGrove");if(parent==null){parent=new GameObject("LeafyBirchGrove",typeof(RectTransform)).transform;parent.SetParent(art,false);}
        parent.gameObject.layer=art.gameObject.layer;parent.SetAsLastSibling();
        float[] cuts={0,.212f,.516f,.806f,1};var regions=new Rect[4];
        var colors=sheet.GetPixels32();
        for(int n=0;n<4;n++){
            int left=(int)(cuts[n]*sheet.width),right=(int)(cuts[n+1]*sheet.width);int minX=right,maxX=left,minY=sheet.height,maxY=0;
            for(int y=0;y<sheet.height;y++)for(int x=left;x<right;x++)if(colors[y*sheet.width+x].a>32){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            regions[n]=new Rect((float)minX/sheet.width,(float)minY/sheet.height,(float)(maxX-minX+1)/sheet.width,(float)(maxY-minY+1)/sheet.height);
        }
        for(int i=0;i<Trees.Length;i++){
            var t=Trees[i];string name="Birch"+i.ToString("00");var child=parent.Find(name);
            if(child==null){child=new GameObject(name,typeof(RectTransform)).transform;child.SetParent(parent,false);}
            child.gameObject.layer=art.gameObject.layer;
            var rect=(RectTransform)child;rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.pivot=new Vector2(.5f,0);
            rect.anchoredPosition=BuildForestHarborMap.Art(new Vector2(t.x,t.y));var region=regions[i%4];
            rect.sizeDelta=new Vector2(t.z*1.35f*region.width*sheet.width/(region.height*sheet.height),t.z*1.12f);
            if(child.GetComponent<CanvasRenderer>()==null)child.gameObject.AddComponent<CanvasRenderer>();
            var tree=child.GetComponent<ForestBirchGraphic>();if(tree==null)tree=child.gameObject.AddComponent<ForestBirchGraphic>();
            tree.artwork=sheet;tree.uvRegion=region;tree.phase=i*1.73f;tree.raycastTarget=false;tree.Pose(0);
        }
        if(ShaderUtil.ShaderHasError(mat.shader))throw new Exception("Shader errors");
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene);AssetDatabase.SaveAssets();
        return "Saved "+Trees.Length+" separate leafy birches with rooted wind bending. Original rails, buildings and World 2 protected.";
    }
}
#endif
