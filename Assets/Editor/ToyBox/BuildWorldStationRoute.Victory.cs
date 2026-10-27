#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static partial class BuildToyBoxMainMenu
{
    static void BuildWorldVictory(WorldStationRoute3D view)
    {
        var old=view.viewCamera.transform.Find("World30Victory");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=Group("World30Victory",view.viewCamera.transform,new Vector3(0,0,8));
        root.localScale=Vector3.one*.88f;
        var v=root.gameObject.AddComponent<WorldVictoryCelebration3D>();view.victory=v;
        var gold=StationMaterial("VictoryGold",new Color(1,.66f,.15f),.85f);gold.SetColor("_EmissionColor",new Color(.06f,.025f,.002f));
        var cream=StationMaterial("VictoryCream",new Color(1,.93f,.69f),.2f);
        var navy=StationMaterial("VictoryNavy",new Color(.018f,.05f,.13f),.4f);
        var spark=StationMaterial("VictorySpark",new Color(1,.83f,.32f));spark.SetColor("_EmissionColor",new Color(3,1.7f,.35f));
        v.presentation=Group("VictoryPresentation",root);
        var p=v.presentation;
        StationBox("BannerGoldBezel",p,new Vector3(0,-.35f,0),new Vector3(7.5f,1.12f,.20f),gold,.15f);
        StationBox("BannerNavyFace",p,new Vector3(0,-.35f,-.13f),new Vector3(7.32f,.94f,.09f),navy,.12f);
        SignalLetter("WORLD 1 COMPLETE",p,new Vector3(0,-.35f,-.24f),.47f,cream,6.95f);
        StationBox("NextStopFrame",p,new Vector3(0,-1.18f,.03f),new Vector3(5.8f,.56f,.16f),gold,.08f);
        StationBox("NextStopCream",p,new Vector3(0,-1.18f,-.07f),new Vector3(5.63f,.42f,.07f),cream,.06f);
        SignalLetter("NEXT STOP - WORLD 2",p,new Vector3(0,-1.18f,-.14f),.26f,navy,5.3f);
        var numberGold=StationMaterial("VictoryNumberGold",new Color(1,.65f,.16f),.25f);
        numberGold.SetFloat("_Smoothness",.30f);
        numberGold.SetColor("_EmissionColor",new Color(.10f,.045f,.004f));
        SignalLetter("30",p,new Vector3(0,1.03f,-.08f),1.58f,numberGold,3.1f);
        foreach(int side in new[]{-1,1})StationSphere("BannerRivet",p,new Vector3(side*3.59f,-.35f,-.22f),Vector3.one*.12f,gold);
        var starGold=StationMaterial("VictoryStarSatinGold",new Color(1f,.72f,.24f),.55f);
        starGold.SetFloat("_Smoothness",.48f);starGold.SetColor("_EmissionColor",new Color(.08f,.035f,.003f));
        v.stars=new Transform[3];var mesh=VictoryStarMesh();
        for(int i=0;i<3;i++)
        {v.stars[i]=Group("VictoryStar"+i,p,new Vector3((i-1)*1.18f,i==1?2.35f:2.25f,0));var star=MeshObject("GoldStar",v.stars[i],Vector3.zero,mesh,starGold);star.transform.localScale=Vector3.one*(i==1?.48f:.43f);}
        v.confetti=new Transform[360];var colors=new[]{gold,StationMaterial("VictoryCoral",new Color(.95f,.24f,.17f),.5f),StationMaterial("VictoryMint",new Color(.12f,.83f,.69f),.5f)};
        for(int i=0;i<v.confetti.Length;i++)
        {var piece=StationBox("Confetti"+i,root,Vector3.zero,new Vector3(i%5==0?.09f:.055f,i%5==0?.32f:.13f,.018f),colors[i%3],.008f);v.confetti[i]=piece.transform;}
        v.fireworks=new Transform[144];
        for(int i=0;i<144;i++)
        {
            var streak=StationBox("FireworkSpark"+i,root,Vector3.zero,new Vector3(.025f,.16f,.018f),i%3==0?spark:colors[i%3],.008f);
            v.fireworks[i]=streak.transform;
        }
        v.floatingStars=new Transform[24];
        for(int i=0;i<24;i++){var g=MeshObject("CelebrationStar"+i,root,Vector3.zero,mesh,i%3==0?spark:gold);v.floatingStars[i]=g.transform;}
        v.ribbons=new Transform[20];
        for(int i=0;i<20;i++)
        {
            var ribbon=Group("CurledRibbon"+i,root);v.ribbons[i]=ribbon;
            for(int j=0;j<9;j++)
            {var segment=StationBox("RibbonFold",ribbon,new Vector3(Mathf.Sin(j*.6f)*.11f,j*.065f,Mathf.Cos(j*.6f)*.08f),new Vector3(.12f,.09f,.012f),colors[i%3],.008f);segment.transform.localRotation=Quaternion.Euler(0,j*34,Mathf.Cos(j*.6f)*28);}
        }
        v.rays=new Transform[32];
        for(int i=0;i<32;i++)
        {float a=i*Mathf.PI*2/32;var r=Group("BurstRay"+i,p,new Vector3(0,.65f,.25f));r.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);StationBox("LightStreak",r,new Vector3(0,2.1f,0),new Vector3(.018f,2.6f,.012f),spark,.005f);v.rays[i]=r;}
        var fill=Group("VictoryLighting",root,new Vector3(-2,3,-3)).gameObject.AddComponent<Light>();fill.type=LightType.Point;fill.range=12;fill.intensity=3;fill.color=new Color(1,.90f,.69f);fill.shadows=LightShadows.None;
        v.celebrationSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/World30Victory.wav");
        v.fireworkBurstSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Celebration/MilestoneFireworkBurst.wav");
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()){renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;}
        var focusRoot=Group("CelebrationFocus",root);focusRoot.gameObject.layer=29;
        var volume=focusRoot.gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=200;volume.weight=0;v.celebrationVolume=volume;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();
        var dof=profile.Add<DepthOfField>(true);dof.mode.Override(DepthOfFieldMode.Bokeh);dof.focusDistance.Override(8f);dof.focalLength.Override(250f);dof.aperture.Override(1.2f);
        var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.32f);vignette.smoothness.Override(.65f);
        profile=ToyBoxGeometry.Save(profile,StationFolder+"/VictoryFocus.asset");volume.sharedProfile=profile;
        foreach(var component in profile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,profile);
        root.gameObject.SetActive(false);
    }
    static Mesh VictoryStarMesh()
    {
        // Rounded silhouette, broad flat face, rolled bevel and solid side wall.
        var outline=new System.Collections.Generic.List<Vector3>();
        var corners=new Vector3[10];
        for(int i=0;i<10;i++){float a=(90+i*36)*Mathf.Deg2Rad;float r=i%2==0?1:.47f;corners[i]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);}
        for(int i=0;i<10;i++)
        {
            var c=corners[i];var a=Vector3.Lerp(c,corners[(i+9)%10],.10f);var b=Vector3.Lerp(c,corners[(i+1)%10],.10f);
            for(int j=0;j<6;j++){float t=j/5f;outline.Add((1-t)*(1-t)*a+2*(1-t)*t*c+t*t*b);}
        }
        int n=outline.Count;var vertices=new Vector3[n*4+2];var triangles=new System.Collections.Generic.List<int>();
        float[] sizes={.90f,1f,1f,.90f},depths={-.15f,-.075f,.075f,.15f};
        for(int r=0;r<4;r++)for(int i=0;i<n;i++)vertices[r*n+i]=outline[i]*sizes[r]+Vector3.forward*depths[r];
        vertices[n*4]=new Vector3(0,0,-.15f);vertices[n*4+1]=new Vector3(0,0,.15f);
        for(int i=0;i<n;i++)
        {
            int next=(i+1)%n;triangles.AddRange(new[]{n*4,next,i,n*4+1,3*n+i,3*n+next});
            for(int r=0;r<3;r++){int a=r*n+i,b=r*n+next,c=(r+1)*n+i,d=(r+1)*n+next;triangles.AddRange(new[]{a,b,c,b,d,c});}
        }
        var mesh=new Mesh{name="RoundedBeveledVictoryStar"};mesh.vertices=vertices;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
        return ToyBoxGeometry.Save(mesh,StationFolder+"/RoundedBeveledVictoryStar.asset");
    }
}
#endif
