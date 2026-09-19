using UnityEngine;

/// <summary>
/// Adds a small dark asymmetric marker to the puck so its rotation reads.
/// Attach this component to the Puck GameObject. It either uses the
/// `decalMesh` you assign (a child MeshRenderer you set up in the scene)
/// or, if you leave it empty, falls back to finding an
/// auto-found child named "SpinDecal".
///
/// The decal inherits the puck's rotation through the normal transform
/// hierarchy, so no extra rotation code is needed — this component only
/// exists to keep the authored decal reference explicit. Its material and
/// color are saved in the scene/prefab and are never instanced at runtime.
/// </summary>
[DisallowMultipleComponent]
public class PuckSpinDecal : MonoBehaviour
{
    [Tooltip("Child MeshRenderer whose material will be tinted dark. " +
             "Make it a small flat Quad or Cube placed slightly off-center " +
             "on the puck's surface.")]
    public MeshRenderer decalRenderer;

    [Tooltip("Legacy authoring hint. Apply this color to the saved decal material in the Editor.")]
    public Color decalColor = new Color(0.08f, 0.03f, 0.0f, 1f);

    void Awake()
    {
        if (decalRenderer == null)
        {
            var child = transform.Find("SpinDecal");
            if (child != null) decalRenderer = child.GetComponent<MeshRenderer>();
        }
    }
}
