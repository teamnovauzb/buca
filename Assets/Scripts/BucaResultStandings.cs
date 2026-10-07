using UnityEngine;
using TMPro;
/// <summary>Updates saved standings labels from the existing platform leaderboard rows.</summary>
public sealed class BucaResultStandings : MonoBehaviour
{
    public LeaderboardPanel panel;
    public TMP_Text[] labels;
    public GameObject[] rows;
    public TMP_Text empty;
    public TMP_Text[] rankLabels, scoreLabels;
    public Renderer[] rowFaces;
    public Renderer[] rankBadges;
    public Material[] rankMaterials;
    public GameObject[] playerMarkers;
    public Material normalFace, playerFace;
    public Color ink = new Color(.025f,.15f,.16f), ivory = new Color(1f,.95f,.81f);
    public TMP_Text footer;
    void LateUpdate() => Sync(panel);
    public void Sync(LeaderboardPanel sourcePanel)
    {
        var panel=sourcePanel;
        if(panel==null)return;
        int count=0;
        bool ranked=false,hasPlayer=false;
        for(int i=0;i<rows.Length;i++)
        {
            bool active=panel.rows!=null&&i<panel.rows.Length&&panel.rows[i]!=null&&panel.rows[i].gameObject.activeSelf;
            rows[i].SetActive(active);
            if(!active)continue;
            var source=panel.rows[i];
            int.TryParse(source.rankText.text,out int rank);
            bool separated=rankLabels!=null&&scoreLabels!=null&&i<rankLabels.Length&&i<scoreLabels.Length;
            if(separated)
            {
                bool mine=source.IsPlayerRow;hasPlayer|=mine;
                Write(rankLabels[i],source.rankText.text=="-"?"–":source.rankText.text,ink);
                Write(labels[i],source.nameText.text,mine?ivory:ink);
                Write(scoreLabels[i],source.scoreText.text,mine?ivory:ink);
                if(rowFaces!=null&&i<rowFaces.Length&&rowFaces[i]!=null)
                    rowFaces[i].sharedMaterial=mine?playerFace:normalFace;
                if(playerMarkers!=null&&i<playerMarkers.Length&&playerMarkers[i]!=null)playerMarkers[i].SetActive(mine);
                if(rankBadges!=null&&i<rankBadges.Length&&rankBadges[i]!=null)
                    rankBadges[i].sharedMaterial=rank>0&&rank<=3&&rankMaterials!=null&&rank<=rankMaterials.Length?rankMaterials[rank-1]:normalFace;
            }
            else labels[i].text=source.rankText.text+"<pos=12%>"+source.nameText.text+"<pos=76%>"+source.scoreText.text;
            ranked|=rank>0;
            count++;
        }
        if(footer!=null)footer.text=!ranked?"YOUR SCORE  ·  NO RANK AVAILABLE":hasPlayer?"YOUR ROW IS HIGHLIGHTED":"TOP "+count+" SCORES";
        empty.gameObject.SetActive(count==0||(!ranked&&count==1));
        if(empty.gameObject.activeSelf)empty.text="No rankings to show yet";
    }
    static void Write(TMP_Text label,string value,Color color)
    {
        if(label==null)return;
        if(label.text!=value)label.text=value;
        if(label.color!=color)label.color=color;
    }
}
