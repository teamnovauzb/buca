using UnityEngine;

/// <summary>
/// Attach this to the Luxodd UnityPluginPrefab (root GameObject) in the
/// MainMenu scene. It calls DontDestroyOnLoad so the WebSocket connection,
/// health check service, and all plugin components persist when the scene
/// changes from MainMenu → Game → back → etc.
///
/// A singleton guard prevents duplicates: if you accidentally have the
/// prefab in the Game scene too, the second instance self-destroys so the
/// original from MainMenu keeps its WebSocket connection alive.
///
/// Place ONE copy of the plugin prefab in the MainMenu scene only.
/// Remove it from the Game scene (and any other scenes) — the persistor
/// carries the original across.
/// </summary>
public class LuxoddPersistor : MonoBehaviour
{
    static LuxoddPersistor _instance;

    void Awake()
    {
        // Singleton guard — only one plugin prefab may exist at a time.
        // If one was already carried over from MainMenu, kill this duplicate.
        if (_instance != null && _instance != this)
        {
            Debug.Log("[LuxoddPersistor] Duplicate plugin prefab detected — destroying the newer copy. " +
                      "(The original from MainMenu stays alive with its active WebSocket connection.)");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        // Must be called on a root GameObject. If you attached this to a
        // child, Unity will silently ignore the call — so we hoist to root.
        if (transform.parent != null)
        {
            Debug.LogWarning("[LuxoddPersistor] Component is on a child GameObject. " +
                             "Unparenting so DontDestroyOnLoad works correctly.");
            transform.SetParent(null);
        }
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}
