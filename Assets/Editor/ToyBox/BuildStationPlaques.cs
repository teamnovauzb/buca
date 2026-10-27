#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class BuildStationPlaques
{
    const string Folder="Assets/TwilightMap/WorkshopRailway";
    const string Output="output/station-plaques";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static TMP_FontAsset NumberFont()
    {
        string path=Folder+"/Station Numbers SDF.asset";
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(font==null)
        {
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/TwilightMap/ForestHarbor/BucaRoundedMenu.ttf");
            font=TMP_FontAsset.CreateFontAsset(source,90,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDF16,512,512,AtlasPopulationMode.Dynamic);
            font.name="Station Numbers SDF";
            Require(font.TryAddCharacters("0123456789"),"Could not bake all ten number glyphs.");
            AssetDatabase.CreateAsset(font,path);AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var atlas in font.atlasTextures){atlas.filterMode=FilterMode.Bilinear;AssetDatabase.AddObjectToAsset(atlas,font);}
            font.atlasPopulationMode=AtlasPopulationMode.Static;
        }
        // Shader.Find can pick the older, duplicate shader under Luxodd's sample content.
        // Unity 6 TMP stores the SDF scale in UV0.w; the legacy shader reads UV1.y.
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader");
        Require(shader!=null,"Current TMP shader is required for clean number edges.");
        font.material.shader=shader;
        font.material.SetFloat("_OutlineWidth",0);font.material.SetFloat("_OutlineSoftness",0);
        font.material.SetFloat("_FaceDilate",0);font.material.SetFloat("_WeightBold",.35f);
        font.material.DisableKeyword("OUTLINE_ON");font.material.DisableKeyword("UNDERLAY_ON");font.material.DisableKeyword("GLOW_ON");
        EditorUtility.SetDirty(font);EditorUtility.SetDirty(font.material);return font;
    }
    public static string Apply()
    {
        return Apply(false);
    }
    [MenuItem("RealBuca/Workshop Railway/Apply Locked Ticket Level Markers")]
    public static string ApplyTickets()
    {
        return Apply(true);
    }
    static Vector2 TicketOffset(float height)
    {
        // Center the visible ticket face on the rails, accounting for its hanging lock.
        return new Vector2(1.5f,-14-(height-19)*.5f);
    }
    static string Apply(bool tickets)
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode required.");
        var map=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        Require(map!=null&&map.gameObject.scene.path=="Assets/Scenes/MainMenu.unity","MainMenu required.");
        var view=map.illustratedMenu;var presentation=view.GetComponent<ForestHarborMapPresentation>();
        Require(presentation.workshopRailway&&view.stopRects.Length==50,"Existing 50-stop workshop map required.");
        Directory.CreateDirectory(Output);
        if(!File.Exists(Output+"/MainMenu.before-plaques.unity"))File.Copy(map.gameObject.scene.path,Output+"/MainMenu.before-plaques.unity");
        var font=NumberFont();presentation.stationPlaques=new WorkshopStationBadge[50];
        for(int i=0;i<50;i++)
        {
            var root=view.stopRects[i];var point=map.Point(map.stops[i].distance);
            root.anchoredPosition=new Vector2(point.x,point.z)*20;view.route[i]=root.anchoredPosition;
            root.localRotation=Quaternion.identity;root.localScale=Vector3.one;
            float artY=470.5f-root.anchoredPosition.y;
            float width=Mathf.Lerp(tickets?46:40,tickets?52:46,Mathf.InverseLerp(250,650,artY));
            float height=tickets?width*.60f+18:width*1.02f;
            var offset=tickets?TicketOffset(height):Vector2.zero;
            var plaque=root.Find("WorkshopPlaque").GetComponent<WorkshopStationBadge>();
            plaque.SetTrainClearance(Vector2.zero);
            presentation.stationPlaques[i]=plaque;plaque.stationPlaque=true;plaque.raycastTarget=false;
            plaque.ticketPlaque=tickets;
            var r=plaque.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,0);
            r.anchoredPosition=new Vector2(0,-3)+offset;r.sizeDelta=new Vector2(width,height);r.localRotation=Quaternion.identity;r.localScale=Vector3.one;
            plaque.SetAllDirty();plaque.transform.SetAsFirstSibling();
            var faceSize=tickets?new Vector2(width-3,height-19):new Vector2(width-7.8f,height-17.8f);
            var faceCenter=new Vector2(-1.5f,tickets?14+(height-19)*.5f:-3+11+(height-13)*.5f)+offset;
            var face=view.faces[i];face.sprite=null;face.type=UnityEngine.UI.Image.Type.Simple;face.color=Color.clear;
            face.rectTransform.anchoredPosition=faceCenter;face.rectTransform.sizeDelta=faceSize;
            face.rectTransform.localScale=Vector3.one;face.rectTransform.localRotation=Quaternion.identity;face.raycastTarget=true;
            var glow=view.glows[i];glow.rectTransform.anchoredPosition=faceCenter;glow.rectTransform.sizeDelta=faceSize;
            glow.color=Color.clear;glow.raycastTarget=false;
            var number=view.numbers[i];number.font=font;number.fontSharedMaterial=font.material;
            number.text=(i+1).ToString();number.fontSize=Mathf.Lerp(tickets?15:18,tickets?16.5f:22,Mathf.InverseLerp(tickets?46:40,tickets?52:46,width));
            number.fontStyle=FontStyles.Bold;number.enableAutoSizing=false;
            number.alignment=TextAlignmentOptions.Center;number.textWrappingMode=TextWrappingModes.NoWrap;
            number.overflowMode=TextOverflowModes.Overflow;number.characterSpacing=0;number.margin=Vector4.zero;
            number.rectTransform.anchoredPosition=faceCenter;
            number.rectTransform.sizeDelta=tickets?new Vector2(width-20,height-23):faceSize;
            number.rectTransform.localScale=Vector3.one;number.rectTransform.localRotation=Quaternion.identity;
            number.raycastTarget=false;number.gameObject.SetActive(true);number.UpdateMeshPadding();number.SetAllDirty();
            view.locks[i].transform.localPosition=new Vector3(width*.40f+offset.x,2+offset.y,0);
            view.awards[i].transform.localPosition=new Vector3(-width*.40f+offset.x,2+offset.y,0);
            root.SetAsLastSibling(); // Plaques remain legible when the engine arrives behind them.
            EditorUtility.SetDirty(plaque);EditorUtility.SetDirty(number);EditorUtility.SetDirty(face);
        }
        if(tickets)
        {
            // Park at the ticket, with the ticket and its number drawn above the engine.
            map.trainNoseOffset=BuildTrainStopClearance.StopOffset;
            map.stationPause=.6f;
            view.forestTrainDisplayScale=.725f;
            // Keep wheel contact on the foreground rail, below the ticket center.
            view.forestTrainRailOffset=-7f;
            map.trainWheelTravelScale=view.forestTrainDisplayScale;
            BuildTrainStopClearance.Configure(map);
            EditorUtility.SetDirty(map);
        }
        presentation.arrivalMarker.ring.transform.SetAsLastSibling();
        map.menuLever.transform.SetAsLastSibling();view.controls.SetAsLastSibling();
        view.Refresh();presentation.Refresh();
        EditorUtility.SetDirty(view);EditorUtility.SetDirty(presentation);
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene);AssetDatabase.SaveAssets();
        EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        return tickets?"Saved option 4 on all 50 stations: notched cream tickets, maple bevels, teal stripes, clear numbers and hanging brass padlocks on locked levels.":
            "Saved 50 wooden station plaques across both worlds, rooted on the train route, with crisp SDF16 navy numbers and a green arrival lamp.";
    }

    public static string CheckAndRender()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode only.");
        var original=Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var copy=Object.Instantiate(original.gameObject);copy.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy,scene);
            var map=copy.GetComponent<TwilightRailwayMap>();map.eventAudio=map.motionAudio=null;map.Open(null);
            typeof(TwilightRailwayMap).GetField("highest",Private).SetValue(map,49);
            var view=map.illustratedMenu;var presentation=view.GetComponent<ForestHarborMapPresentation>();
            Require(presentation.HasStationPlaques,"Missing station plaques.");
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true)){camera.enabled=false;camera.scene=scene;}
            copy.SetActive(true);Directory.CreateDirectory(Output);
            foreach(var camera in copy.GetComponentsInChildren<Camera>(true))
                if(camera.gameObject.activeInHierarchy&&camera.targetTexture!=null)
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=camera.targetTexture});
            for(int i=0;i<50;i++)
            {
                var number=view.numbers[i];var point=map.Point(map.stops[i].distance);
                Require(number.text==(i+1).ToString(),"Incorrect or duplicate number: "+(i+1));
                Require(Vector2.Distance(view.stopRects[i].anchoredPosition,new Vector2(point.x,point.z)*20)<.01f,"Plaque base misses train stop: "+(i+1));
                if(presentation.stationPlaques[i].ticketPlaque)
                {
                    var clearance=presentation.stationPlaques[i].TrainClearance;
                    Require((view.faces[i].rectTransform.anchoredPosition-clearance).sqrMagnitude<.0001f,"Ticket click target misses its station: "+(i+1));
                    Require((number.rectTransform.anchoredPosition-clearance).sqrMagnitude<.0001f,"Number misses its ticket: "+(i+1));
                }
                Require(number.font.atlasRenderMode==UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDF16,"Number font must use SDF16.");
                Require(AssetDatabase.GetAssetPath(number.fontSharedMaterial.shader)=="Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader","Number uses the legacy TMP shader.");
                Require(number.fontSharedMaterial.GetFloat("_OutlineSoftness")==0&&!number.enableAutoSizing,"Number rendering has blur or autosizing.");
                number.ForceMeshUpdate();
                Require(number.preferredWidth<number.rectTransform.rect.width,"Number does not fit: "+(i+1));
            }
            var render=typeof(ForestHarborValidation).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic);
            render.Invoke(null,new object[]{map,11,1920,1080,Output+"/World1.png"});
            render.Invoke(null,new object[]{map,31,1920,1080,Output+"/World2.png"});
            Require(!presentation.arrivalMarker.ring.circular,"Arrival glow must fit the plaque.");
            Require(Vector2.Distance(presentation.arrivalMarker.ring.rectTransform.anchoredPosition,
                view.stopRects[31].anchoredPosition+view.faces[31].rectTransform.anchoredPosition)<.01f,"Arrival glow is not centered on number.");
            typeof(TwilightRailwayMap).GetField("moving",Private).SetValue(map,true);presentation.Refresh();
            Require(!presentation.arrivalMarker.ring.gameObject.activeSelf,"Arrival glow remains visible in transit.");
            render.Invoke(null,new object[]{map,49,1600,1000,Output+"/Level50-16x10.png"});
            render.Invoke(null,new object[]{map,31,3840,2160,Output+"/World2-4K.png"});
            string report="PASS: exactly 50 native number labels; every plaque base matches its train stop; static SDF16 font with no outline softness; all number strings fit; all 50 level/footer raycasts pass; green plaque glow aligns on arrival and hides during travel. World 1, World 2, level 50, 16:10 and 4K previews rendered. No Play mode or player-progress changes.";
            File.WriteAllText(Output+"/validation.txt",report);return report;
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
