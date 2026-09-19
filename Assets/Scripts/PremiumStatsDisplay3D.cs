#if UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dimensional TextMeshPro score lettering rendered by an isolated 3D camera
/// into the top-left HUD. The cyan and amber numbers remain fully dynamic.
/// </summary>
public sealed class PremiumStatsDisplay3D : MonoBehaviour
{
    const int StatsLayer = 29;
    const int TextureWidth = 1024;
    const int TextureHeight = 512;
    static readonly Vector3 RigPosition = new Vector3(1200f, 1200f, 1200f);
    static readonly Color LabelFace = new Color(0.97f, 1f, 1f, 1f);
    static readonly Color CyanFace = new Color(0.06f, 0.90f, 0.95f, 1f);
    static readonly Color AmberFace = new Color(1f, 0.77f, 0.18f, 1f);
    static readonly Color TealSide = new Color(0.02f, 0.55f, 0.63f, 1f);
    static readonly Color DeepSide = new Color(0.01f, 0.17f, 0.24f, 1f);

    sealed class LetterStack
    {
        public TextMeshPro back;
        public TextMeshPro side;
        public TextMeshPro face;
    }

    readonly LetterStack[] _labels = new LetterStack[3];
    readonly LetterStack[] _values = new LetterStack[3];
    RawImage _image;
    GameObject _rig;
    Camera _camera;
    RenderTexture _texture;

    public bool Initialize(TMP_FontAsset font)
    {
        if (font == null) return false;
        _image = GetComponent<RawImage>();
        RectTransform rect = transform as RectTransform;
        if (_image == null || rect == null) return false;

        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(48f, -38f);
        rect.sizeDelta = new Vector2(465f, 224f);
        rect.localScale = Vector3.one;
        _image.raycastTarget = false;
        _image.color = Color.white;

        if (_rig == null) BuildRig(font);
        _image.texture = _texture;
        return _texture != null;
    }

    void BuildRig(TMP_FontAsset font)
    {
        _rig = new GameObject("Premium Stats 3D Rig");
        _rig.transform.position = RigPosition;

        _texture = new RenderTexture(TextureWidth, TextureHeight, 16, RenderTextureFormat.ARGB32)
        {
            name = "Buca Premium Stats 3D",
            antiAliasing = 4,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _texture.Create();

        GameObject cameraObject = new GameObject("Premium Stats Camera", typeof(Camera));
        cameraObject.transform.SetParent(_rig.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -5f);
        cameraObject.layer = StatsLayer;
        _camera = cameraObject.GetComponent<Camera>();
        _camera.orthographic = true;
        _camera.orthographicSize = 1f;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 10f;
        _camera.cullingMask = 1 << StatsLayer;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Color.clear;
        _camera.allowHDR = false;
        _camera.allowMSAA = true;
        _camera.targetTexture = _texture;

        string[] names = { "STROKES", "PAR", "SCORE" };
        for (int row = 0; row < names.Length; row++)
        {
            _labels[row] = CreateStack(names[row] + " 3D", font, LabelFace);
            _values[row] = CreateStack(names[row] + " Value 3D", font,
                row == 1 ? AmberFace : CyanFace);
            float y = 0.57f - row * 0.57f;
            FitStack(_labels[row], names[row], -1.91f, y, 2.35f, 0.39f);
            FitStack(_values[row], row == 1 ? "1" : "0", 0.80f, y, 0.93f, 0.49f);
        }
    }

    LetterStack CreateStack(string name, TMP_FontAsset font, Color faceColor)
    {
        return new LetterStack
        {
            back = CreateLetters(name + " Depth", font, DeepSide),
            side = CreateLetters(name + " Teal Edge", font,
                faceColor == AmberFace ? new Color(0.54f, 0.30f, 0.02f, 1f) : TealSide),
            face = CreateLetters(name + " Face", font, faceColor)
        };
    }

    TextMeshPro CreateLetters(string name, TMP_FontAsset font, Color color)
    {
        GameObject object3D = new GameObject(name, typeof(TextMeshPro));
        object3D.transform.SetParent(_rig.transform, false);
        object3D.layer = StatsLayer;
        TextMeshPro text = object3D.GetComponent<TextMeshPro>();
        text.font = font;
        text.fontSize = 10f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Left;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.rectTransform.sizeDelta = new Vector2(15f, 3f);
        text.color = color;
        return text;
    }

    static void FitStack(LetterStack stack, string content, float left, float centerY,
        float maxWidth, float targetHeight)
    {
        stack.face.text = content;
        stack.face.ForceMeshUpdate();
        Bounds bounds = stack.face.textBounds;
        float scale = Mathf.Min(maxWidth / Mathf.Max(0.01f, bounds.size.x),
            targetHeight / Mathf.Max(0.01f, bounds.size.y));
        float x = left - bounds.min.x * scale;
        float y = centerY - bounds.center.y * scale;

        PlaceLetters(stack.back, content, scale, new Vector3(x + 0.070f, y - 0.066f, 0.060f));
        PlaceLetters(stack.side, content, scale, new Vector3(x + 0.035f, y - 0.032f, 0.030f));
        PlaceLetters(stack.face, content, scale, new Vector3(x, y, 0f));
    }

    static void PlaceLetters(TextMeshPro letters, string content, float scale, Vector3 position)
    {
        letters.text = content;
        letters.transform.localScale = Vector3.one * scale;
        letters.transform.localPosition = position;
    }

    public void SetValues(int strokes, int par, int score)
    {
        if (_rig == null) return;
        FitStack(_values[0], strokes.ToString(), 0.80f, 0.57f, 0.93f, 0.49f);
        FitStack(_values[1], par.ToString(), 0.80f, 0f, 0.93f, 0.49f);
        FitStack(_values[2], score.ToString("N0"), 0.80f, -0.57f, 1.10f, 0.49f);
    }

    public void SetVisible(bool visible)
    {
        if (_image != null) _image.enabled = visible;
        if (_camera != null) _camera.enabled = visible;
    }

    void OnDestroy()
    {
        if (_camera != null) _camera.targetTexture = null;
        if (_rig != null) Destroy(_rig);
        if (_texture != null)
        {
            _texture.Release();
            Destroy(_texture);
        }
    }
}
#endif
