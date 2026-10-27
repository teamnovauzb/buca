#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class NaturalMapWaterValidation
{
    const string Output="output/natural-water";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static Color32[] Surface(Texture artwork,Material material,float time,string path=null)
    {
        var rt=RenderTexture.GetTemporary(1672,941,0,RenderTextureFormat.ARGB32);
        var texture=new Texture2D(1672,941,TextureFormat.RGBA32,false);
        var before=RenderTexture.active;
        try
        {
            material.SetFloat("_WaterPhaseOffset",time);Graphics.Blit(artwork,rt,material);
            RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1672,941),0,0);texture.Apply();
            if(path!=null)File.WriteAllBytes(path,texture.EncodeToPNG());return texture.GetPixels32();
        }
        finally{RenderTexture.active=before;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(texture);}
    }
    static int Delta(Color32 a,Color32 b)=>Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b);
    static double Mean(Color32[] a,Color32[] b,Color32[] field)
    {
        long sum=0;int count=0;
        for(int i=0;i<a.Length;i++)if(field[i].r>128){sum+=Delta(a[i],b[i]);count++;}
        return (double)sum/Math.Max(1,count)/3;
    }
    public static string Check()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode only.");
        Directory.CreateDirectory(Output);
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var image=map.illustratedMenu.artwork.GetComponent<UnityEngine.UI.RawImage>();
        Require(image.material.shader.name=="BUCA/Workshop Misty Harbor","Approved map material required.");
        Require(!ShaderUtil.ShaderHasError(image.material.shader),"Water shader compilation failed.");
        Require(image.GetComponent<ForestHarborWaterFlow>().enabled,"Water time driver disabled.");
        var data=(Texture2D)image.material.GetTexture("_WaterData");Require(data!=null,"Water field missing.");
        var field=data.GetPixels32();var material=new Material(image.material);
        try
        {
            // Freeze the independent fog and beacon so this measures water motion only.
            material.SetFloat("_FlowTime",0);
            var a=Surface(image.texture,material,0,Output+"/Surface-0.png");
            var b=Surface(image.texture,material,.5f,Output+"/Surface-half-second.png");
            var tiny=Surface(image.texture,material,1f/60);
            var areas=new[]{new Rect(770,202,340,51),new Rect(837,253,53,76),new Rect(880,370,101,88),new Rect(430,514,207,106),new Rect(1030,390,470,229),new Rect(982,683,680,80),new Rect(1545,350,126,350)};
            var names=new[]{"Back river","Upper waterfall","Bridge waterfall","World 1 stream","Harbor basin","Foreground sea","Outer harbor channel"};
            var counts=new int[areas.Length];int changed=0,outside=0;
            for(int y=0;y<941;y++)for(int x=0;x<1672;x++)
            {
                int at=y*1672+x;if(Delta(a[at],b[at])<4)continue;changed++;
                if(field[at].r==0)outside++;
                var p=new Vector2(x,940-y);for(int r=0;r<areas.Length;r++)if(areas[r].Contains(p))counts[r]++;
            }
            Require(outside<20,"Water animation reaches non-water pixels: "+outside);
            for(int r=0;r<areas.Length;r++)Require(counts[r]>100,"Water region has insufficient motion: "+names[r]+"="+counts[r]);
            double half=Mean(a,b,field),step=Mean(a,tiny,field);
            Require(step<half*.30,"Motion changes too abruptly between frames.");
            var report=new StringBuilder("PASS: shader compiles and runtime flow driver is enabled.\n");
            report.AppendLine("Water pixels moving over 0.5s: "+changed+"; changes outside water: "+outside);
            for(int r=0;r<areas.Length;r++)report.AppendLine(names[r]+": "+counts[r]+" moving pixels");
            report.AppendLine("Mean RGB change at 60fps: "+step.ToString("F3")+"; over 0.5s: "+half.ToString("F3"));
            foreach(float wrap in new[]{6.25f,1f/.24f,1f/.62f})
            {
                var before=Surface(image.texture,material,wrap-.001f);var after=Surface(image.texture,material,wrap+.001f);
                double seam=Mean(before,after,field);Require(seam<half*.08,"Flow phase resets visibly at "+wrap);
                report.AppendLine("Seam check at "+wrap.ToString("F4")+"s: "+seam.ToString("F4"));
            }
            File.WriteAllText(Output+"/validation.txt",report.ToString());return report.ToString();
        }
        finally{Object.DestroyImmediate(material);}
    }

    static UnityEngine.SceneManagement.Scene preview;
    static TwilightRailwayMap previewMap;
    static Material previewWater;
    static MethodInfo render;
    static int frame;
    public static string StartPreview()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode only.");
        Require(previewMap==null,"Preview already rendering.");
        var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var copy=Object.Instantiate(original.gameObject);copy.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy,preview);
            previewMap=copy.GetComponent<TwilightRailwayMap>();previewMap.eventAudio=previewMap.motionAudio=null;previewMap.Open(null);
            typeof(TwilightRailwayMap).GetField("highest",Private).SetValue(previewMap,49);
            var background=previewMap.illustratedMenu.artwork.GetComponent<UnityEngine.UI.RawImage>();
            previewWater=new Material(background.material);background.material=previewWater;
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.scene=preview;}
            copy.SetActive(true);Directory.CreateDirectory(Output+"/frames");
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true))
                if(camera.gameObject.activeInHierarchy&&camera.targetTexture!=null)
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=camera.targetTexture});
            render=typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic);
            // This selection additionally checks every station/footer raycast before animation capture.
            render.Invoke(null,new object[]{previewMap,11,1920,1080,Output+"/Map.png"});
            frame=0;EditorApplication.update+=NextFrame;return "Rendering a four-second, 24fps Edit-mode water preview.";
        }
        catch{Close();throw;}
    }
    static void NextFrame()
    {
        try
        {
            previewWater.SetFloat("_WaterPhaseOffset",frame/24f);
            render.Invoke(null,new object[]{previewMap,31,1280,720,Output+"/frames/"+frame.ToString("000")+".png"});
            frame++;
            if(frame%12==0)File.WriteAllText(Output+"/preview-progress.txt",frame+"/96 frames");
            if(frame==96){File.WriteAllText(Output+"/preview-complete.txt","96 frames at 24fps, isolated Edit-mode copy; all 50 controls passed. No gameplay or player-progress changes.");Close();}
        }
        catch(Exception e){File.WriteAllText(Output+"/preview-error.txt",e.ToString());Close();}
    }
    static void Close()
    {
        EditorApplication.update-=NextFrame;
        if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);
        if(previewWater!=null)Object.DestroyImmediate(previewWater);
        previewMap=null;previewWater=null;
    }
}
#endif
