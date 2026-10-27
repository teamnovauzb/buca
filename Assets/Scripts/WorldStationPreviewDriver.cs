using UnityEngine;
/// <summary>Only attached to the disposable editor-built browser preview scene.</summary>
public sealed class WorldStationPreviewDriver : MonoBehaviour
{
    WorldStationRoute3D view;
    Bounds fullMap;
    bool detail;
    void Start(){view=GetComponent<WorldStationRoute3D>();fullMap=view.framing;view.SetUnlocked(true);view.ResetWorld();}
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.C))detail=!detail;
        view.framing=detail&&!view.BonusVisible?new Bounds(new Vector3(13.4f,2.15f,2.8f),new Vector3(9.7f,6.4f,10.3f)):fullMap;
        view.Fit(view.viewCamera.aspect);
        if(view.Modal)return;
        if(Input.GetKeyDown(KeyCode.Return)||Input.GetMouseButtonDown(0))view.OpenFromMenu(_=>view.ResetWorld());
    }
}
