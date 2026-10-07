using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Animates editor-authored hanging signs; uses the existing continue/end callbacks.</summary>
public sealed class HangingTimeUp3D : MonoBehaviour
{
    public RenderTexture levelCapture, blurScratch, blurredLevel;
    public Material blurMaterial;
    public Transform backdrop;
    public GameObject presentation;
    public Transform header, continueSign, endSign;
    public GameObject[] selectors;
    public Collider[] buttons;
    public Camera gameplayCamera;
    public LevelFailedPanel owner;
    public bool IsShowing { get; private set; }
    public bool Ready => IsShowing && elapsed>=1.25f;
    float elapsed, nextMove;
    int selected;
    bool awaitRelease;
    Vector3[] rests;
    void Awake(){Cache(); presentation.SetActive(false);}
    void Cache(){if(rests==null) rests=new[]{header.localPosition,continueSign.localPosition,endSign.localPosition};}
    public void Show()
    {
        Cache(); elapsed=0; selected=0; nextMove=0;
        awaitRelease=ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black)||Input.GetMouseButton(0);
        CaptureBackground();
        IsShowing=true; presentation.SetActive(true); Fit(); SampleReveal(0); Select(0);
    }
    void CaptureBackground()
    {
        presentation.SetActive(false);
        var target=gameplayCamera.targetTexture;
        var active=RenderTexture.active;
        var projection=gameplayCamera.projectionMatrix;
        gameplayCamera.ResetProjectionMatrix();
        bool customProjection=projection!=gameplayCamera.projectionMatrix;
        try
        {
            gameplayCamera.projectionMatrix=projection;
            var request=new UniversalRenderPipeline.SingleCameraRequest {destination=levelCapture};
            if(RenderPipeline.SupportsRenderRequest(gameplayCamera,request)) RenderPipeline.SubmitRenderRequest(gameplayCamera,request);
            else {gameplayCamera.targetTexture=levelCapture;gameplayCamera.Render();}
            for(int i=0;i<4;i++)
            {
                blurMaterial.SetVector("_BlurDirection",new Vector4(4,0,0,0));
                Graphics.Blit(i==0?levelCapture:blurredLevel,blurScratch,blurMaterial);
                blurMaterial.SetVector("_BlurDirection",new Vector4(0,4,0,0));
                Graphics.Blit(blurScratch,blurredLevel,blurMaterial);
            }
        }
        finally
        {
            gameplayCamera.targetTexture=target;
            if(customProjection)gameplayCamera.projectionMatrix=projection;
            else gameplayCamera.ResetProjectionMatrix();
            RenderTexture.active=active;
        }
    }
    public void Hide(){IsShowing=false; if(presentation!=null) presentation.SetActive(false);}
    void Fit()
    {
        transform.SetParent(gameplayCamera.transform,false);
        transform.localPosition=new Vector3(0,0,3);
        transform.localRotation=Quaternion.identity;
        float height=gameplayCamera.orthographic?gameplayCamera.orthographicSize*2:6*Mathf.Tan(gameplayCamera.fieldOfView*Mathf.Deg2Rad*.5f);
        transform.localScale=Vector3.one*Mathf.Min(height/8.7f,height*gameplayCamera.aspect/8.7f);
        if(backdrop!=null)
        {
            float distance=3+2*transform.localScale.x;
            float h=gameplayCamera.orthographic?height:height*distance/3;
            backdrop.localScale=new Vector3(h*gameplayCamera.aspect/transform.localScale.x*2f,h/transform.localScale.x*2f,1);
        }
    }
    public void SampleReveal(float time)
    {
        Cache(); elapsed=Mathf.Max(0,time);
        Transform[] signs={header,continueSign,endSign};
        float t=Mathf.Clamp01(elapsed/.90f);
        float u=1-t;
        float drop=9*u*u*u;
        // Connected boards travel together: independent vertical delays made
        // their faces intersect and obscured the labels during the entrance.
        for(int i=0;i<3;i++)
        {
            float swing=Mathf.Sin(t*Mathf.PI*3-i*.12f)*Mathf.Sin(t*Mathf.PI)*(1-t)*3;
            signs[i].localPosition=rests[i]+Vector3.up*drop;
            signs[i].localRotation=Quaternion.Euler(0,0,swing);
        }
    }
    public void Select(int index){selected=Mathf.Clamp(index,0,1);for(int i=0;i<selectors.Length;i++)selectors[i].SetActive(i==selected);}
    void Update()
    {
        if(!IsShowing)return;
        Fit(); SampleReveal(elapsed+Time.unscaledDeltaTime);
        if(!Ready)return;
        if(awaitRelease)
        {
            if(!ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black)&&!Input.GetMouseButton(0))awaitRelease=false;
            return;
        }
        var stick=ArcadeInputAdapter.GetStick();
        if(stick.sqrMagnitude<.1f)nextMove=0;
        else if(Time.unscaledTime>=nextMove){Select(stick.y>0?0:1);nextMove=Time.unscaledTime+.25f;}
        if(Input.GetKeyDown(KeyCode.UpArrow))Select(0);
        if(Input.GetKeyDown(KeyCode.DownArrow))Select(1);
        if(Input.GetMouseButtonDown(0))
        {
            var ray=gameplayCamera.ScreenPointToRay(Input.mousePosition);
            for(int i=0;i<buttons.Length;i++)if(buttons[i].Raycast(ray,out var hit,20)){Select(i);Activate();return;}
        }
        if(ArcadeInputAdapter.ConfirmDown()||Input.GetKeyDown(KeyCode.Return))Activate();
        else if(ArcadeInputAdapter.CancelDown()||Input.GetKeyDown(KeyCode.Escape))owner.EndFromHanging();
    }
    public void Activate(){if(!Ready)return;if(selected==0)owner.ContinueFromHanging();else owner.EndFromHanging();}
    void OnDisable(){Hide();}
}
