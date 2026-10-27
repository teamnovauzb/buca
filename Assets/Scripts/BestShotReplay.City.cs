using UnityEngine;

public sealed partial class BestShotReplay
{
    public Renderer[] cityFrameParts;
    public Material[] originalFrameMaterials, cityFrameMaterials;
    public Renderer videoScreen;
    MaterialPropertyBlock replayDisplayProperties, backdropProperties;
    Color originalReplaySky;
    bool replaySkyCaptured;
    void ApplyReplayTheme()
    {
        bool city=LevelManager.Instance!=null && LevelManager.Instance.CurrentLevelNumber>30;
        if(!replaySkyCaptured){originalReplaySky=playbackCamera.backgroundColor;replaySkyCaptured=true;}
        bool cityRecording=BestLevelNumber>30;
        playbackCamera.backgroundColor=cityRecording?new Color(.018f,.027f,.046f):originalReplaySky;
        if(videoScreen!=null)
        {
            if(replayDisplayProperties==null)replayDisplayProperties=new MaterialPropertyBlock();
            videoScreen.GetPropertyBlock(replayDisplayProperties);
            replayDisplayProperties.SetFloat("_Exposure",cityRecording?1.2f:1f);
            replayDisplayProperties.SetFloat("_ToneMap",cityRecording?1f:0f);
            videoScreen.SetPropertyBlock(replayDisplayProperties);
        }
        if(backdrop!=null && backdrop.TryGetComponent<Renderer>(out var backdropRenderer))
        {
            if(backdropProperties==null)backdropProperties=new MaterialPropertyBlock();
            backdropRenderer.GetPropertyBlock(backdropProperties);
            backdropProperties.SetFloat("_Brightness",city?.32f:.78f);
            backdropRenderer.SetPropertyBlock(backdropProperties);
        }
        if(cityFrameParts==null)return;
        for(int i=0;i<cityFrameParts.Length;i++)
            if(cityFrameParts[i]!=null)cityFrameParts[i].sharedMaterial=city?cityFrameMaterials[i]:originalFrameMaterials[i];
    }
}
