using UnityEngine;

/// <summary>Animates only editor-authored cabinet meshes and saved digit glyphs.</summary>
public sealed class BucaArcadeResult3D : MonoBehaviour
{
    public LeaderboardPanel panel;
    public GameObject stage;
    public Transform cabinet, scoreHousing;
    public GameObject heartsHeading, timeHeading;
    public MeshFilter[] digits;
    public Mesh[] numeralMeshes;
    public AudioClip finalChime;
    public BucaPodiumLeaderboard3D leaderboard;
    public GameObject[] leaderboardObjects;
    public bool ShowingLeaderboard { get; private set; }
    public int DisplayedScore { get; private set; }
    public bool CountComplete { get; private set; }
    int presentation = -1, previous = -1;
    float started, nextTick;
    bool chimed;
    void LateUpdate()
    {
        bool visible = panel != null && panel.gameObject.activeInHierarchy && panel.group.alpha > 0;
        stage.SetActive(visible);
        if (!visible) return;
        if (presentation != panel.PresentationId)
        {
            presentation = panel.PresentationId; started = panel.ShownAtUnscaled;
            nextTick = .8f; previous = -1; chimed = false; CountComplete = false;
        }
        float age = Time.unscaledTime - started;
        ShowingLeaderboard = true;
        cabinet.gameObject.SetActive(true);
        if(leaderboardObjects!=null)foreach(var item in leaderboardObjects)if(item!=null)item.SetActive(false);
        float enter = Mathf.Clamp01(age / .8f);
        float ease = 1 - Mathf.Pow(1 - enter, 4);
        cabinet.localPosition = new Vector3(-4.7f, Mathf.Lerp(-1.4f, 0, ease), 0);
        cabinet.localRotation = Quaternion.Euler(Mathf.Lerp(-7, 0, ease), 0, 0);
        cabinet.localScale = Vector3.one * Mathf.Lerp(.66f, .75f, ease);
        float t = Mathf.Clamp01((age - .8f) / 2.1f);
        int score = Mathf.Max(0, panel.PlayerScore);
        DisplayedScore = t >= 1 ? score : (int)System.Math.Floor((double)score * (1 - System.Math.Pow(1-t,3)));
        SetScore(DisplayedScore);
        if (age >= nextTick && t > 0 && t < 1 && score > 0)
        {
            AudioManager.Instance?.PlayScoreTick(); nextTick = age + Mathf.Lerp(.055f,.16f,t);
        }
        CountComplete = t >= 1;
        if (CountComplete && !chimed)
        {
            chimed = true;
            if (AudioManager.Instance != null && finalChime != null) AudioManager.Instance.PlaySfx(finalChime,.65f);
        }
        float pulse = age >= 2.9f ? Mathf.Sin(Mathf.Clamp01((age-2.9f)/.45f)*Mathf.PI)*.035f : 0;
        scoreHousing.localScale = Vector3.one * (1+pulse);
        bool timed = panel.titleText.text.Contains("TIME"); heartsHeading.SetActive(!timed); timeHeading.SetActive(timed);
    }
    public void SetScore(int score)
    {
        if(previous == score) return; previous = score;
        string value = Mathf.Max(0,score).ToString().PadLeft(4,'0');
        float step = Mathf.Min(1.15f, 5.5f/value.Length);
        for(int i=0;i<digits.Length;i++)
        {
            var tile=digits[i].transform.parent;
            tile.gameObject.SetActive(i<value.Length);
            if(i>=value.Length)continue;
            tile.localPosition=new Vector3((i-(value.Length-1)*.5f)*step,-.22f,-.34f);
            tile.localScale=new Vector3(step/.55f,1,1);
            digits[i].transform.localScale=new Vector3(.68f/(step/.55f),.78f,.65f);
            digits[i].sharedMesh=numeralMeshes[value[i]-'0'];
        }
    }
    void OnDisable(){if(stage!=null)stage.SetActive(false); presentation=-1;}
}
