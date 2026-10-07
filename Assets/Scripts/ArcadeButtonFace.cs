using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Raised cabinet cap and silver bezel, matching the gameplay control deck.</summary>
public sealed class ArcadeButtonFace : UnityEngine.UI.Image, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public ArcadeInputAdapter.Button arcadeButton;
    public Color capColor=Color.green;
    bool pointerHeld;
    float depression;
    void Update()
    {
        float target=pointerHeld || ArcadeInputAdapter.GetButton(arcadeButton)?1:0;
        float next=Mathf.MoveTowards(depression,target,Time.unscaledDeltaTime*12);
        if(next!=depression){depression=next;sprite=depression>.35f?pressedSprite:raisedSprite;}
    }
    public void OnPointerDown(PointerEventData e){pointerHeld=true;}
    public void OnPointerUp(PointerEventData e){pointerHeld=false;}
    public void OnPointerExit(PointerEventData e){pointerHeld=false;}
    protected override void OnDisable(){pointerHeld=false;depression=0;base.OnDisable();}
    public Sprite raisedSprite, pressedSprite;
    protected override void OnEnable(){base.OnEnable();sprite=raisedSprite;}
}
