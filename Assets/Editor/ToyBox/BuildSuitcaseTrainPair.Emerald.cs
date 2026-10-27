#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class BuildSuitcaseTrainPair
{
    public const string EmeraldOutput = "output/emerald-train";

    [MenuItem("RealBuca/Forest Harbor/Apply Emerald Train and Moving Rods")]
    public static string ApplyEmerald()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode only.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainMenu.unity") throw new Exception("Open MainMenu first.");
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (map == null || map.trainMechanism == null || !map.illustratedMenu.forestHarborPresentation)
            throw new Exception("The existing forest railway is required.");
        Directory.CreateDirectory(EmeraldOutput);
        string backup = EmeraldOutput + "/MainMenu.before-emerald-train.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);
        Directory.CreateDirectory(Folder + "/Meshes");
        Directory.CreateDirectory(Folder + "/Materials");
        meshes.Clear();
        EmeraldMaterials();
        var mechanism = map.trainMechanism;
        if (mechanism.woodland.body.name == "EmeraldTankEngine")
            throw new Exception("The emerald engine is already installed.");
        var engine = EmeraldEngine(map.train);
        Undo.RegisterCreatedObjectUndo(engine.body, "Add emerald train");
        Undo.RecordObject(mechanism, "Connect emerald wheels and rods");
        Undo.RecordObject(mechanism.woodland.body, "Replace woodland engine");
        mechanism.woodland.body.SetActive(false);
        mechanism.woodland = engine;

        var view = map.illustratedMenu;
        var rig = map.GetComponent<SuitcaseLocomotiveView>();
        Undo.RecordObjects(new Object[] { map, map.train, view, view.locomotive, rig }, "Fit emerald train to rails");
        // 104 / (2 * 2.6) = 20 image pixels per world unit, matching the map.
        // The elevated camera and axle width also match the illustrated rail gauge.
        map.train.localScale = Vector3.one * 1.45f;
        map.trainProjectionDepth = Mathf.Sqrt(2);
        rig.verticalSize = 2.6f;
        rig.centerOffset = new Vector3(0, 1.55f, 0);
        rig.cameraOffset = new Vector3(0, 8, -8);
        view.locomotive.sizeDelta = new Vector2(156, 104);
        view.forestTrainGroundOffset = 1.55f * Mathf.Sqrt(.5f) * 20;
        typeof(TwilightRailwayMap).GetMethod("Pose",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(map, null);
        view.PoseTrain();
        view.GetComponent<ForestHarborMapPresentation>().Refresh();
        rig.Frame();
        EditorUtility.SetDirty(mechanism);
        EditorUtility.SetDirty(map);
        EditorUtility.SetDirty(view);
        EditorUtility.SetDirty(rig);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Saved the compact emerald engine with four spoked wheels and two distance-driven crank linkages.";
    }

    static Material EmeraldMaterial(string name, Color color, float metallic, float smoothness)
    {
        var material = Mat("Emerald-" + name, color, metallic, smoothness);
        material.shader = Shader.Find("BUCA/Woodland Toy");
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_GrainStrength", 0);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void EmeraldMaterials()
    {
        green = EmeraldMaterial("Enamel", new Color(.04f, .37f, .17f), .32f, .73f);
        brass = EmeraldMaterial("Copper", new Color(.78f, .49f, .22f), .8f, .72f);
        gold = EmeraldMaterial("PolishedTrim", new Color(.98f, .77f, .40f), .82f, .78f);
        dark = EmeraldMaterial("BlackenedSteel", new Color(.035f, .043f, .042f), .5f, .48f);
        steel = EmeraldMaterial("WheelTreads", new Color(.35f, .39f, .40f), .85f, .65f);
        oak = EmeraldMaterial("CabInterior", new Color(.15f, .065f, .025f), .05f, .25f);
        glow = EmeraldMaterial("Lamp", new Color(1, .82f, .3f), .1f, .65f);
        glow.SetColor("_EmissionColor", new Color(.6f, .28f, .04f));
        EditorUtility.SetDirty(glow);
    }

    static SuitcaseTrainMechanism.Engine EmeraldEngine(Transform parent)
    {
        var root = Group("EmeraldTankEngine", parent);
        var body = Group("BodyDetails", root);
        Box("Underframe", body, new Vector3(0, .75f, 0), new Vector3(3.25f, .19f, 1.62f), .065f, dark);
        Box("CopperFootplate", body, new Vector3(0, .87f, 0), new Vector3(3.35f, .065f, 1.7f), .025f, brass);
        Disc("ShortBoiler", body, new Vector3(.43f, 1.4f, 0), .53f, 1.83f, green, AlongX, .08f);
        foreach (float side in new[] { -1f, 1f })
        {
            Box("RoundedWaterTank", body, new Vector3(.34f, 1.34f, side * .49f),
                new Vector3(1.73f, .82f, .47f), .15f, green);
            Box("TankLowerTrim", body, new Vector3(.33f, .98f, side * .735f),
                new Vector3(1.73f, .035f, .035f), .012f, gold);
            Bar("CopperHandrail", body, new Vector3(-.43f, 1.68f, side * .67f),
                new Vector3(1.14f, 1.68f, side * .67f), .021f, brass);
            foreach (float x in new[] { -.35f, .37f, 1.06f })
                Bar("HandrailMount", body, new Vector3(x, 1.68f, side * .59f),
                    new Vector3(x, 1.68f, side * .72f), .018f, gold);
        }
        foreach (float x in new[] { -.36f, .75f })
            Torus("BoilerBand", body, new Vector3(x, 1.4f, 0), .54f, .026f, gold, AlongX);
        Disc("FrontSmokeBox", body, new Vector3(1.38f, 1.4f, 0), .53f, .17f, dark, AlongX, .04f);
        Disc("GreenFrontDoor", body, new Vector3(1.49f, 1.4f, 0), .46f, .08f, green, AlongX, .03f);
        Torus("DoorCopperRim", body, new Vector3(1.54f, 1.4f, 0), .46f, .035f, brass, AlongX);
        Disc("LampHousing", body, new Vector3(1.60f, 1.43f, 0), .255f, .15f, gold, AlongX, .03f);
        Disc("WarmLamp", body, new Vector3(1.69f, 1.43f, 0), .205f, .018f, glow, AlongX, .008f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6;
            Sphere("FrontRivet", body, new Vector3(1.54f, 1.4f + Mathf.Sin(a) * .40f, Mathf.Cos(a) * .40f),
                Vector3.one * .035f, gold);
        }
        var chimney = Lathe("ShortChimney", new[] { V(.19f, 0), V(.2f, .06f), V(.145f, .11f),
            V(.145f, .45f), V(.22f, .51f), V(.23f, .56f), V(.205f, .6f) }, dark);
        Place(chimney, body, new Vector3(.98f, 1.87f, 0));
        Torus("CopperChimneyLip", body, new Vector3(.98f, 2.43f, 0), .217f, .035f, brass, Quaternion.identity);
        Disc("ChimneyOpening", body, new Vector3(.98f, 2.475f, 0), .18f, .01f, dark, Quaternion.identity, .003f);
        var dome = Lathe("CopperSteamDome", new[] { V(.24f, 0), V(.25f, .04f), V(.22f, .08f),
            V(.22f, .22f), V(.18f, .33f), V(.08f, .40f), V(0, .42f) }, brass);
        Place(dome, body, new Vector3(.02f, 1.88f, 0));
        Sphere("DomeFinial", body, new Vector3(.02f, 2.32f, 0), Vector3.one * .09f, gold);

        Box("Cab", body, new Vector3(-1.08f, 1.57f, 0), new Vector3(1.04f, 1.39f, 1.5f), .075f, green);
        foreach (float side in new[] { -1f, 1f })
        {
            foreach (float x in new[] { -1.32f, -.86f })
            {
                Pane("CopperWindowFrame", body, new Vector3(x, 1.86f, side * .771f), .38f, .69f, .04f, gold);
                Pane("DarkWindow", body, new Vector3(x, 1.86f, side * .8f), .30f, .60f, .025f, oak);
                Pane("CabLowerPanelTrim", body, new Vector3(x, 1.15f, side * .77f), .38f, .26f, .035f, brass);
                Pane("CabLowerPanel", body, new Vector3(x, 1.15f, side * .8f), .32f, .20f, .02f, green);
            }
            Box("DoorHandle", body, new Vector3(-.68f, 1.48f, side * .83f),
                new Vector3(.04f, .13f, .04f), .012f, gold);
            Box("CabStep", body, new Vector3(-1.06f, .77f, side * .91f),
                new Vector3(.70f, .07f, .23f), .025f, dark);
            Disc("PistonCylinder", body, new Vector3(1.20f, .5f, side * .92f),
                .155f, .61f, dark, AlongX, .03f);
            Torus("PistonCopperBand", body, new Vector3(1.5f, .5f, side * .92f),
                .155f, .025f, brass, AlongX);
        }
        Roof(body, new Vector3(-1.08f, 2.28f, 0), 1.27f, 1.81f, green);
        foreach (float x in new[] { -1.715f, -.445f })
        {
            Vector3 last = new Vector3(x, 2.28f, -.905f);
            for (int i = 1; i <= 20; i++)
            {
                float z = Mathf.Lerp(-.905f, .905f, i / 20f);
                var next = new Vector3(x, 2.28f + .22f * (1 - z * z / (.905f * .905f)), z);
                Bar("RoofCopperEdge", body, last, next, .024f, gold);
                last = next;
            }
        }
        foreach (float x in new[] { -1.50f, -1.1f, -.67f })
        foreach (float side in new[] { -1f, 1f })
            Sphere("RoofRivet", body, new Vector3(x, 2.37f, side * .7f), Vector3.one * .042f, gold);
        Box("FrontBufferBeam", body, new Vector3(1.66f, .77f, 0), new Vector3(.19f, .22f, 1.68f), .035f, dark);
        foreach (float side in new[] { -1f, 1f })
        {
            Disc("BufferStem", body, new Vector3(1.79f, .77f, side * .57f), .08f, .19f, dark, AlongX, .02f);
            Disc("CopperBuffer", body, new Vector3(1.9f, .77f, side * .57f), .15f, .075f, brass, AlongX, .025f);
        }
        Combine(body, "CompactEmeraldBody");

        var wheels = new List<SuitcaseTrainMechanism.Wheel>();
        var gear = new List<SuitcaseTrainMechanism.SideGear>();
        foreach (float side in new[] { -1f, 1f })
        {
            float phase = side < 0 ? 0 : 90;
            var rear = EmeraldWheel(root, wheels, new Vector3(-.90f, .49f, side * .91f), phase, side);
            var front = EmeraldWheel(root, wheels, new Vector3(.68f, .49f, side * .91f), phase, side);
            var coupling = Box("CopperCouplingRod", root, Vector3.zero, new Vector3(1, .085f, .065f), .025f, brass);
            var drive = Box("PistonRod", root, Vector3.zero, new Vector3(1, .05f, .045f), .018f, steel);
            var slider = Box("PistonCrosshead", root, Vector3.zero, new Vector3(.14f, .14f, .075f), .025f, gold);
            gear.Add(new SuitcaseTrainMechanism.SideGear {
                rearPin = rear, frontPin = front, couplingRod = coupling,
                driveRod = drive, crosshead = slider, sliderHeight = .49f, rodLength = .67f
            });
        }
        return new SuitcaseTrainMechanism.Engine { body = root.gameObject, wheels = wheels.ToArray(), gear = gear.ToArray() };
    }

    static Transform EmeraldWheel(Transform root, List<SuitcaseTrainMechanism.Wheel> wheels,
        Vector3 position, float phase, float side)
    {
        const float radius = .49f;
        var pivot = Group("EmeraldWheel" + wheels.Count, root);
        pivot.localPosition = position;
        var visual = Group("SpokedWheel", pivot);
        Torus("SteelTread", visual, Vector3.zero, .465f, .025f, steel, AlongZ);
        Torus("CopperRim", visual, new Vector3(0, 0, side * .025f), .434f, .024f, brass, AlongZ);
        Torus("GreenInnerRim", visual, Vector3.zero, .407f, .029f, green, AlongZ);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI / 6;
            var spoke = Box("GreenSpoke", visual,
                new Vector3(Mathf.Cos(angle) * .245f, Mathf.Sin(angle) * .245f, 0),
                new Vector3(.34f, .046f, .065f), .017f, green);
            spoke.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
        }
        Disc("CopperHub", visual, new Vector3(0, 0, side * .035f), .12f, .10f, brass, AlongZ, .02f);
        Combine(visual, "CompactEmeraldWheel" + (side < 0 ? "Left" : "Right"));
        wheels.Add(new SuitcaseTrainMechanism.Wheel { pivot = pivot, radius = radius, phaseDegrees = phase });
        var pin = Group("CrankPin", pivot);
        pin.localPosition = new Vector3(.19f, 0, side * .13f);
        Disc("CopperCrankBoss", pin, Vector3.zero, .092f, .065f, gold, AlongZ, .018f);
        Disc("CopperCrankCap", pin, new Vector3(0, 0, side * .052f), .059f, .035f, brass, AlongZ, .012f);
        return pin;
    }
}
#endif
