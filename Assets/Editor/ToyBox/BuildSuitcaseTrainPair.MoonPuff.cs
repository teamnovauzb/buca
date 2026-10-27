#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class BuildSuitcaseTrainPair
{
    const string MoonOutput = "output/moon-puff-upgrade";
    static Material moonIndigo, moonPurple, moonBlue, moonPearl, moonGold, moonWood, moonDark, moonLamp;

    [MenuItem("RealBuca/Forest Harbor/Build Moon Puff Toy Upgrade")]
    public static string ApplyMoonPuff()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode required.");
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (map == null || map.gameObject.scene.path != "Assets/Scenes/MainMenu.unity") throw new Exception("Open MainMenu first.");
        Directory.CreateDirectory(MoonOutput);
        string backup = MoonOutput + "/MainMenu.before-moon-puff.unity";
        if (!File.Exists(backup)) EditorSceneManager.SaveScene(map.gameObject.scene, backup, true);
        backup = MoonOutput + "/WorldTwoHarborReveal.before-moon-puff.prefab";
        if (!File.Exists(backup)) File.Copy(BuildWorldTwoHarborReveal.Path, backup);
        meshes.Clear(); MoonMaterials();
        var old = map.trainMechanism.moonlight;
        Undo.RecordObject(map.trainMechanism, "Install Moon Puff World 2 train");
        map.trainMechanism.moonlight = MoonEngine(map.train, map.chimneySmoke);
        map.moonlightChimneySmoke = map.trainMechanism.moonlight.body.GetComponent<MoonPuffToyParts>().smoke;
        Undo.RegisterCreatedObjectUndo(map.trainMechanism.moonlight.body, "Create Moon Puff");
        if (old != null && old.body != null)
        {
            old.body.SetActive(false);
            if (old.body.name == "MoonPuffToyEngine") Object.DestroyImmediate(old.body);
        }
        map.wheels = map.trainMechanism.woodland.wheels.Concat(map.trainMechanism.moonlight.wheels).Select(w => w.pivot).ToArray();
        map.trainMechanism.Pose(map.WheelDistanceAt(map.RouteDistance), map.train.localScale.x,
            map.RouteDistance >= (map.stops[29].distance + map.stops[30].distance) * .5f);
        PrefabUtility.SaveAsPrefabAsset(map.trainMechanism.moonlight.body, Folder + "/MoonPuffToyEngine.prefab");
        // Keep the full pair prefab current for previews and future scene construction.
        PrefabUtility.SaveAsPrefabAsset(map.train.gameObject, Folder + "/WorldTrainPair.prefab");
        BuildMoonUpgrade(map);
        EditorUtility.SetDirty(map); EditorUtility.SetDirty(map.trainMechanism);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(map.gameObject.scene); EditorSceneManager.SaveScene(map.gameObject.scene);
        return "Moon Puff installed on the World 2 map; option 4 toy assembly attached to the existing level 30 harbor reveal.";
    }

    static Material MoonMaterial(string name, Color color, float metal = .02f, float grain = .22f)
    {
        var mat = Mat("MoonPuff-" + name, color, metal, .48f);
        mat.shader = Shader.Find("BUCA/Woodland Toy"); mat.SetColor("_BaseColor", color);
        mat.SetFloat("_GrainStrength", grain); mat.SetFloat("_TextureTint", 0);
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ToyBoxMenu/Materials/WorkshopMaplePhoto.png"));
        mat.SetTextureScale("_BaseMap", new Vector2(.75f, 1.5f));
        EditorUtility.SetDirty(mat); return mat;
    }

    static void MoonMaterials()
    {
        moonIndigo = MoonMaterial("IndigoCab", new Color(.16f, .12f, .48f));
        moonPurple = MoonMaterial("VioletRoof", new Color(.29f, .20f, .70f));
        moonBlue = MoonMaterial("BlueWoodWheels", new Color(.025f, .31f, .92f));
        moonPearl = MoonMaterial("PearlWoodBoiler", new Color(.94f, .92f, .89f), .02f, .38f);
        moonGold = MoonMaterial("GoldenCrescent", new Color(1, .67f, .13f), .7f, 0);
        moonWood = MoonMaterial("MapleCouplingRod", new Color(1, .74f, .36f), .01f, .65f);
        moonDark = MoonMaterial("Recess", new Color(.025f, .035f, .11f), 0, 0);
        moonLamp = MoonMaterial("WarmLamp", new Color(1, .95f, .75f), .05f, 0);
        moonLamp.SetColor("_EmissionColor", new Color(.30f, .20f, .07f));
        brass = moonGold;
    }

    static SuitcaseTrainMechanism.Engine MoonEngine(Transform parent, TrainChimneySmoke sourceSmoke)
    {
        var root = Group("MoonPuffToyEngine", parent);
        var parts = root.gameObject.AddComponent<MoonPuffToyParts>();
        var chassis = Group("GroundedChassis", root);
        Box("IndigoFootplate", chassis, new Vector3(0, .94f, 0), new Vector3(3.60f, .28f, 1.54f), .085f, moonIndigo);
        Box("DarkAxleBed", chassis, new Vector3(-.15f, .70f, 0), new Vector3(2.95f, .19f, 1.34f), .055f, moonDark);
        foreach (float side in new[] { -1f, 1f })
            Box("MapleFootplateLip", chassis, new Vector3(-.1f, 1.075f, side * .77f), new Vector3(3.3f, .055f, .045f), .018f, moonWood);
        // Chunky purple slats have real gaps, matching the toy cowcatcher in option 3.
        for (int i = 0; i < 6; i++)
        {
            float z = Mathf.Lerp(-.66f, .66f, i / 5f);
            var slat = Box("PurpleCowcatcherSlat", chassis, new Vector3(1.92f, .62f, z), new Vector3(.16f, .69f, .13f), .035f, moonPurple);
            slat.localRotation = Quaternion.Euler(0, 0, 25);
        }
        Box("CowcatcherTop", chassis, new Vector3(1.78f, .94f, 0), new Vector3(.22f, .16f, 1.51f), .045f, moonPurple);
        Box("CowcatcherBottom", chassis, new Vector3(2.07f, .32f, 0), new Vector3(.22f, .15f, 1.56f), .045f, moonPurple);
        Combine(chassis, "MoonPuffChassis");

        parts.cab = Group("AssembleCab", root);
        Box("RoundedIndigoCab", parts.cab, new Vector3(-1.07f, 1.91f, 0), new Vector3(1.30f, 1.69f, 1.38f), .095f, moonIndigo);
        foreach (float side in new[] { -1f, 1f })
        {
            Pane("ArchedWindowGoldFrame", parts.cab, new Vector3(-.81f, 2.23f, side * .707f), .47f, .67f, .055f, moonGold);
            Pane("WarmAmberWindow", parts.cab, new Vector3(-.81f, 2.23f, side * .742f), .37f, .55f, .025f, moonLamp);
        }
        Combine(parts.cab, "MoonPuffCab");
        parts.roof = Group("AssembleRoof", root);
        MoonRoof(parts.roof);
        Combine(parts.roof, "MoonPuffRoof");

        parts.boiler = Group("AssemblePearlBoiler", root);
        HoneyDisc("PearlBoiler", parts.boiler, new Vector3(.48f, 1.64f, 0), .59f, 1.94f, moonPearl, AlongX, .12f);
        foreach (float x in new[] { -.30f, 1.30f })
            HoneyDisc("PearlBoilerRim", parts.boiler, new Vector3(x, 1.64f, 0), .597f, .055f, moonPearl, AlongX, .015f);
        HoneyDisc("IndigoFrontRing", parts.boiler, new Vector3(1.48f, 1.64f, 0), .57f, .20f, moonIndigo, AlongX, .055f);
        HoneyDisc("BrassLampFrame", parts.boiler, new Vector3(1.60f, 1.64f, 0), .425f, .12f, moonGold, AlongX, .025f);
        Combine(parts.boiler, "MoonPuffBoiler");
        parts.lamp = HoneyDisc("WarmHeadlamp", parts.boiler, new Vector3(1.68f, 1.64f, 0), .335f, .045f, moonLamp, AlongX, .018f).GetComponent<Renderer>();

        parts.chimney = Group("AssembleChimney", root);
        var funnel = Lathe("ShortIndigoFunnel", new[] { V(.18f,0),V(.20f,.06f),V(.17f,.12f),V(.18f,.36f),V(.26f,.41f),V(.26f,.51f),V(.19f,.52f),V(.18f,.42f) }, moonIndigo);
        Place(funnel, parts.chimney, new Vector3(.90f, 2.18f, 0));
        Torus("VioletFunnelLip", parts.chimney, new Vector3(.90f, 2.69f, 0), .225f, .035f, moonPurple, Quaternion.identity);
        HoneyDisc("FunnelOpening", parts.chimney, new Vector3(.90f, 2.59f, 0), .18f, .015f, moonDark, Quaternion.identity, .004f);
        Combine(parts.chimney, "MoonPuffChimney");
        parts.crescent = Group("AssembleGoldCrescent", root);
        foreach (float side in new[] { -1f, 1f })
        {
            var crescent = MeshObject("GoldMoon", parts.crescent, new Vector3(-1.25f, 1.88f, side * .72f), Crescent(), moonGold);
            crescent.localScale = Vector3.one * .31f;
        }
        Combine(parts.crescent, "MoonPuffCrescent");

        var wheels = new List<SuitcaseTrainMechanism.Wheel>();
        var gear = new List<SuitcaseTrainMechanism.SideGear>();
        foreach (float side in new[] { -1f, 1f })
        {
            float phase = side < 0 ? 0 : 90;
            var rear = MoonWheel(root, wheels, new Vector3(-1.02f, .54f, side * .85f), .54f, phase, side);
            var front = MoonWheel(root, wheels, new Vector3(.44f, .54f, side * .85f), .54f, phase, side);
            MoonWheel(root, wheels, new Vector3(1.43f, .31f, side * .79f), .31f, phase, side);
            var rod = Box("AnimatedMapleCouplingRod", root, Vector3.zero, new Vector3(1, .135f, .10f), .045f, moonWood);
            gear.Add(new SuitcaseTrainMechanism.SideGear { rearPin = rear, frontPin = front, couplingRod = rod });
        }
        parts.smoke = Object.Instantiate(sourceSmoke, root);
        parts.smoke.name = "MoonPuffChimneySmoke"; parts.smoke.transform.localPosition = new Vector3(.90f, 2.70f, 0);
        parts.smoke.plume.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return new SuitcaseTrainMechanism.Engine { body = root.gameObject, wheels = wheels.ToArray(), gear = gear.ToArray() };
    }

    static Transform MoonWheel(Transform root, List<SuitcaseTrainMechanism.Wheel> wheels, Vector3 p, float radius, float phase, float side)
    {
        var pivot = Group("BlueWoodWheel" + wheels.Count, root); pivot.localPosition = p;
        var visual = Group("CarvedBlueWheel", pivot);
        HoneyDisc("RoundedBlueWheel", visual, Vector3.zero, radius, .22f, moonBlue, AlongZ, .055f);
        Torus("InsetGroove", visual, new Vector3(0, 0, side * .116f), radius * .78f, .013f, moonIndigo, AlongZ);
        HoneyDisc("InsetBlueFace", visual, new Vector3(0, 0, side * .12f), radius * .71f, .025f, moonBlue, AlongZ, .008f);
        HoneyDisc("GoldAxleHub", visual, new Vector3(0, 0, side * .16f), radius * .20f, .065f, moonGold, AlongZ, .022f);
        Combine(visual, "MoonPuffWheel" + (radius > .5f ? "Driving" : "Leading") + (side < 0 ? "Left" : "Right"));
        wheels.Add(new SuitcaseTrainMechanism.Wheel { pivot = pivot, radius = radius, phaseDegrees = phase });
        var pin = Group("GoldCrankPin", pivot); pin.localPosition = new Vector3(radius * .37f, 0, side * .23f);
        if (radius > .5f)
        {
            HoneyDisc("CrankCollar", pin, Vector3.zero, .12f, .10f, moonGold, AlongZ, .025f);
            HoneyDisc("RoundedGoldPin", pin, new Vector3(0, 0, side * .075f), .095f, .05f, moonGold, AlongZ, .018f);
        }
        return pin;
    }

    static void MoonRoof(Transform parent)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
        const float halfWidth = .90f, halfLength = .82f, thickness = .17f;
        for (int i = 0; i <= 40; i++)
        {
            float z = Mathf.Lerp(-halfWidth, halfWidth, i / 40f), y = .26f * (1 - z * z / (halfWidth * halfWidth));
            vertices.Add(new Vector3(-halfLength, y, z)); vertices.Add(new Vector3(halfLength, y, z));
            vertices.Add(new Vector3(-halfLength, y - thickness, z)); vertices.Add(new Vector3(halfLength, y - thickness, z));
            uv.Add(new Vector2(0, z)); uv.Add(new Vector2(1, z)); uv.Add(new Vector2(0, z)); uv.Add(new Vector2(1, z));
            if (i < 40)
            {
                int p = i * 4;
                triangles.AddRange(new[] { p,p+4,p+5,p,p+5,p+1, p+2,p+3,p+7,p+2,p+7,p+6,
                    p,p+2,p+6,p,p+6,p+4, p+1,p+5,p+7,p+1,p+7,p+3 });
            }
        }
        triangles.AddRange(new[] { 0,1,3,0,3,2,160,162,163,160,163,161 });
        MeshObject("CarvedPurpleRoof", parent, new Vector3(-1.08f, 2.79f, 0), Finish("MoonPuffThickRoof", vertices, triangles, null, uv), moonPurple);
    }

    static void BuildMoonUpgrade(TwilightRailwayMap map)
    {
        var root = PrefabUtility.LoadPrefabContents(BuildWorldTwoHarborReveal.Path);
        try
        {
            var reveal = root.GetComponent<WorldTwoHarborReveal>();
            bool wholeMap = reveal.trainUpgrade != null && reveal.trainUpgrade.wholeMap;
            if (reveal.trainUpgrade != null) Object.DestroyImmediate(reveal.trainUpgrade);
            foreach (string name in new[] { "MoonPuffStage", "MoonPuffTrainImage", "Level30Ticket", "Level31Ticket" })
            {
                var previous = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
                if (previous != null) Object.DestroyImmediate(previous.gameObject);
            }
            var upgrade = root.AddComponent<MoonPuffToyUpgrade>();
            upgrade.wholeMap = wholeMap;
            reveal.trainUpgrade = upgrade; reveal.duration = Mathf.Max(reveal.duration, MoonPuffToyUpgrade.Duration);
            upgrade.artwork = reveal.artwork;
            // A loaded screen-space Canvas can have zero scale until its first layout.
            // Bake geometry at identity, then parent the finished stage under the Canvas.
            var stage = Group("MoonPuffStage", null);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(stage.gameObject, root.scene);
            var trains = Group("UpgradeEnginePair", stage); trains.localScale = Vector3.one * map.train.localScale.x;
            var mechanism = trains.gameObject.AddComponent<SuitcaseTrainMechanism>(); upgrade.mechanism = mechanism;
            HoneyMaterials();
            mechanism.woodland = HoneyEngine(trains, false);
            upgrade.originalRoof = mechanism.woodland.body.GetComponentsInChildren<Transform>(true).First(t => t.name == "ThickCurvedMapleRoof");
            upgrade.originalRoof.SetParent(mechanism.woodland.body.transform, true);
            Combine(mechanism.woodland.body.transform.Find("WoodenBody"), "MoonPuffOriginalWoodenBody");
            MoonMaterials(); mechanism.moonlight = MoonEngine(trains, map.chimneySmoke);
            upgrade.parts = mechanism.moonlight.body.GetComponent<MoonPuffToyParts>();
            upgrade.trainScale = map.train.localScale.x; upgrade.displayScale = map.illustratedMenu.forestTrainDisplayScale;
            mechanism.Pose(0, upgrade.trainScale, false);
            var stars = Group("AssemblySparkles", trains);
            upgrade.sparkles = new Transform[18];
            for (int i = 0; i < upgrade.sparkles.Length; i++)
                upgrade.sparkles[i] = MeshObject("GoldenSparkle" + i, stars, Vector3.zero, Star(), moonGold);
            var camera = Group("UpgradeTrainCamera", stage).gameObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 3.8f; camera.cullingMask = 1 << Layer;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
            camera.nearClipPlane = .03f; camera.farClipPlane = 100; camera.allowHDR = false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false; upgrade.trainCamera = camera;
            var image = new GameObject("MoonPuffTrainImage", typeof(RectTransform), typeof(RawImage));
            image.transform.SetParent(reveal.artwork.transform, false);
            upgrade.trainImage = image.GetComponent<RawImage>(); upgrade.trainImage.raycastTarget = false;
            upgrade.arrivedTicket = new Color(.025f, .23f, .12f); upgrade.lockedTicket = new Color(.94f, .84f, .57f);
            upgrade.ticket30 = MoonTicket(reveal.artwork.transform, "Level30Ticket", "30", upgrade.arrivedTicket, map.illustratedMenu.numbers[0].font);
            upgrade.ticket31 = MoonTicket(reveal.artwork.transform, "Level31Ticket", "31", upgrade.lockedTicket, map.illustratedMenu.numbers[0].font);
            upgrade.ticket31Face = upgrade.ticket31.Find("TicketFace").GetComponent<Image>();
            upgrade.route = new Vector3[241]; upgrade.wheelTravel = new float[241];
            for (int i = 0; i < upgrade.route.Length; i++)
            {
                float d = Mathf.Lerp(map.stops[29].distance, map.stops[30].distance, i / 240f);
                upgrade.route[i] = map.Point(d);
                upgrade.wheelTravel[i] = map.WheelDistanceAt(d) - map.WheelDistanceAt(map.stops[29].distance);
            }
            upgrade.motionAudio = stage.gameObject.AddComponent<AudioSource>();
            upgrade.accentsAudio = stage.gameObject.AddComponent<AudioSource>();
            foreach (var audio in new[] { upgrade.motionAudio, upgrade.accentsAudio })
            { audio.playOnAwake = false; audio.spatialBlend = 0; audio.dopplerLevel = 0; }
            upgrade.chug = map.woodenChug;
            BakeMoonAccents(upgrade);
            stage.SetParent(root.transform, false); stage.localPosition = new Vector3(0, -10000, 0);
            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, BuildWorldTwoHarborReveal.Path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static RectTransform MoonTicket(Transform parent, string name, string label, Color color, TMP_FontAsset font)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image)); root.transform.SetParent(parent, false);
        root.GetComponent<Image>().color = new Color(.88f, .60f, .17f); root.GetComponent<Image>().raycastTarget = false;
        var face = new GameObject("TicketFace", typeof(RectTransform), typeof(Image)); face.transform.SetParent(root.transform, false);
        var rect = (RectTransform)face.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * 3; rect.offsetMax = Vector2.one * -3;
        face.GetComponent<Image>().color = color; face.GetComponent<Image>().raycastTarget = false;
        var text = new GameObject("LevelNumber", typeof(RectTransform), typeof(TextMeshProUGUI)); text.transform.SetParent(face.transform, false);
        rect = (RectTransform)text.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var tmp = text.GetComponent<TextMeshProUGUI>(); tmp.font = font; tmp.text = label; tmp.color = new Color(1, .96f, .77f);
        tmp.alignment = TextAlignmentOptions.Center; tmp.enableAutoSizing = true; tmp.fontSizeMin = 12; tmp.fontSizeMax = 64; tmp.raycastTarget = false;
        tmp.outlineWidth = .15f; tmp.outlineColor = new Color(.025f, .12f, .12f);
        return (RectTransform)root.transform;
    }

    static void BakeMoonAccents(MoonPuffToyUpgrade upgrade)
    {
        const int rate = 24000;
        var random = new System.Random(63031);
        var click = new float[(int)(rate * .15f)];
        for (int i = 0; i < click.Length; i++)
        {
            float t = i / (float)rate;
            click[i] = (Mathf.Sin(t * 2 * Mathf.PI * 870) * .33f + (float)(random.NextDouble() * 2 - 1) * .18f) * Mathf.Exp(-t * 55) * Mathf.Clamp01(t / .002f);
        }
        var chime = new float[(int)(rate * 1.1f)];
        for (int i = 0; i < chime.Length; i++)
        {
            float t = i / (float)rate;
            foreach (float hz in new[] { 784f, 987.77f, 1174.66f })
                chime[i] += .13f * Mathf.Sin(2 * Mathf.PI * hz * t) * Mathf.Exp(-t * 5) * Mathf.Clamp01(t / .02f);
        }
        var original = new float[upgrade.chug.samples * upgrade.chug.channels]; upgrade.chug.GetData(original, 0);
        var puff = new float[Mathf.Min(original.Length, (int)(upgrade.chug.frequency * .235f))]; Array.Copy(original, puff, puff.Length);
        for (int i = 0; i < puff.Length; i++) puff[i] *= Mathf.Clamp01((puff.Length - i) / (upgrade.chug.frequency * .015f));
        upgrade.click = MoonAudio("ToyPartClick", click, rate);
        upgrade.readyChime = MoonAudio("MoonPuffReady", chime, rate);
        upgrade.finishingChuk = MoonAudio("MoonPuffFirstChuk", puff, upgrade.chug.frequency);
    }

    static AudioClip MoonAudio(string name, float[] samples, int rate)
    {
        string path = "Assets/Audio/Celebration/" + name + ".wav";
        using (var stream = new BinaryWriter(File.Create(path)))
        {
            stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); stream.Write(36 + samples.Length * 2);
            stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); stream.Write(16); stream.Write((short)1); stream.Write((short)1);
            stream.Write(rate); stream.Write(rate * 2); stream.Write((short)2); stream.Write((short)16);
            stream.Write(System.Text.Encoding.ASCII.GetBytes("data")); stream.Write(samples.Length * 2);
            foreach (float sample in samples) stream.Write((short)(Mathf.Clamp(sample, -1, 1) * 32767));
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
#endif
