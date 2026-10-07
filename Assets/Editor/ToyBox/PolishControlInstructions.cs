using UnityEditor;
using UnityEngine;

public static partial class BuildToyBoxMainMenu
{
    [MenuItem("RealBuca/Toy Box 3D/Polish Control Instructions")]
    public static void PolishControlInstructions()
    {
        PrepareFolders();
        ToyBoxGeometry.Initialize();
        string path = Root + "/Prefabs/SolidGameplayHud.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var hints = root.transform.Find("PhysicalControlHints");
            if (hints == null) throw new System.InvalidOperationException("Control deck is missing.");
            BuildReadableControlInstructions(hints);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("READABLE_CONTROL_INSTRUCTIONS_SAVED");
    }

    static void BuildReadableControlInstructions(Transform hints)
    {
        foreach (string name in new[] { "JOYSTICK", "SHOOT", "UNDO", "RESET", "ControlInstructionCards" })
        {
            var old = hints.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        var cards = Group("ControlInstructionCards", hints);
        var hud = hints.GetComponentInParent<ToyBoxGameplayHud>();
        var socket = hints.Find("SHOOTSocket");
        if(hud != null && socket != null)
        {
            var oldRing = socket.Find("ShootSelectionRing");
            if(oldRing != null) Object.DestroyImmediate(oldRing.gameObject);
            var selection = Material("ShootSelectionGold", new Color(1f,.8f,.22f), .3f);
            selection.shader = Shader.Find("Universal Render Pipeline/Unlit");
            selection.SetColor("_BaseColor", new Color(1f,.8f,.22f));
            EditorUtility.SetDirty(selection);
            Ring("ShootSelectionRing", socket, new Vector3(0,.17f,0), .37f, .035f, selection, .025f);
            hud.shootHighlight = socket.Find("ShootSelectionRing").gameObject;
            hud.shootHighlight.SetActive(false);
            hud.shootCap = hints.Find("SHOOTCap");
        }
        var backing = Material("ControlBadgeCream", new Color(.96f, .88f, .68f), .32f);
        backing.EnableKeyword("_EMISSION");
        backing.SetColor("_EmissionColor", new Color(.96f, .88f, .68f) * .18f);
        EditorUtility.SetDirty(backing);
        var trim = Material("ControlBadgeTeal", new Color(.045f, .30f, .32f), .4f);
        var lettering = Material("ControlInstructionInk", new Color(.035f, .09f, .15f), .2f);
        lettering.shader = Shader.Find("Universal Render Pipeline/Unlit");
        lettering.SetColor("_BaseColor", new Color(.035f, .09f, .15f));
        EditorUtility.SetDirty(lettering);

        string[] titles = { "JOYSTICK", "SHOOT", "UNDO" };
        string[] instructions = { "PULL TO AIM", "HOLD THEN RELEASE", "LAST SHOT" };
        for (int i = 0; i < titles.Length; i++)
        {
            // Separate tilted nameplates face the player instead of compressing
            // all instructions into one strip on the horizontal playing surface.
            var card = Group(titles[i] + "Instructions", cards, new Vector3((i - 1f) * 2.3f, -.87f, -.43f));
            card.localRotation = Quaternion.Euler(-25, 0, 0);
            // Form the broad rounded outline before flattening its depth. A thin
            // RoundedBox alone clamps corner radius to half its thickness.
            var edge = Box("TealRim", card, new Vector3(0,0,.015f), new Vector3(1.86f,.53f,.53f), trim,.24f);
            edge.transform.localScale = new Vector3(1,1,.16f);
            var face = Box("CreamInset", card, new Vector3(0,0,-.025f), new Vector3(1.77f,.44f,.44f), backing,.20f);
            face.transform.localScale = new Vector3(1,1,.12f);
            Text(titles[i], card, new Vector3(0, .085f, -.057f), .12f, lettering);
            Text(instructions[i], card, new Vector3(0, -.09f, -.057f), .085f, lettering);
        }
    }
}
