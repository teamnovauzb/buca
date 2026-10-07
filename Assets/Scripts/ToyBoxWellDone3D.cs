using UnityEngine;

/// <summary>Animates the Editor-saved wooden result; all meshes and controls are baked.</summary>
public sealed class ToyBoxWellDone3D : MonoBehaviour
{
    public Transform banner, scorePlaque, nextButton, nextCap, countdownFill;
    public Collider nextHit;
    public MeshFilter[] scoreDigits;
    public Mesh[] digitMeshes;
    public ToyBoxPodiumLabel shotsLabel;
    public Transform[] confetti;
    public Vector3 bannerRest, scoreRest, buttonRest, capRest;

    public void Prepare(int shots)
    {
        shotsLabel.source.text=Mathf.Max(0,shots)+" SHOTS";
        shotsLabel.Refresh();nextCap.localPosition=capRest;
        SetCountdown(5);SampleEntrance(0);
    }
    public void SetTotal(int total)
    {
        string value=Mathf.Max(0,total).ToString();
        float scale=Mathf.Min(1.16f,4.3f/(value.Length*.85f));
        for(int i=0;i<scoreDigits.Length;i++)
        {
            bool visible=i<value.Length;scoreDigits[i].gameObject.SetActive(visible);
            if(!visible)continue;
            scoreDigits[i].sharedMesh=digitMeshes[value[i]-'0'];
            scoreDigits[i].transform.localScale=Vector3.one*scale;
            scoreDigits[i].transform.localPosition=Vector3.right*((i-(value.Length-1)*.5f)*scale*.85f);
        }
    }
    public void SampleEntrance(float seconds)
    {
        Pop(banner,bannerRest,seconds,0);
        Pop(scorePlaque,scoreRest,seconds,.12f);
        Pop(nextButton,buttonRest,seconds,.22f);
    }
    static void Pop(Transform part,Vector3 rest,float seconds,float delay)
    {
        float t=Mathf.Clamp01((seconds-delay)/.65f);
        float ease=1-Mathf.Pow(1-t,3);
        float bounce=Mathf.Sin(t*Mathf.PI)*.045f;
        part.localPosition=rest+Vector3.down*((1-ease)*.6f);
        part.localScale=Vector3.one*(.90f+.10f*ease+bounce);
        part.localRotation=Quaternion.Euler((1-ease)*-8,0,0);
    }
    public void PressNext() { nextCap.localPosition=capRest+Vector3.forward*.08f; }
    public void SampleConfetti(float seconds)
    {
        for(int i=0;i<confetti.Length;i++)
        {
            float t=seconds-.18f-(i%6)*.065f;
            var piece=confetti[i];bool visible=t>=0 && t<2.8f;
            piece.gameObject.SetActive(visible);if(!visible)continue;
            float seed=Mathf.Repeat(i*.618f+.2f,1),spread=Mathf.Repeat(i*.382f+.1f,1);
            float side=i%2==0?-1:1;
            piece.localPosition=new Vector3(side*(3.15f+seed*2.35f+.16f*t),-2.3f+spread*5.5f+.75f*t-.28f*t*t,.5f);
            piece.localRotation=Quaternion.Euler(90+t*90,i*37+t*95,i*51+t*75);
            piece.localScale=Vector3.one*(.13f+seed*.075f)*Mathf.Clamp01(t/.18f)*Mathf.Clamp01((2.8f-t)/.6f);
        }
    }
    public void StopConfetti() { foreach(var piece in confetti)piece.gameObject.SetActive(false); }
    public void SetCountdown(float seconds)
    {
        float t=Mathf.Clamp01(seconds/5);
        countdownFill.localScale=new Vector3(Mathf.Max(.001f,t),1,1);
        countdownFill.localPosition=new Vector3(-1.55f*(1-t),-.51f,-.27f);
    }
}
