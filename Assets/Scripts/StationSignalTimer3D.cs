using UnityEngine;

/// <summary>Physical railway countdown with raised numeral tiles and signal lenses.</summary>
public sealed class StationSignalTimer3D : MonoBehaviour
{
    public GameObject[] numerals;
    public Renderer[] lenses;
    public Material[] lit, unlit;
    public Transform[] tiles;
    public int Seconds { get; private set; } = 30;
    int previous = -1;
    float changedAt;

    public void Display(float remaining)
    {
        Seconds = Mathf.Clamp(Mathf.CeilToInt(remaining), 0, 99);
        if (Seconds != previous)
        {
            for (int i = 0; i < numerals.Length; i++)
                numerals[i].SetActive(i % 10 == (i < 10 ? Seconds / 10 : Seconds % 10));
            previous = Seconds;
            changedAt = Time.unscaledTime;
        }
        int signal = Seconds > 10 ? 0 : Seconds > 5 ? 1 : 2;
        for (int i = 0; i < lenses.Length; i++) lenses[i].sharedMaterial = i == signal ? lit[i] : unlit[i];
        float phase = Mathf.Clamp01((Time.unscaledTime - changedAt) / .22f);
        foreach (var tile in tiles) tile.localRotation = Quaternion.Euler(-12 * Mathf.Sin(phase * Mathf.PI), 0, 0);
    }
}
