using TMPro;
using UnityEngine;

/// <summary>An upright emerald marker that follows the engine and shows its last reached station.</summary>
[ExecuteAlways, DefaultExecutionOrder(120)]
public sealed class TrainLevelBadge : MonoBehaviour
{
    public TwilightRailwayMap map;
    public TMP_Text number;
    public Vector2 offset = new Vector2(0, 88);
    public void Refresh()
    {
        if(map==null||map.train==null||number==null)return;
        var position=map.train.localPosition;
        ((RectTransform)transform).anchoredPosition=new Vector2(position.x,position.z)*20+offset;
        number.text=(map.Parked+1).ToString();
    }
    void LateUpdate()=>Refresh();
}
