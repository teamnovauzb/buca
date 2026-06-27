#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// ONE-SHOT: turn the chrome BALL into a flat HOCKEY PUCK (a slim disc).
///
///   RealBuca ▸ Puck → Flat Hockey Puck
///
/// Two things, both needed for it to look right:
///   1. Swaps the puck's mesh to a flat disc — baked into a saved mesh asset so the
///      win/grow animation (which rescales the puck uniformly) keeps it flat.
///   2. Turns OFF "roll to velocity" + freezes tumble, so the puck SLIDES flat like a
///      real hockey puck instead of rolling like a ball.
///
/// Keeps the puck's size, collider, trail, glow, and material (still chrome — say the
/// word for matte black). Gameplay/physics radius unchanged. AFTER running you can
/// DELETE THIS FILE (keep the generated mesh at Assets/Meshes/HockeyPuck.mesh).
/// </summary>
public static class ChangePuckToHockey
{
    const string GameScene = "Assets/Scenes/Game.unity";
    const string MeshPath  = "Assets/Meshes/HockeyPuck.mesh";
    const float  Flatten   = 0.14f;   // height = 14% of a normal cylinder → a slim puck

    [MenuItem("RealBuca/Puck → Flat Hockey Puck")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play mode first",
                "This edits + saves the Game scene. Click ■ Stop, then run it again.", "OK");
            return;
        }

        var mesh = EnsureFlatMesh();

        var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        GameObject puck = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var pc = root.GetComponentInChildren<PuckController>(true);
            if (pc != null) { puck = pc.gameObject; break; }
        }
        if (puck == null)
        {
            EditorUtility.DisplayDialog("Puck not found",
                "Couldn't find the PuckController in the Game scene. Nothing changed.", "OK");
            return;
        }

        // 1) Flat disc mesh, lying flat.
        var mf = puck.GetComponent<MeshFilter>();
        if (mf != null) mf.sharedMesh = mesh;
        puck.transform.localRotation = Quaternion.identity;

        // 2) Slide flat instead of rolling.
        var pd = puck.GetComponent<PuckDynamics>();
        if (pd != null) { pd.alignRotationToVelocity = false; EditorUtility.SetDirty(pd); }
        var rb = puck.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = false;
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // no tumble; can spin flat
            EditorUtility.SetDirty(rb);
        }

        EditorUtility.SetDirty(puck);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Puck → hockey puck ✓",
            "The puck is now a flat disc that SLIDES flat (no more rolling like a ball).\n\n" +
            "• Press ▶ Play and launch it.\n" +
            "• Same size / collider / trail / glow — only the shape + roll changed.\n" +
            "• You can DELETE this file (keep Assets/Meshes/HockeyPuck.mesh).\n\n" +
            "It's still chrome — want it MATTE BLACK like a real hockey puck? Just say so.",
            "OK");
    }

    static Mesh EnsureFlatMesh()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (existing != null) return existing;

        var temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        var mesh = Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);
        Object.DestroyImmediate(temp);

        mesh.name = "HockeyPuck";
        var verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++) verts[i].y *= Flatten;   // flatten into a disc
        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Directory.CreateDirectory(Path.GetDirectoryName(MeshPath));
        AssetDatabase.CreateAsset(mesh, MeshPath);
        return mesh;
    }
}
#endif
