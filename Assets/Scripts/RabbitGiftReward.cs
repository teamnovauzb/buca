using System.Collections;
using UnityEngine;

/// <summary>Level-five shy-peek reward; all animation uses real time, including paused gameplay.</summary>
public sealed class RabbitGiftReward : MonoBehaviour
{
    public const float RevealDuration = 4.2f;
    public int rewardLevel = 5;
    public CanvasGroup fade, heading, controls;
    public RectTransform gift, lid, rabbit, leftEar, rightEar, paw, leftPaw;
    public UnityEngine.UI.Button continueButton;
    public AudioSource sound;
    public AudioClip revealSound;
    public AudioSource cheerSource;
    public AudioClip happyCheer;
    public RabbitGiftCelebration celebration;
    bool cheered;
    public UnityEngine.UI.RawImage artwork;
    public RectTransform ticketLayer;
    public bool WaitingForContinue { get; private set; }
    public float Elapsed { get; private set; }
    bool confirmed, released;
    float idleTime;
    bool ticketsReady;

    static float Ease(float a, float b, float t) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, t));
    static void Angle(RectTransform rect, float angle) => rect.localRotation = Quaternion.Euler(0, 0, angle);

    public void Sample(float t)
    {
        Elapsed = t;
        if(celebration!=null)celebration.Sample(t);
        if(!ticketsReady && ticketLayer!=null)
        {
            for(int i=0;i<ticketLayer.childCount;i++)
            {
                var badge=ticketLayer.GetChild(i).GetComponentInChildren<WorkshopStationBadge>(true);
                if(badge!=null){badge.SetLocked(i>rewardLevel);badge.SetState(false,false);}
                foreach(var label in ticketLayer.GetChild(i).GetComponentsInChildren<TMPro.TMP_Text>(true))
                    label.color=new Color(.025f,.09f,.22f);
            }
            ticketsReady=true;
        }
        fade.alpha = Ease(0, .35f, t);
        float shake = Mathf.Sin(t * 29) * (1-Ease(.45f, .95f, t)) * Ease(.25f,.4f,t);
        gift.anchoredPosition = new Vector2(shake * 4, -82 + 5 * Mathf.Abs(shake));
        Angle(gift, shake * 2.3f);
        float open = Ease(.85f, 1.4f, t);
        float away = Ease(1.75f, 2.6f, t);
        lid.anchoredPosition = new Vector2(-170 * away, 36 + 78 * open + 20 * away);
        Angle(lid, -6 * open + 41 * away);
        lid.localScale = Vector3.one * (1 - .13f * away);
        // The first small rise exposes only ears. The second exposes eyes, then the full greeting.
        float y = -470 + 105 * Ease(1.05f,1.5f,t) + 205 * Ease(1.65f,2.25f,t)
            + 130 * Ease(2.65f,3.35f,t);
        float settle = t > 3.35f ? Mathf.Sin((t-3.35f)*9)*Mathf.Exp(-(t-3.35f)*4)*5 : 0;
        rabbit.anchoredPosition = new Vector2(0, y + settle);
        Angle(rabbit, -5 * Ease(2.2f,2.5f,t) + 8 * Ease(2.7f,3.35f,t));
        Angle(leftEar, 6 * Mathf.Sin(t*5.3f) * Ease(1.1f,1.5f,t) * (1-.75f*Ease(2.6f,3.4f,t)));
        Angle(rightEar, -5 * Mathf.Sin(t*5.3f-1) * Ease(1.2f,1.6f,t));
        float wave = Ease(3.1f,3.35f,t);
        Angle(paw, -25 + wave*(22+14*Mathf.Sin((t-3.1f)*9)));
        Angle(leftPaw, 24);
        heading.alpha = Ease(2.7f,3.4f,t);
        controls.alpha = Ease(3.7f,RevealDuration,t);
        controls.interactable = controls.blocksRaycasts = WaitingForContinue;
        if(ticketLayer!=null && artwork!=null)
            ticketLayer.localScale=new Vector3(artwork.rectTransform.rect.width/1672f, artwork.rectTransform.rect.height/941f,1);
    }

    public IEnumerator Play()
    {
        confirmed=false; released=false; WaitingForContinue=false; cheered=false;
        gameObject.SetActive(true);
        continueButton.onClick.AddListener(Confirm);
        Sample(0);
        var audio=AudioManager.Instance;
        if(sound!=null && revealSound!=null)
        {
            sound.volume=audio!=null?audio.masterVolume*audio.sfxVolume*.7f:.7f;
            if(audio!=null)sound.outputAudioMixerGroup=audio.sfxGroup;
            sound.clip=revealSound; sound.Play();
        }
        float t=0;
        while(t<RevealDuration) { Sample(t); PlayCheer(t); t+=Time.unscaledDeltaTime; yield return null; }
        WaitingForContinue=true;
        idleTime=0;
        Sample(RevealDuration);
        PlayCheer(RevealDuration);
        while(!confirmed) { idleTime+=Time.unscaledDeltaTime; Sample(RevealDuration+idleTime); yield return null; }
        WaitingForContinue=false;
        controls.interactable=controls.blocksRaycasts=false;
        for(float exit=0;exit<.3f;exit+=Time.unscaledDeltaTime)
        { fade.alpha=1-Mathf.SmoothStep(0,1,exit/.3f); yield return null; }
        continueButton.onClick.RemoveListener(Confirm);
        gameObject.SetActive(false);
    }

    void PlayCheer(float t)
    {
        if(cheered || t<RabbitGiftCelebration.StartSeconds)return;
        cheered=true;
        if(cheerSource==null || happyCheer==null)return;
        var audio=AudioManager.Instance;
        cheerSource.volume=(audio!=null?audio.masterVolume*audio.sfxVolume:1)*.65f;
        if(audio!=null)cheerSource.outputAudioMixerGroup=audio.sfxGroup;
        cheerSource.PlayOneShot(happyCheer);
    }

    public void Confirm() { if(WaitingForContinue && released) confirmed=true; }
    void Update()
    {
        if(!WaitingForContinue)return;
        bool held=Input.GetMouseButton(0)||Input.GetKey(KeyCode.Return)||Input.GetKey(KeyCode.Space)
            ||ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Black);
        if(!released) { if(!held)released=true; return; }
        if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space)||ArcadeInputAdapter.ConfirmDown())Confirm();
    }
    void OnDisable()
    {
        WaitingForContinue=false;
        if(sound!=null)sound.Stop();
        if(cheerSource!=null)cheerSource.Stop();
        if(continueButton!=null)continueButton.onClick.RemoveListener(Confirm);
    }
}
