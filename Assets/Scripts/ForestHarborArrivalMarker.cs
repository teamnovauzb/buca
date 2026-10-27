using UnityEngine;
/// <summary>Keeps the legacy arrival outline hidden while the train replaces its stop marker.</summary>
public sealed class ForestHarborArrivalMarker : MonoBehaviour
{
    public TwilightIllustratedMenu view;
    public ForestHarborMapPresentation presentation;
    public ForestHarborSelectionOutline ring;
    public void Refresh()
    {
        // This outline lives outside the stop's CanvasGroup, so hide it separately.
        if(ring!=null)ring.gameObject.SetActive(false);
    }
}
