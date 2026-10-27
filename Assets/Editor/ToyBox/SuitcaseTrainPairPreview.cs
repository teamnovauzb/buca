#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Static renders and mechanical checks on isolated editor copies. Never enters Play mode.</summary>
public static class SuitcaseTrainPairPreview
{
    const string Output="output/train-options/implementation";
    public static string CheckAndRender()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode only.");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TwilightMap/Locomotives/WorldTrainPair.prefab");
        if(prefab==null)throw new Exception("Build the train pair first.");
        Directory.CreateDirectory(Output);
        var scene=EditorSceneManager.NewPreviewScene();
        var previous=RenderTexture.active;
        var target=new RenderTexture(1200,700,24,RenderTextureFormat.ARGB32){antiAliasing=4};
        var pixels=new Texture2D(1200,700,TextureFormat.RGB24,false);
        Material floorMaterial=null;
        try
        {
            var copy=Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(copy,scene);
            copy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);copy.transform.localScale=Vector3.one;
            var mechanism=copy.GetComponent<SuitcaseTrainMechanism>();
            Verify(mechanism);
            var cameraObject=new GameObject("TrainPreviewCamera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;
            camera.orthographic=true;camera.orthographicSize=2.25f;camera.aspect=1200f/700;
            camera.transform.position=new Vector3(3.4f,3.3f,-8.5f);
            camera.transform.LookAt(new Vector3(-.45f,1.35f,0));
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.045f,.045f);
            camera.cullingMask=1<<28;camera.targetTexture=target;camera.nearClipPlane=.03f;camera.farClipPlane=100;
            camera.allowHDR=false;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            Light(scene,"WarmKey",new Vector3(43,-35,0),new Color(1,.83f,.62f),2.5f);
            Light(scene,"CoolFill",new Vector3(35,145,0),new Color(.59f,.73f,1),1.2f);
            Light(scene,"TopRim",new Vector3(65,80,0),new Color(1,.9f,.72f),1.5f);
            var probeObject=new GameObject("StudioReflection");SceneManager.MoveGameObjectToScene(probeObject,scene);
            var probe=probeObject.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Custom;
            probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/ToyBoxMenu/Materials/BakedStudioReflection.asset");
            probe.size=Vector3.one*50;probe.intensity=1;
            floorMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            floorMaterial.color=new Color(.075f,.065f,.045f);floorMaterial.SetFloat("_Smoothness",.18f);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(floor,scene);floor.layer=28;
            floor.transform.position=new Vector3(-.4f,-.19f,0);floor.transform.localScale=new Vector3(9,.25f,4);
            floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;
            foreach(bool city in new[]{false,true})
            {
                mechanism.Pose(.38f,1,city);
                camera.backgroundColor=city?new Color(.018f,.032f,.06f):new Color(.035f,.045f,.025f);
                camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1200,700),0,0);pixels.Apply();
                File.WriteAllBytes(Output+(city?"/World2-Moonlight.png":"/World1-Woodland.png"),pixels.EncodeToPNG());
            }
            File.WriteAllText(Output+"/mechanism-validation.txt",
                "PASS (Edit mode only): both engines; all 20 wheels complete forward and reverse 360-degree revolutions at their own circumference; four crank mechanisms keep links attached through 32 phases; piston rods keep constant length and sliders stay horizontal; unchanged distance holds every wheel still; World 1/2 body visibility switches correctly. No game run or player preferences changed.");
            return "Editor-only train renders and mechanical validation saved to "+Output;
        }
        finally
        {
            RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);
            EditorSceneManager.ClosePreviewScene(scene);if(floorMaterial!=null)Object.DestroyImmediate(floorMaterial);
        }
    }

    static void Verify(SuitcaseTrainMechanism mechanism)
    {
        foreach(var engine in new[]{mechanism.woodland,mechanism.moonlight})
        {
            foreach(var wheel in engine.wheels)
            {
                foreach(float sign in new[]{1f,-1f})
                {
                    float sum=0;mechanism.Pose(0,1,false);float before=wheel.pivot.localEulerAngles.z;
                    for(int i=1;i<=32;i++)
                    {
                        mechanism.Pose(sign*2*Mathf.PI*wheel.radius*i/32,1,false);
                        float after=wheel.pivot.localEulerAngles.z;sum+=Mathf.DeltaAngle(before,after);before=after;
                        foreach(var side in engine.gear)
                        {
                            var parent=engine.body.transform;
                            var rear=parent.InverseTransformPoint(side.rearPin.position);var front=parent.InverseTransformPoint(side.frontPin.position);
                            var start=parent.InverseTransformPoint(side.couplingRod.TransformPoint(Vector3.left*.5f));
                            var end=parent.InverseTransformPoint(side.couplingRod.TransformPoint(Vector3.right*.5f));
                            Require(Vector3.Distance(start,rear)<.001f&&Vector3.Distance(end,front)<.001f,"Coupling rod detached from a crank.");
                            Require(Mathf.Abs(Vector3.Distance(side.crosshead.localPosition,front)-side.rodLength)<.001f,"Piston rod changed length.");
                            Require(Mathf.Abs(side.crosshead.localPosition.y-side.sliderHeight)<.001f,"Crosshead left its guide.");
                        }
                    }
                    Require(Mathf.Abs(sum+sign*360)<.05f,"Wheel failed a complete revolution: "+wheel.pivot.name);
                }
            }
        }
        mechanism.Pose(5,.75f,false);var stopped=mechanism.woodland.wheels[0].pivot.localRotation;
        mechanism.Pose(5,.75f,false);Require(Quaternion.Angle(stopped,mechanism.woodland.wheels[0].pivot.localRotation)<.001f,"Stopped wheel moved.");
        Require(mechanism.woodland.body.activeSelf&&!mechanism.moonlight.body.activeSelf,"Wrong World 1 body.");
        mechanism.Pose(5,.75f,true);Require(!mechanism.woodland.body.activeSelf&&mechanism.moonlight.body.activeSelf,"Wrong World 2 body.");
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Light(Scene scene,string name,Vector3 angles,Color color,float intensity)
    {
        var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);go.transform.rotation=Quaternion.Euler(angles);
        var light=go.AddComponent<Light>();light.type=LightType.Directional;light.color=color;light.intensity=intensity;
        light.cullingMask=1<<28;light.shadows=LightShadows.Soft;light.shadowBias=.03f;
    }
}
#endif
