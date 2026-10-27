using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The wooden controls and scenery occlusion for the forest-to-harbor railway.</summary>
[ExecuteAlways, DefaultExecutionOrder(50)]
public sealed partial class ForestHarborMapPresentation : MonoBehaviour
{
    public TwilightIllustratedMenu view;
    public Button previous, next;
    public TMP_Text destination, progress;
    public CanvasGroup trainVisibility;
    public RectTransform contactShadow;
    public float harborTunnelEntrance, harborTunnelExit;
    public float forestTunnelEntrance, forestTunnelExit;
    public bool forestOpenCurve;
    public bool roundForestStops;
    public bool workshopRailway;
    public WorkshopStationBadge[] stationPlaques;
    public bool HasStationPlaques => stationPlaques!=null && stationPlaques.Length==50;
    public ForestHarborSignalSelector signalSelector;
    public ForestHarborArrivalMarker arrivalMarker;
    bool bound;

    void OnEnable() { Bind(); Refresh(); }
    void Bind()
    {
        if (bound || !Application.isPlaying || previous == null || next == null) return;
        bound = true;
        previous.onClick.AddListener(() => view.map.StepSelection(-1));
        next.onClick.AddListener(() => view.map.StepSelection(1));
    }
    void LateUpdate() { Bind(); Refresh(); }
    public void Refresh()
    {
        if (view == null || destination == null) return;
        var map = view.map;
        var trainBadge=view.artwork!=null?view.artwork.GetComponentInChildren<TrainLevelBadge>():null;
        if(trainBadge!=null)trainBadge.Refresh();
        if(Application.isPlaying&&view.artwork!=null)
        {
            var image=view.artwork.GetComponent<UnityEngine.UI.RawImage>();
            if(image!=null&&image.material!=null&&image.material.HasProperty("_WorldReveal"))
                image.material.SetFloat("_WorldReveal",map.IsLevelLocked(30)?0:1);
        }
        destination.text = (map.Selected + 1).ToString();
        progress.text = (map.Selected + 1) + " / 50";
        previous.interactable = map.Selected > 0;
        next.interactable = map.Selected < 49;
        // The line between the elevated terminal and the quay goes behind the station.
        // Its physical route still advances continuously, including wheels and sound.
        float distance = map.RouteDistance - map.trainNoseOffset;
        float conceal = Mathf.Min((distance - harborTunnelEntrance) / .65f,
            (harborTunnelExit - distance) / .65f);
        if (!forestOpenCurve)
            conceal = Mathf.Max(conceal, Mathf.Min((distance - forestTunnelEntrance) / .55f,
                (forestTunnelExit - distance) / .55f));
        if (trainVisibility != null) trainVisibility.alpha = 1 - Mathf.Clamp01(conceal);
        if (contactShadow != null)
        {
            var position = map.train.localPosition;
            contactShadow.anchoredPosition = new Vector2(position.x, position.z) * 20;
            var tangent = map.Point(distance + .1f) - map.Point(distance - .1f);
            contactShadow.localRotation = Quaternion.Euler(0,0,Mathf.Atan2(tangent.z,tangent.x)*Mathf.Rad2Deg);
            contactShadow.gameObject.SetActive(conceal <= 0);
        }
        if (signalSelector != null) signalSelector.Refresh();
        RefreshTicketClearance();
        if (arrivalMarker != null) arrivalMarker.Refresh();
        if(HasStationPlaques)
            for(int i=0;i<stationPlaques.Length;i++)
                if(stationPlaques[i]!=null)
                {
                    bool arrived=!map.Moving&&i==map.Parked;
                    stationPlaques[i].SetState(i==map.Selected,arrived);
                    if(stationPlaques[i].ticketPlaque)
                        view.numbers[i].color=arrived?new Color(1,.98f,.85f):new Color(.025f,.09f,.22f);
                }
    }
}
