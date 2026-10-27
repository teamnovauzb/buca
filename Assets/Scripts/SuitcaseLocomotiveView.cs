using UnityEngine;

/// <summary>Photographs the travelling 3D locomotive onto the illustrated railway without flattening its turns or wheels.</summary>
[ExecuteAlways, DefaultExecutionOrder(100)]
public sealed class SuitcaseLocomotiveView : MonoBehaviour
{
    public TwilightRailwayMap map;
    public Camera trainCamera;
    public float verticalSize = 3f;
    public Vector3 centerOffset = new Vector3(0,1.2f,0);
    public Vector3 cameraOffset = new Vector3(0,6,-10.3923f);
    RenderTexture passengerCapture, originalCapture;

    public void EnsurePassengerResolution(float framingScale)
    {
        if (trainCamera == null || trainCamera.targetTexture == null) return;
        if (originalCapture == null) originalCapture = trainCamera.targetTexture;
        int width = Mathf.Clamp(Mathf.CeilToInt(1440 * framingScale / 64) * 64, 1440, 3072);
        int height = Mathf.RoundToInt(width * (float)originalCapture.height / originalCapture.width);
        if (passengerCapture != null && passengerCapture.width == width) return;
        var descriptor = originalCapture.descriptor;
        descriptor.width = width; descriptor.height = height;
        descriptor.useMipMap = false; descriptor.autoGenerateMips = false;
        var capture = new RenderTexture(descriptor)
        {
            name = "Sharp passenger train capture", filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        capture.Create();
        trainCamera.targetTexture = capture;
        var image = map.illustratedMenu.locomotive.GetComponent<UnityEngine.UI.RawImage>();
        if (image != null) image.texture = capture;
        ReleasePassengerCapture();
        passengerCapture = capture;
    }

    void ReleasePassengerCapture()
    {
        if (passengerCapture == null) return;
        passengerCapture.Release();
        if (Application.isPlaying) Destroy(passengerCapture); else DestroyImmediate(passengerCapture);
        passengerCapture = null;
    }

    void OnDestroy()
    {
        if (passengerCapture != null && trainCamera != null)
        {
            trainCamera.targetTexture = originalCapture;
            if (map != null && map.illustratedMenu != null && map.illustratedMenu.locomotive != null)
            {
                var image = map.illustratedMenu.locomotive.GetComponent<UnityEngine.UI.RawImage>();
                if (image != null) image.texture = originalCapture;
            }
        }
        ReleasePassengerCapture();
    }

    void LateUpdate() => Frame();

    public void Frame()
    {
        if (map == null || map.train == null || trainCamera == null) return;
        var target = map.train.localPosition + map.train.localRotation * centerOffset;
        trainCamera.transform.localPosition = target + cameraOffset;
        trainCamera.transform.localRotation = Quaternion.LookRotation(-cameraOffset);
        trainCamera.orthographicSize = verticalSize * Mathf.Abs(map.transform.lossyScale.x);
    }
}
