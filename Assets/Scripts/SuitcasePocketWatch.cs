using TMPro;
using UnityEngine;

/// <summary>Displays the railway's existing departure timer without owning a second clock.</summary>
[ExecuteAlways]
public sealed class SuitcasePocketWatch : MonoBehaviour
{
    public TwilightRailwayMap map;
    public TMP_Text digits, caption;
    public SuitcaseCountdownRing ring;

    void OnEnable() => Refresh();
    void LateUpdate() => Refresh();

    public void Refresh()
    {
        if (map == null || digits == null || caption == null || ring == null) return;
        float remaining = Application.isPlaying ? map.Countdown : TwilightRailwayMap.DepartureSeconds;
        int seconds = Mathf.CeilToInt(Mathf.Max(0, remaining));
        string label = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        if (digits.text != label) digits.text = label;
        string state = Application.isPlaying && map.Travelling ? "TRAVELLING" : "DEPARTS IN";
        if (caption.text != state) caption.text = state;
        ring.Fill = remaining / TwilightRailwayMap.DepartureSeconds;
    }
}
