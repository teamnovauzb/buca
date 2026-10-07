using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Buca.Prototypes
{
    // This component exists only in the isolated comparison scene. All visuals
    // are authored and saved by the Editor builder; runtime only switches them.
    public sealed class BucaLightingComparison : MonoBehaviour
    {
        public GameObject current, prototype;
        public Transform currentPuck, prototypePuck;
        public Camera comparisonCamera;
        public UniversalRenderPipelineAsset webPipeline;
        public UniversalRenderPipelineAsset prototypePipeline;
        public Vector3 closePosition, closeTarget, widePosition, wideTarget;
        public bool animate;
        float phase;
        void Awake()
        {
            // Use the actual saved Web profile even when previewing in Editor.
            QualitySettings.renderPipeline = webPipeline;
            QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            SetMode("current");
            SetView("close");
            Debug.Log("BUCA_LIGHTING_READY: Web profile, real-time shadows disabled, saved A/B puck materials.");
        }
        public void SetMode(string mode)
        {
            bool next = mode == "prototype";
            if (Application.isPlaying) QualitySettings.renderPipeline = next ? prototypePipeline : webPipeline;
            current.SetActive(!next); prototype.SetActive(next);
            Debug.Log("BUCA_LIGHTING_MODE=" + (next ? "prototype" : "current"));
        }
        public void SetMotion(string mode) { animate = mode == "on"; if (!animate) { phase = 0; Pose(); } }
        public void SetView(string view)
        {
            bool wide = view == "wide";
            comparisonCamera.transform.position = wide ? widePosition : closePosition;
            comparisonCamera.transform.LookAt(wide ? wideTarget : closeTarget);
        }
        void Update() { if (animate) { phase += Time.unscaledDeltaTime * .7f; Pose(); } }
        void Pose()
        {
            var offset = new Vector3(Mathf.Sin(phase) * .60f, 0, Mathf.Sin(phase * 2) * .16f);
            current.transform.localPosition = offset;
            prototype.transform.localPosition = offset;
            currentPuck.localRotation = prototypePuck.localRotation = Quaternion.Euler(0, phase * 25, 0);
        }
    }
}
