using UnityEngine;

public sealed partial class WorldStationRoute3D
{
    Light _transitionKey;
    Color _keyColor;
    float _keyIntensity;
    bool _transitionCached;

    void SetWorldEnvironment(bool secondWorld)
    {
        if(worldOneRoom!=null)worldOneRoom.SetActive(!secondWorld);
        if(worldTwoCity!=null)worldTwoCity.SetActive(secondWorld);
        if(worldOneLabel!=null)worldOneLabel.SetActive(!secondWorld);
        if(worldTwoLabel!=null)worldTwoLabel.SetActive(secondWorld);
    }
    void CacheTransition()
    {
        if(_transitionCached)return;
        var key=transform.Find("CozyStationRoom/EveningArchitecturalKey");
        if(key!=null)_transitionKey=key.GetComponent<Light>();
        if(_transitionKey!=null){_keyColor=_transitionKey.color;_keyIntensity=_transitionKey.intensity;}
        _transitionCached=true;
    }
    static float CinematicEase(float t){t=Mathf.Clamp01(t);return t*t*t*(t*(t*6-15)+10);}
    public void SampleTransition(float seconds)
    {
        CacheTransition();
        // Full-screen dissolve conceals the world swap; no tray or camera transforms change.
        float cover=CinematicEase((seconds-4.15f)/.50f)*(1-CinematicEase((seconds-5f)/1.15f));
        if(transitionCurtain!=null)transitionCurtain.color=new Color(.025f,.045f,.085f,cover);
        if(seconds>=4.8f&&!BonusVisible)ShowBonus();
        float lightBlend=CinematicEase((seconds-4.8f)/1.35f);
        if(_transitionKey!=null)
        {
            _transitionKey.color=Color.Lerp(_keyColor,new Color(.80f,.88f,1f),lightBlend);
            _transitionKey.intensity=_keyIntensity*Mathf.Lerp(1,1.12f,lightBlend);
        }
    }
    void FinishTransition()
    {
        if(transitionCurtain!=null)transitionCurtain.color=new Color(.025f,.045f,.085f,0);
        _neutral=true;
    }
    void RestoreTransition()
    {
        FinishTransition();
        if(_transitionCached&&_transitionKey!=null){_transitionKey.color=_keyColor;_transitionKey.intensity=_keyIntensity;}
    }
}
