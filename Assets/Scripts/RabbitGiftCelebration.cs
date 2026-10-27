using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Saved, shaded 3D stars and curled ribbons composited behind the rabbit.</summary>
public sealed class RabbitGiftCelebration : MonoBehaviour
{
    public const float StartSeconds=3.05f;
    public Transform stage;
    public Camera effectCamera;
    public UnityEngine.UI.RawImage display;
    public Transform[] pieces;
    public bool continuousWaves;
    public int starCount=16;
    RenderTexture texture;
    static float Seed(int i,float salt)=>Mathf.Repeat(Mathf.Sin(i*127.1f+salt*311.7f)*43758.5453f,1);
    void Initialize()
    {
        if(texture!=null||stage==null)return;
        stage.SetParent(null,false);stage.position=new Vector3(0,-2800,0);stage.localScale=Vector3.one;
        effectCamera.scene=gameObject.scene;
        texture=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){name="Rabbit3DCelebration",antiAliasing=4};
        texture.Create();effectCamera.targetTexture=texture;display.texture=texture;
    }
    public void Sample(float seconds)
    {
        if(seconds<StartSeconds)
        {
            if(display!=null)display.enabled=false;
            if(stage!=null)stage.gameObject.SetActive(false);
            return;
        }
        Initialize();if(texture==null)return;
        float elapsed=seconds-StartSeconds;
        bool visible=elapsed>=0&&elapsed<(continuousWaves?17f:8.5f);
        display.enabled=visible;stage.gameObject.SetActive(visible);
        if(!visible)return;
        for(int i=0;i<pieces.Length;i++)
        {
            bool star=i<starCount;
            float seed=Seed(i,1),spread=Seed(i,2);
            bool shower=i>=100;
            float t=elapsed-seed*(shower?1.1f:.45f);
            float life=shower?6.5f:4.8f;
            if(continuousWaves)
            {
                // Staggered relaunches maintain a shower instead of resetting the whole screen.
                float delay=seed*(shower?2.5f:1.4f);
                t=elapsed-delay;
                if(t>=0)t=Mathf.Repeat(t,life);
            }
            var piece=pieces[i];piece.gameObject.SetActive(t>=0&&t<life);
            if(t<0||t>=life)continue;
            float side=i%2==0?-1:1;
            float x,y;
            if(shower)
            {
                x=-9.3f+18.6f*spread+Mathf.Sin(t*1.8f+i)*.45f;
                y=6.5f-Seed(i,3)*10.5f-(.65f+seed*.9f)*t-.11f*t*t;
            }
            else
            {
                x=side*(8.7f-(2.2f+spread*4.5f)*t)+Mathf.Sin(t*2+i)*.25f;
                y=-4.9f+(7.2f+seed*3.8f)*t-2.3f*t*t;
            }
            float z=-2.3f+spread*4.6f+Mathf.Sin(t*1.4f+i)*.4f;
            piece.localPosition=new Vector3(x,y,z);
            piece.localRotation=Quaternion.Euler(15+Mathf.Sin(t*2+i)*36,i*43+t*(star?82:120),i*27+t*(star?45:75));
            float envelope=Mathf.SmoothStep(0,1,t/.16f)*(1-Mathf.SmoothStep(0,1,(t-life+.6f)/.6f));
            if(continuousWaves)envelope*=1-Mathf.SmoothStep(0,1,(elapsed-14f)/3f);
            float size=(star?.25f+seed*.14f:.26f+seed*.23f)*envelope;
            piece.localScale=Vector3.one*size;
        }
        RenderPipeline.SubmitRenderRequest(effectCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=texture});
    }
    void OnDisable(){if(display!=null)display.enabled=false;if(stage!=null)stage.gameObject.SetActive(false);}
    void OnDestroy()
    {
        if(effectCamera!=null)effectCamera.targetTexture=null;
        if(texture!=null){texture.Release();if(Application.isPlaying)Destroy(texture);else DestroyImmediate(texture);}
        if(stage!=null){if(Application.isPlaying)Destroy(stage.gameObject);else DestroyImmediate(stage.gameObject);}
    }
}
