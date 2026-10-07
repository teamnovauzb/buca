using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class BuildToyBoxMainMenu
{
    [MenuItem("RealBuca/Toy Box 3D/11 - Brighten Current Objects and Add Shine")]
    public static void AddCurrentObjectShine()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var poses=new Dictionary<Transform,(Vector3 position,Quaternion rotation,Vector3 scale)>();
            foreach(var part in root.GetComponentsInChildren<Transform>(true))
                if(part.GetComponentInParent<ToyBoxSurfaceShine>()==null)
                    poses.Add(part,(part.localPosition,part.localRotation,part.localScale));
            int colliders=root.GetComponentsInChildren<Collider>(true).Length;
            AddSurfaceShine(root.transform,root.GetComponent<ToyBoxMenuController>());
            foreach(var pair in poses)
                if(pair.Key==null || pair.Key.localPosition!=pair.Value.position || pair.Key.localRotation!=pair.Value.rotation || pair.Key.localScale!=pair.Value.scale)
                    throw new Exception("Shine changed an existing object pose.");
            if(root.GetComponentsInChildren<Collider>(true).Length!=colliders) throw new Exception("Shine changed interaction colliders.");
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MenuScene);
        var menu=UnityEngine.Object.FindAnyObjectByType<ToyBoxMenuController>();
        Validate(menu.gameObject);
        var shine=menu.GetComponentInChildren<ToyBoxSurfaceShine>(true);
        if(shine==null || shine.glints.Length!=9) throw new Exception("Missing saved surface glints.");
        int count=menu.GetComponentsInChildren<Transform>(true).Length;
        shine.Sample(0);var before=shine.glints[0].localScale;
        shine.Sample(.8f);
        if(before==shine.glints[0].localScale || menu.GetComponentsInChildren<Transform>(true).Length!=count)
            throw new Exception("Surface animation did not sample existing meshes.");
        foreach(var glint in shine.glints)
            if(!AssetDatabase.Contains(glint.GetComponent<MeshFilter>().sharedMesh) || glint.GetComponent<Collider>()!=null)
                throw new Exception("Glints must be saved, non-interactive geometry.");
        menu.SetClock(30);
        ToyBoxMenuValidation.RenderCamera(menu.menuCamera,"Workshop-Object-Shine",1920,1080);
        Debug.Log("OBJECT_SHINE_PASSED: saved glints animate without constructing objects; existing layout and controls retained.");
    }

    public static void ValidateCurrentObjectShineBatch()
    {
        AddCurrentObjectShine();
        ToyBoxStartupValidation.RunBatch();
    }

    static void AddSurfaceShine(Transform root,ToyBoxMenuController menu)
    {
        // Modify illumination and material overrides only: existing geometry/camera stays in place.
        var rig=root.Find("CameraAndLighting");
        var key=rig.Find("WarmKey").GetComponent<Light>();key.intensity=1.05f;key.shadowStrength=.72f;
        rig.Find("CoolFill").GetComponent<Light>().intensity=.34f;
        rig.GetComponentInChildren<ReflectionProbe>().intensity=.78f;
        var volume=rig.GetComponentInChildren<Volume>();
        const string profilePath=Root+"/Materials/CurrentObjectShineGrade.asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if(profile==null)
        {
            profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="CurrentObjectShineGrade";
            AssetDatabase.CreateAsset(profile,profilePath);
            foreach(var source in volume.sharedProfile.components)
            {
                var copy=UnityEngine.Object.Instantiate(source);copy.name=source.name;
                AssetDatabase.AddObjectToAsset(copy,profile);profile.components.Add(copy);
            }
        }
        if(profile.TryGet<ColorAdjustments>(out var grade))
        {
            grade.postExposure.Override(.04f);grade.contrast.Override(5);EditorUtility.SetDirty(grade);
        }
        if(profile.TryGet<Bloom>(out var bloom))
        {
            bloom.intensity.Override(.22f);bloom.threshold.Override(1.05f);EditorUtility.SetDirty(bloom);
        }
        EditorUtility.SetDirty(profile);volume.sharedProfile=profile;

        var glossy=new HashSet<string>{"CobaltPaint","CoralPaint","HoneyYellow","MintPaint","WorkshopBluePanel","WorkshopLeaf"};
        foreach(var branch in new[]{root.Find("ToyTable"),root.Find("RaisedBucaSign"),root.Find("ToyRoomScenery"),menu.homeRoot.transform})
        foreach(var renderer in branch.GetComponentsInChildren<MeshRenderer>(true))
        {
            var original=renderer.sharedMaterial;
            if(original==null)continue;
            string name=original.name.Replace("MenuShine_","");
            if(!glossy.Contains(name))continue;
            var source=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+name+".mat");
            string path=Root+"/Materials/MenuShine_"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                material=new Material(source){name="MenuShine_"+name};AssetDatabase.CreateAsset(material,path);
            }
            material.SetFloat("_Smoothness",.66f);material.SetFloat("_BumpScale",.16f);
            // Low color lift leaves the form's shadows intact; bloom comes from the small glints.
            material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",source.GetColor("_BaseColor")*.035f);
            EditorUtility.SetDirty(material);renderer.sharedMaterial=material;
        }

        var old=menu.homeRoot.transform.Find("SavedSurfaceShine");
        if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var group=Group("SavedSurfaceShine",menu.homeRoot.transform);
        var animation=group.gameObject.AddComponent<ToyBoxSurfaceShine>();animation.viewCamera=menu.menuCamera;
        var glints=new List<Transform>();var anchors=new List<Transform>();var offsets=new List<Vector3>();
        var mesh=SurfaceGlintMesh();
        var colors=new[]{new Color(.4f,.7f,1),new Color(1,.48f,.3f),new Color(1,.82f,.25f),new Color(.4f,1,.7f)};
        var materials=new Material[4];
        for(int i=0;i<4;i++)
        {
            materials[i]=Material("SurfaceGlint"+i,Color.Lerp(colors[i],Color.white,.65f),.65f);
            materials[i].EnableKeyword("_EMISSION");materials[i].SetColor("_EmissionColor",Color.Lerp(colors[i],Color.white,.5f)*3.2f);
            EditorUtility.SetDirty(materials[i]);
        }
        Action<Transform,Vector3,int> add=(anchor,offset,color)=>
        {
            var glint=MeshObject("SurfaceGlint"+glints.Count,group,Vector3.zero,mesh,materials[color]);
            var renderer=glint.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            glints.Add(glint.transform);anchors.Add(anchor);offsets.Add(offset);
        };
        for(int i=0;i<menu.homeButtons.Length;i++)add(menu.homeButtons[i].cap,new Vector3(.32f,.28f,-.85f),new[]{1,2,3,0}[i]);
        string letters="BUCA";
        for(int i=0;i<4;i++)add(root.Find("RaisedBucaSign/SolidLetter_"+letters[i]),new Vector3(i==3?.58f:.45f,.88f,-.08f),i);
        add(root.Find("ToyTable"),new Vector3(-6.05f,2.22f,-2.9f),0);
        add(root.Find("ToyRoomScenery/WoodenWallClock"),new Vector3(-1.05f,.92f,-.27f),2);
        animation.glints=glints.ToArray();animation.anchors=anchors.ToArray();animation.offsets=offsets.ToArray();
        animation.Sample(0);
    }

    static Mesh SurfaceGlintMesh()
    {
        var v=new List<Vector3>();var t=new List<int>();
        v.Add(new Vector3(0,0,-.09f));v.Add(new Vector3(0,0,.09f));
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;float r=i%2==0?1:.20f;
            v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0));
        }
        for(int i=0;i<8;i++)
        {
            int a=2+i,b=2+(i+1)%8;t.AddRange(new[]{0,b,a,1,a,b});
        }
        var mesh=new Mesh{name="SavedSurfaceGlint"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        return ToyBoxGeometry.Save(mesh,Root+"/Meshes/SavedSurfaceGlint.asset");
    }
}
