using UnityEngine;
public sealed class CinematicMapDetails : MonoBehaviour
{
    public RectTransform artwork, controls, tickets, controlBackdrop;
    public int level=30;
    void LateUpdate(){Refresh();}
    public void Refresh()
    {
        var scale=new Vector3(artwork.rect.width/1672f,artwork.rect.height/941f,1);
        if(controls!=null)controls.localScale=scale;
        if(controlBackdrop!=null)controlBackdrop.localScale=scale;
        if(tickets!=null)
        {
            tickets.localScale=scale;
            for(int i=0;i<tickets.childCount;i++)
            {
                var badge=tickets.GetChild(i).GetComponentInChildren<WorkshopStationBadge>(true);
                if(badge!=null){badge.SetLocked(i+1>level);badge.SetState(false,false);}
            }
        }
    }
}
