using UnityEngine;

/// <summary>
/// Adds a small dark asymmetric marker to the puck so its rotation reads.
/// Attach this component to the Puck GameObject. It either uses the
/// `decalMesh` you assign (a child MeshRenderer you set up in the scene)
/// or, if you leave it empty, falls back to tinting/rotating an
/// auto-found child named "SpinDecal".
///
/// The decal inherits the puck's rotation through the normal transform
/// hierarchy, so no extra rotation code is needed — this component only
/// exists to guarantee the decal is present and colored correctly at
/// runtime (so you can swap its mesh / material in the Inspector and
/// the puck stays readable).
/// </summary>
[DisallowMultipleComponent]
public class PuckSpinDecal : MonoBehaviour
{
    [Tooltip("Child MeshRenderer whose material will be tinted dark. " +
             "Make it a small flat Quad or Cube placed slightly off-center " +
             "on the puck's surface.")]
    public MeshRenderer decalRenderer;

    [Tooltip("Dark color applied to the decal so it contrasts with the " +
             "bright yellow puck and reads visually as the puck spins.")]
    public Color decalColor = new Color(0.08f, 0.03f, 0.0f, 1f);

    void Awake()
    {
        if (decalRenderer == null)
        {
            var child = transform.Find("SpinDecal");
            if (child != null) decalRenderer = child.GetComponent<MeshRenderer>();
        }
        if (decalRenderer == null) return;

        // Instance the material so our tint doesn't leak into shared assets.
        var mat = decalRenderer.material;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", decalColor);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", decalColor);
        // Kill emission in case the base material had some.
        mat.DisableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", Color.black);
    }
}
