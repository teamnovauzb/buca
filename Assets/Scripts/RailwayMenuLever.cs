using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>The map return control, with either a mesh lever or a wooden push button.</summary>
public sealed class RailwayMenuLever : MonoBehaviour, IPointerClickHandler
{
    public TwilightRailwayMap map;
    public Transform handle;
    public RectTransform pushButtonVisual;
    public Camera objectCamera;
    public MenuReturnTransition returnTransition;
    public AudioClip pullSound;
    bool returnRequested;
    float elapsed;
    public bool Returning { get; private set; }
    public void ResetLever() { Returning=false; returnRequested=false; elapsed=0; Pose(0); }
    void OnEnable() => ResetLever();
    public void Pose(float progress)
    {
        float t=Mathf.Clamp01(progress); t=t*t*t*(t*(t*6-15)+10);
        if(pushButtonVisual!=null)
            pushButtonVisual.localScale=Vector3.one*Mathf.Lerp(1,.94f,t);
        else if(handle!=null)handle.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-22,48,t));
    }
    public void Pull()
    {
        if(Returning || (returnTransition!=null && returnTransition.Busy))return;
        Returning=true; elapsed=0;
        if(Application.isPlaying && AudioManager.Instance!=null)AudioManager.Instance.PlaySfx(pullSound,1f);
    }
    public void OnPointerClick(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left)Pull(); }
    public bool TickReturn(float dt)
    {
        // Directional input belongs to level selection. Only an explicit menu action starts a return.
        if(!Returning)return false;
        if(returnRequested)return true;
        elapsed+=Mathf.Max(0,dt); Pose(elapsed/.38f);
        if(elapsed>=.48f)
        {
            returnRequested=true;
            if(returnTransition!=null)returnTransition.Begin(map.GoBack);
            else map.GoBack();
        }
        return true;
    }
}
