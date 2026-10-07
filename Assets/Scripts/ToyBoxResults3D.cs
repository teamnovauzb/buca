using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed partial class ToyBoxResults3D : MonoBehaviour
{
    public RenderTexture levelCapture,blurScratch,blurredLevel;
    public Material blurMaterial;
    public Transform levelBackdrop;
    public GameObject presentation;
    public Camera resultsCamera;
    public Transform leftBoard,nextRoot,nextCap,podiumBase,roomRoot;
    public Transform baseBranding;
    public ToyBoxPodiumLabel[] solidLabels;
    public Mesh[] digitMeshes;
    public MeshFilter[] totalDigits;
    public Vector3 nextRest;
    public Collider nextHit;
    public GameObject selectedRing;
    public TMP_Text[] scoreValues;
    public GameObject[] earnedStars;
    public TMP_Text countdownText,comboText;
    public ToyBoxWellDone3D wellDone;
    public GameObject scorecardShortcut;
    bool normalResult;
    public bool ShowingWellDone => normalResult && wellDone!=null;
    public Collider ActiveNextHit => ShowingWellDone ? wellDone.nextHit : nextHit;
    public bool IsShowing { get; private set; }
    public float Remaining { get; private set; }
    Camera _gameCamera;
    bool _cameraEnabled,_neutral;
    Action _finished;
    float _press, _nextPage;
    float entrance, nextScoreTick;
    const float EntranceDuration=1.8f;
    int finalTotal, shownTotal=-1;
    Vector3[] starScales;
    Quaternion[] starRotations;
    void SampleEntrance()
    {
        float t=Mathf.Clamp01(entrance/.75f);
        float settle=1f-Mathf.Pow(1f-t,3f);
        float bounce=Mathf.Sin(t*Mathf.PI)*.055f;
        leftBoard.localPosition=Vector3.down*(1f-settle)*2.2f;
        leftBoard.localScale=Vector3.one*(.82f+.18f*settle+bounce);
        leftBoard.localRotation=Quaternion.Euler((1f-settle)*-14f,0,(1f-settle)*-3f);
        if(ShowingWellDone)wellDone.SampleEntrance(entrance);
        for(int i=0;i<earnedStars.Length;i++)
        {
            float k=Mathf.Clamp01((entrance-.45f-i*.17f)/.42f);
            float pop=1f-Mathf.Pow(1f-k,3f)+Mathf.Sin(k*Mathf.PI)*.18f;
            earnedStars[i].transform.localScale=starScales[i]*Mathf.Max(.001f,pop);
            earnedStars[i].transform.localRotation=starRotations[i]*Quaternion.Euler(0,(1f-k)*75f,(1f-k)*-20f);
        }
        int total=Mathf.RoundToInt(finalTotal*Mathf.SmoothStep(0,1,Mathf.Clamp01((entrance-.7f)/1.1f)));
        if(total!=shownTotal){
            if(total>shownTotal && shownTotal>=0 && entrance>=nextScoreTick && AudioManager.Instance!=null)
            {
                AudioManager.Instance.PlaySfx(AudioManager.Instance.scoreCountTickSfx,.85f,Mathf.Lerp(.95f,1.3f,Mathf.Clamp01((entrance-.7f)/1.1f)));
                nextScoreTick=entrance+.09f;
            }
            shownTotal=total;SetSolidTotal(total);scoreValues[4].text=total.ToString("N0");RefreshLabels();}
    }
    public GameObject regularTitle, holeInOneTitle, scorecardRoot;
    public Collider scorecardHit;
    public TMP_Text golfSummary, scorecardTitle, scorecardFooter;
    public TMP_Text[] scorecardRows;
    int cardPage;
    public bool ScorecardShowing => scorecardRoot!=null && scorecardRoot.activeSelf;

    public void ToggleScorecard()
    {
        if(!IsShowing || _press>0 || scorecardRoot==null) return;
        bool showing=!ScorecardShowing;
        scorecardRoot.SetActive(showing); SetResultVisible(!showing);
        if(showing) { celebrationTime=6f; StopCelebration(); }
        SampleCelebration(celebrationTime);
        Remaining=5; RefreshScorecard(); RefreshLabels();
        if(AudioManager.Instance!=null) AudioManager.Instance.PlayButtonClick();
    }
    public void TurnScorecardPage(int direction)
    {
        cardPage=(cardPage+direction+5)%5; RefreshScorecard(); RefreshLabels();
    }
    void RefreshScorecard()
    {
        if(scorecardRows==null) return;
        var manager=LevelManager.Instance;
        scorecardTitle.text="MY SHOTS - HOLES "+(cardPage*6+1)+" TO "+(cardPage*6+6);
        for(int row=0;row<scorecardRows.Length;row++)
        {
            int index=cardPage*6+row;
            int shots=0,par=0;
            if(manager!=null) foreach(var entry in manager.CampaignScorecard)
                if(entry.holeNumber==index+1) { shots=entry.strokes; par=entry.par; }
            int best=PlayerPrefs.GetInt(LevelManager.PrefLevelBestStrokes+index,0);
            string result=shots>0?shots+(shots==1?" SHOT":" SHOTS"):"NOT PLAYED THIS RUN";
            scorecardRows[row].text=(index+1).ToString("00")+"  "+result+"  BEST "+(best>0?best.ToString():"-");
        }
        scorecardFooter.text="PERSONAL BEST - FEWER SHOTS IS BETTER";
    }

    public void Show(ScoreCalculator.ScoreBreakdown score,Action finished)
    {
        if(IsShowing) Hide(false);
        _finished=finished; IsShowing=true;
        _gameCamera=Camera.main; _cameraEnabled=_gameCamera!=null && _gameCamera.enabled;
        CaptureLevelBackdrop();
        if(_gameCamera!=null) _gameCamera.enabled=false;
        presentation.SetActive(true);
        scorecardRoot.SetActive(false);
        cardPage=LevelManager.Instance!=null?(LevelManager.Instance.CurrentLevelNumber-1)/6:0;
        bool oneShot=score.strokesUsed==1;
        normalResult=!oneShot && wellDone!=null;
        SetResultVisible(true);
        if(ShowingWellDone)wellDone.Prepare(score.strokesUsed);
        regularTitle.SetActive(!oneShot); holeInOneTitle.SetActive(oneShot);
        golfSummary.text=score.strokesUsed+(score.strokesUsed==1?" SHOT":" SHOTS");
        Remaining=5; _press=0; _neutral=true;
        int[] values={score.basePoints,score.timeBonus,score.railBonus,score.strokeBonus,score.total};
        for(int i=0;i<values.Length;i++) scoreValues[i].text=values[i].ToString("N0");
        SetSolidTotal(score.total);
        for(int i=0;i<earnedStars.Length;i++) earnedStars[i].SetActive(i<score.stars);
        comboText.text="";
        nextCap.localPosition=nextRest; selectedRing.SetActive(true);
        if(starScales==null){starScales=new Vector3[earnedStars.Length];starRotations=new Quaternion[earnedStars.Length];
            for(int i=0;i<earnedStars.Length;i++){starScales[i]=earnedStars[i].transform.localScale;starRotations[i]=earnedStars[i].transform.localRotation;}}
        finalTotal=score.total;shownTotal=-1;entrance=0;nextScoreTick=.7f;
        Fit(); UpdateCountdown(); SampleEntrance();
        BeginCelebration(oneShot);
    }

    void CaptureLevelBackdrop()
    {
        if(_gameCamera==null) return;
        // Render into Editor-saved targets before enabling the results stage.
        // Freeze the real completed level once; no duplicate board or room is generated.
        var hud=UnityEngine.Object.FindAnyObjectByType<ToyBoxGameplayHud>();
        bool hudActive=hud!=null && hud.gameObject.activeSelf;
        Matrix4x4 projection=_gameCamera.projectionMatrix;
        _gameCamera.ResetProjectionMatrix();
        bool customProjection=projection!=_gameCamera.projectionMatrix;
        var previousTarget=_gameCamera.targetTexture;
        var previousActive=RenderTexture.active;
        try
        {
            if(hudActive) hud.gameObject.SetActive(false);
            _gameCamera.projectionMatrix=projection;
            var request=new UniversalRenderPipeline.SingleCameraRequest {destination=levelCapture};
            if(RenderPipeline.SupportsRenderRequest(_gameCamera,request)) RenderPipeline.SubmitRenderRequest(_gameCamera,request);
            else { _gameCamera.targetTexture=levelCapture; _gameCamera.Render(); }
            blurMaterial.SetVector("_BlurDirection",new Vector4(3,0,0,0));
            Graphics.Blit(levelCapture,blurScratch,blurMaterial);
            blurMaterial.SetVector("_BlurDirection",new Vector4(0,3,0,0));
            Graphics.Blit(blurScratch,blurredLevel,blurMaterial);
            for(int i=0;i<2;i++)
            {
                blurMaterial.SetVector("_BlurDirection",new Vector4(3,0,0,0)); Graphics.Blit(blurredLevel,blurScratch,blurMaterial);
                blurMaterial.SetVector("_BlurDirection",new Vector4(0,3,0,0)); Graphics.Blit(blurScratch,blurredLevel,blurMaterial);
            }
        }
        finally
        {
            _gameCamera.targetTexture=previousTarget;
            if(customProjection) _gameCamera.projectionMatrix=projection; else _gameCamera.ResetProjectionMatrix();
            RenderTexture.active=previousActive;
            if(hudActive) hud.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if(!IsShowing) return;
        Fit();
        if(entrance<EntranceDuration) SampleEntrance();
        bool cardClick=Input.GetMouseButtonDown(0) && scorecardHit.Raycast(resultsCamera.ScreenPointToRay(Input.mousePosition),out var cardHit,100);
        if(!_neutral && (cardClick || Input.GetKeyDown(KeyCode.Tab) || ArcadeInputAdapter.GetButtonDown(ArcadeInputAdapter.Button.Green)))
        { ToggleScorecard(); return; }
        if(ScorecardShowing)
        {
            if(ArcadeInputAdapter.ConfirmDown() || ArcadeInputAdapter.CancelDown() || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape)) { ToggleScorecard(); return; }
            var stick=ArcadeInputAdapter.GetStick();
            if(Input.GetKey(KeyCode.RightArrow)) stick.x=1;
            if(Input.GetKey(KeyCode.LeftArrow)) stick.x=-1;
            if(stick.sqrMagnitude<.2f) _nextPage=0;
            else if(Time.unscaledTime>=_nextPage)
            { TurnScorecardPage(Mathf.Abs(stick.x)>Mathf.Abs(stick.y)?(stick.x>0?1:-1):(stick.y<0?1:-1)); _nextPage=Time.unscaledTime+.3f; }
            return;
        }
        bool held=Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Return)
            || ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black);
        if(_neutral) { if(!held) _neutral=false; }
        else if(_press<=0)
        {
            bool click=Input.GetMouseButtonDown(0) && ActiveNextHit.Raycast(resultsCamera.ScreenPointToRay(Input.mousePosition),out var hit,100);
            if(click || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || ArcadeInputAdapter.ConfirmDown()) PressContinue();
        }
        Tick(Time.unscaledDeltaTime);
    }
    public void PressContinue()
    {
        if(!IsShowing || _press>0) return;
        _press=.16f; nextCap.localPosition=nextRest+Vector3.down*.12f;
        if(ShowingWellDone)wellDone.PressNext();
        if(AudioManager.Instance!=null) AudioManager.Instance.PlayButtonClick();
    }
    public void Tick(float delta)
    {
        if(!IsShowing || ScorecardShowing) return;
        TickCelebration(delta);
        if(_press>0) { _press-=delta; if(_press<=0) Hide(true); return; }
        if(entrance<EntranceDuration)
        {
            float step=Mathf.Min(delta,EntranceDuration-entrance);entrance+=step;delta-=step;
            SampleEntrance();
            if(entrance<EntranceDuration)return;
        }
        Remaining=Mathf.Max(0,Remaining-delta); UpdateCountdown();
        if(Remaining<=0) Hide(true);
    }
    void SetSolidTotal(int total)
    {
        if(ShowingWellDone)wellDone.SetTotal(total);
        string value=Mathf.Max(0,total).ToString();
        float height=Mathf.Min(.91f,4.25f/(value.Length*.85f));
        for(int i=0;i<totalDigits.Length;i++)
        {
            bool visible=i<value.Length; totalDigits[i].gameObject.SetActive(visible);
            if(!visible) continue;
            totalDigits[i].sharedMesh=digitMeshes[value[i]-'0'];
            totalDigits[i].transform.localScale=Vector3.one*height;
            totalDigits[i].transform.localPosition=Vector3.right*((i-(value.Length-1)*.5f)*height*.85f);
        }
    }
    void RefreshLabels() { foreach(var label in solidLabels) label.Refresh(); }
    void UpdateCountdown() { countdownText.text="IN "+Mathf.Max(1,Mathf.CeilToInt(Remaining)); if(ShowingWellDone)wellDone.SetCountdown(Remaining); RefreshLabels(); }
    void SetResultVisible(bool visible)
    {
        leftBoard.gameObject.SetActive(visible && !ShowingWellDone);
        nextRoot.gameObject.SetActive(visible && !ShowingWellDone);
        podiumBase.gameObject.SetActive(visible && !ShowingWellDone);
        if(wellDone!=null)wellDone.gameObject.SetActive(visible && ShowingWellDone);
        if(scorecardShortcut!=null)scorecardShortcut.SetActive(!ShowingWellDone);
    }
    public void Fit()
    {
        float aspect=Mathf.Max(.2f,resultsCamera.aspect);
        baseBranding.gameObject.SetActive(false);
        leftBoard.localPosition=Vector3.zero;
        nextRoot.localPosition=new Vector3(0,-4.28f,-1.02f);
        podiumBase.localPosition=new Vector3(0,-4.3f,0);
        podiumBase.localScale=Vector3.one;
        float halfHeight=6.0f;
        float halfWidth=ShowingWellDone && !ScorecardShowing?6.1f:3.9f;
        float distance=Mathf.Max(halfHeight,halfWidth/aspect)/Mathf.Tan(resultsCamera.fieldOfView*.5f*Mathf.Deg2Rad)+1.5f;
        resultsCamera.transform.localPosition=new Vector3(0,distance*.045f,-distance);
        resultsCamera.transform.localRotation=Quaternion.Euler(Mathf.Atan(.045f)*Mathf.Rad2Deg,0,0);
        float backdropHeight=40*Mathf.Tan(resultsCamera.fieldOfView*.5f*Mathf.Deg2Rad);
        levelBackdrop.localScale=new Vector3(backdropHeight*aspect*1.01f,backdropHeight*1.01f,1);
    }
    public void Hide(bool continueGame)
    {
        if(!IsShowing) return;
        IsShowing=false; presentation.SetActive(false);
        StopCelebration();
        if(_gameCamera!=null) _gameCamera.enabled=_cameraEnabled;
        var callback=_finished; _finished=null;
        if(continueGame) callback?.Invoke();
    }
    void OnDisable() { Hide(false); }
}
