using UnityEngine;

/// <summary>Animates prebuilt wooden bonus signs without creating visuals at runtime.</summary>
public sealed class ToyBoxComboSign : MonoBehaviour
{
    public Transform presentation;
    public GameObject[] messages;
    public GameObject holeInOne;
    float elapsed, lifetime=1.3f;
    public void ShowHoleInOne()
    {
        elapsed=0; lifetime=2.4f;
        foreach(var message in messages) message.SetActive(false);
        holeInOne.SetActive(true); presentation.gameObject.SetActive(true); Sample(0);
    }
    public void Show(int multiplier)
    {
        elapsed=0; lifetime=1.3f; if(holeInOne!=null) holeInOne.SetActive(false);
        for(int i=0;i<messages.Length;i++) messages[i].SetActive(i==Mathf.Clamp(multiplier,2,5)-2);
        presentation.gameObject.SetActive(true);
        Sample(0);
    }
    void Update()
    {
        if(!presentation.gameObject.activeSelf) return;
        elapsed+=Time.unscaledDeltaTime; Sample(elapsed);
    }
    public void Sample(float time)
    {
        var camera=Camera.main;
        if(camera==null) { presentation.gameObject.SetActive(false); return; }
        float distance=9;
        float halfHeight=camera.orthographic?camera.orthographicSize:distance*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad);
        presentation.position=camera.transform.position+camera.transform.forward*distance+camera.transform.up*(halfHeight*.75f);
        presentation.rotation=camera.transform.rotation;
        float fit=Mathf.Min(.82f,halfHeight*camera.aspect*.82f/2.3f);
        float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/.18f));
        float exit=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(lifetime-.3f,lifetime,time));
        presentation.localScale=Vector3.one*fit*enter*exit;
        if(time>=lifetime) presentation.gameObject.SetActive(false);
    }
    public void Hide() { presentation.gameObject.SetActive(false); }
}
