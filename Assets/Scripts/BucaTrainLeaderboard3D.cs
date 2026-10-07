using UnityEngine;
/// <summary>Updates only visibility and materials of the scene-authored 3D train.</summary>
public sealed class BucaTrainLeaderboard3D : MonoBehaviour
{
    public LeaderboardPanel panel;
    public GameObject stage;
    public GameObject[] carriages;
    public Renderer[] faces;
    public GameObject[] playerEdges;
    public Material maple, mint;
    void LateUpdate()
    {
        bool visible=panel!=null && panel.gameObject.activeInHierarchy && panel.group.alpha>0;
        if(stage!=null && stage.activeSelf!=visible)stage.SetActive(visible);
        if(!visible)return;
        for(int i=0;i<carriages.Length;i++)
        {
            bool active=i<panel.rows.Length && panel.rows[i].gameObject.activeSelf;
            carriages[i].SetActive(active);
            bool player=active && panel.rows[i].rowBackground.color.g>panel.rows[i].rowBackground.color.r;
            if(active)faces[i].sharedMaterial=player?mint:maple;
            if(playerEdges!=null && i<playerEdges.Length && playerEdges[i]!=null)playerEdges[i].SetActive(player);
        }
    }
    void OnDisable(){if(stage!=null)stage.SetActive(false);}
}
