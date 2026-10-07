using System;
using UnityEngine;

/// <summary>Switches references to saved materials and meshes; never generates assets.</summary>
public sealed class BucaSkinBinding : MonoBehaviour
{
    public const string PreferenceKey = "BucaCosmeticSkin";
    public const int Count = 5;
    [Serializable] public sealed class Surface
    {
        public Renderer target;
        public Material[] variants;
    }
    public Surface[] surfaces;
    public MeshFilter motif;
    public Mesh[] motifMeshes;
    public static int Selected => Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 0), 0, Count - 1);
    public static void Select(int index)
    {
        PlayerPrefs.SetInt(PreferenceKey, Mathf.Clamp(index, 0, Count - 1));
        PlayerPrefs.Save();
    }
    void OnEnable() => Apply(Selected);
    public void Apply(int index)
    {
        index = Mathf.Clamp(index, 0, Count - 1);
        if (surfaces != null) foreach (var surface in surfaces)
            if (surface.target != null && surface.variants != null && surface.variants.Length == Count)
                surface.target.sharedMaterial = surface.variants[index];
        if (motif != null && motifMeshes != null && motifMeshes.Length == Count)
            motif.sharedMesh = motifMeshes[index];
    }
}
