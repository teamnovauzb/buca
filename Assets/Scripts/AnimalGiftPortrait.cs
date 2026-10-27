using UnityEngine;
using UnityEngine.UI;

/// <summary>Renders the collectible toy itself inside the shared gift reveal.</summary>
public sealed class AnimalGiftPortrait : MonoBehaviour
{
    public GameObject passengerPrefab;
    public RawImage image;
    GameObject stage;
    RenderTexture texture;
    Transform paw;
    void OnEnable()
    {
        if(passengerPrefab==null||image==null)return;
        stage=new GameObject("Animal portrait stage");stage.transform.position=new Vector3(0,-1500,0);
        var animal=Instantiate(passengerPrefab,stage.transform);
        foreach(var t in animal.GetComponentsInChildren<Transform>())t.gameObject.layer=27;
        paw=animal.transform.Find("GreetingPaw");
        var camera=new GameObject("Portrait camera").AddComponent<Camera>();camera.transform.SetParent(stage.transform,false);
        camera.transform.localPosition=new Vector3(0,1.4f,-6);camera.transform.LookAt(stage.transform.position+new Vector3(0,1.3f,0));
        camera.orthographic=true;camera.orthographicSize=1.65f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<27;
        texture=new RenderTexture(512,640,24){antiAliasing=4};texture.Create();camera.targetTexture=texture;image.texture=texture;
        var light=new GameObject("Portrait light").AddComponent<Light>();light.transform.SetParent(stage.transform,false);light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-35,0);light.intensity=1.4f;light.cullingMask=1<<27;
    }
    void Update(){if(paw!=null)paw.localRotation=Quaternion.Euler(0,0,20+18*Mathf.Sin(Time.unscaledTime*7));}
    static void Release(Object item){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
    void OnDisable(){if(stage!=null)Release(stage);if(texture!=null){texture.Release();Release(texture);}if(image!=null)image.texture=null;}
}
