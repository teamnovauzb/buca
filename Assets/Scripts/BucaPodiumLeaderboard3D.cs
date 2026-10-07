using UnityEngine;
/// <summary>Real mesh lettering and podiums, driven by the existing leaderboard data.</summary>
public sealed class BucaPodiumLeaderboard3D : MonoBehaviour
{
    public LeaderboardPanel panel;
    public GameObject stage;
    public string alphabet;
    public Mesh[] glyphs;
    public Transform[] names, scores, ranks;
    public Transform status, footer;
    public Renderer[] faces, bodies, dots;
    public Renderer[] caps;
    public GameObject[] clockParts;
    public Material cream, mint, navy, gold, silver, bronze, muted;
    public MeshFilter[] slots;
    float opened;
    bool showing;
    string previous;
    void LateUpdate()
    {
        bool visible=panel.gameObject.activeInHierarchy && panel.group.alpha>0;
        if(visible&&!showing){opened=Time.realtimeSinceStartup;previous=null;}
        showing=visible;stage.SetActive(visible);
        if(!visible)return;
        Sync(panel);
        int remaining=Mathf.Clamp(Mathf.CeilToInt(5-(Time.realtimeSinceStartup-opened)),0,5);
        for(int i=0;i<dots.Length;i++)dots[i].sharedMaterial=i<remaining?mint:muted;
    }
    public void Sync(LeaderboardPanel source)
    {
        string key=source.titleText.text+source.myRankText.text;
        foreach(var row in source.rows)key+=row.gameObject.activeSelf+row.nameText.text+row.scoreText.text+row.rankText.text+row.rowBackground.color;
        if(key==previous)return;previous=key;
        for(int i=0;i<5;i++)
        {
            var row=source.rows[i];bool active=row.gameObject.activeSelf;
            bool player=active&&row.rowBackground.color.g>row.rowBackground.color.r;
            Write(names[i],active?row.nameText.text:"—",i<3?2.05f:3.6f,i<3?.38f:.42f);
            Write(scores[i],active?row.scoreText.text:"",i<3?2.05f:1.85f,i<3?.44f:.43f);
            Write(ranks[i],active?row.rankText.text:"-",.65f,i<3?.59f:.40f);
            faces[i].sharedMaterial=player?mint:cream;
            if(i<3){bodies[i].sharedMaterial=player?mint:i==0?gold:i==1?silver:bronze;if(caps!=null&&i<caps.Length)caps[i].sharedMaterial=bodies[i].sharedMaterial;}
        }
        bool timedOut=source.titleText.text.Contains("TIME");
        if(clockParts!=null)foreach(var part in clockParts)part.SetActive(timedOut);
        status.localPosition=new Vector3(timedOut?.45f:0,2.5f,-.56f);
        Write(status,source.titleText.text,timedOut?4.3f:5.0f,.34f);
        // Only show the footer when it contains the player's otherwise unlisted result.
        string ownResult=System.Text.RegularExpressions.Regex.Replace(source.myRankText.text,"<.*?>","");
        bool listed=false;
        foreach(var row in source.rows)
            if(row.gameObject.activeSelf && row.rowBackground.color.g>row.rowBackground.color.r)listed=true;
        footer.gameObject.SetActive(!listed && !string.IsNullOrWhiteSpace(ownResult));
        Write(footer,listed?"":ownResult,7.8f,.21f);
    }
    void Write(Transform parent,string text,float width,float height)
    {
        var ui=parent.GetComponentInChildren<TMPro.TextMeshPro>(true);
        if(ui!=null)
        {
            ui.text=text??"";
            ui.rectTransform.sizeDelta=new Vector2(width,height*1.65f);
            ui.fontSizeMax=height*14f;ui.fontSizeMin=height*7f;ui.fontSize=height*14f;
            ui.ForceMeshUpdate(true);
            return;
        }
        text=(text??"").ToUpperInvariant();if(text.Length>parent.childCount)text=text.Substring(0,parent.childCount);
        float total=0;
        for(int i=0;i<text.Length;i++){int n=alphabet.IndexOf(text[i]);total+=n>=0?glyphs[n].bounds.size.x+.10f:.4f;}
        float scale=Mathf.Min(height,width/Mathf.Max(.01f,total));float x=-total*.5f;
        for(int i=0;i<parent.childCount;i++)
        {
            var child=parent.GetChild(i);int n=i<text.Length?alphabet.IndexOf(text[i]):-1;
            child.gameObject.SetActive(n>=0);
            if(i>=text.Length)continue;
            float w=n>=0?glyphs[n].bounds.size.x:.3f;
            if(n>=0){child.GetComponent<MeshFilter>().sharedMesh=glyphs[n];child.localPosition=new Vector3((x+w*.5f)*scale,0,0);child.localScale=Vector3.one*scale;}
            x+=w+.10f;
        }
    }
    void OnDisable(){showing=false;if(stage!=null)stage.SetActive(false);}
}
