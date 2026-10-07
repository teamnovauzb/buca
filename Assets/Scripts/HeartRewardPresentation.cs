using UnityEngine;

/// <summary>Animates an authored pool of reward cards; creates no UI, meshes or materials.</summary>
public sealed class HeartRewardPresentation : MonoBehaviour
{
    [System.Serializable] public class Card
    {
        public RectTransform rect;
        public CanvasGroup group;
        [System.NonSerialized] public float elapsed;
        [System.NonSerialized] public Vector2 start;
        [System.NonSerialized] public int heartIndex;
        [System.NonSerialized] public bool active;
    }
    public RectTransform canvasRect;
    public ToyBoxGameplayHud hud;
    public Card[] cards;
    int next;
    public void Show(Vector3 worldPosition, int remaining)
    {
        if(cards==null || cards.Length==0) return;
        var card=cards[next++%cards.Length];
        var camera=Camera.main;
        Vector2 screen=camera!=null ? (Vector2)camera.WorldToScreenPoint(worldPosition) : new Vector2(Screen.width*.5f,Screen.height*.5f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,null,out card.start);
        var half=canvasRect.rect.size*.5f;
        card.start.x=Mathf.Clamp(card.start.x,-half.x+180,half.x-180);
        card.start.y=Mathf.Clamp(card.start.y,-half.y*.2f,half.y*.5f);
        card.elapsed=0; card.heartIndex=remaining-1; card.active=true;
        card.rect.gameObject.SetActive(true); card.group.alpha=1; card.rect.localScale=Vector3.zero;
    }
    void LateUpdate()
    {
        if(cards==null) return;
        foreach(var card in cards)
        {
            if(!card.active) continue;
            card.elapsed+=Time.deltaTime;
            float t=card.elapsed;
            Vector2 target=new Vector2(0,canvasRect.rect.height*.38f);
            if(hud!=null && hud.heartFills!=null && Camera.main!=null)
            {
                int index=Mathf.Clamp(card.heartIndex,0,hud.heartFills.Length-1);
                if(index>=0 && hud.heartFills[index]!=null)
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,Camera.main.WorldToScreenPoint(hud.heartFills[index].transform.position),null,out target);
            }
            float travel=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.85f)/.8f));
            card.rect.anchoredPosition=Vector2.Lerp(card.start,target,travel)+Vector2.up*(Mathf.Sin(travel*Mathf.PI)*90);
            float intro=Mathf.Clamp01(t/.32f); float back=1+2.70158f*Mathf.Pow(intro-1,3)+1.70158f*Mathf.Pow(intro-1,2);
            card.rect.localScale=Vector3.one*Mathf.Lerp(back,.16f,travel);
            card.group.alpha=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1.45f)/.25f));
            if(t>=1.7f){card.active=false;card.rect.gameObject.SetActive(false);}
        }
    }
    public void Clear()
    {
        if(cards==null)return;
        foreach(var card in cards){card.active=false;if(card.rect!=null)card.rect.gameObject.SetActive(false);}
    }
    void OnDisable()=>Clear();
}
