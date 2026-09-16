using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight one-shot Gaussian backdrop used by result screens. It captures
/// only when a panel opens, so there is no continuous WebGL post-process cost.
/// </summary>
public sealed class ResultBackdropBlur : MonoBehaviour
{
    RawImage _image;
    RenderTexture _texture;
    Material _material;

    public bool HasTexture => _texture != null && _texture.IsCreated();

    public static ResultBackdropBlur Ensure(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(objectName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage));

        if (existing == null) go.transform.SetParent(parent, false);

        RectTransform rt = go.transform as RectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        RawImage image = go.GetComponent<RawImage>();
        image.raycastTarget = false;
        image.color = Color.clear;
        go.transform.SetSiblingIndex(0);

        ResultBackdropBlur blur = go.GetComponent<ResultBackdropBlur>();
        if (blur == null) blur = go.AddComponent<ResultBackdropBlur>();
        blur._image = image;
        return blur;
    }

    public IEnumerator Capture(string textureName)
    {
        // The parent result CanvasGroup is transparent at this point, so the
        // screenshot contains only gameplay and never recursively captures UI.
        yield return new WaitForEndOfFrame();
        Release();

        Texture2D screenshot = null;
        RenderTexture passTexture = null;
        try
        {
            screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenshot == null)
                throw new InvalidOperationException("Screen capture returned null.");

            int width = Mathf.Max(1, screenshot.width / 4);
            int height = Mathf.Max(1, screenshot.height / 4);
            _texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = textureName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            _texture.Create();
            Graphics.Blit(screenshot, _texture);

            Material blur = GetBlurMaterial();
            if (blur != null)
            {
                passTexture = RenderTexture.GetTemporary(width, height, 0,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                passTexture.filterMode = FilterMode.Bilinear;
                passTexture.wrapMode = TextureWrapMode.Clamp;

                for (int pass = 0; pass < 3; pass++)
                {
                    float radius = 1.2f + pass * 0.65f;
                    blur.SetVector("_BlurDirection", new Vector4(radius, 0f, 0f, 0f));
                    Graphics.Blit(_texture, passTexture, blur);
                    blur.SetVector("_BlurDirection", new Vector4(0f, radius, 0f, 0f));
                    Graphics.Blit(passTexture, _texture, blur);
                }
            }

            if (_image != null)
            {
                _image.texture = _texture;
                // Keep the blurred gameplay visible, but lower its brightness
                // so the physical result totem remains the visual focus.
                // This is a texture tint, not the old flat grey overlay.
                _image.color = new Color(0.70f, 0.70f, 0.70f, 1f);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[ResultBackdropBlur] Capture failed: {exception.Message}");
            if (_image != null) _image.color = Color.clear;
        }
        finally
        {
            if (passTexture != null) RenderTexture.ReleaseTemporary(passTexture);
            if (screenshot != null) Destroy(screenshot);
        }
    }

    Material GetBlurMaterial()
    {
        if (_material != null) return _material;

        Shader shader = Resources.Load<Shader>("BucaGaussianBlur");
        if (shader == null) shader = Shader.Find("Hidden/Buca/GaussianBlur");
        if (shader == null || !shader.isSupported) return null;

        _material = new Material(shader)
        {
            name = "Buca Result Gaussian Blur (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
        return _material;
    }

    public void Release()
    {
        if (_image != null)
        {
            _image.texture = null;
            _image.color = Color.clear;
        }
        if (_texture == null) return;
        if (_texture.IsCreated()) _texture.Release();
        Destroy(_texture);
        _texture = null;
    }

    void OnDestroy()
    {
        Release();
        if (_material != null) Destroy(_material);
        _material = null;
    }
}
