#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>The approved oak, teal and red wooden toy, authored as saved meshes.</summary>
public static partial class BuildSuitcaseTrainPair
{
    public const string HoneyOutput = "output/honey-oak-train";
    static Material honeyWood, honeyEdge, honeyTeal, honeyRed, honeyBrass, honeyDark, honeyLamp;

    [MenuItem("RealBuca/Forest Harbor/Apply Honey Oak Toy Train")]
    public static string ApplyHoneyOak()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode only.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (scene.path != "Assets/Scenes/MainMenu.unity" || map == null || map.trainMechanism == null)
            throw new Exception("Open the existing MainMenu railway first.");
        Directory.CreateDirectory(HoneyOutput);
        string backup = HoneyOutput + "/MainMenu.before-honey-oak.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);
        Directory.CreateDirectory(Folder + "/Meshes");
        Directory.CreateDirectory(Folder + "/Materials");
        meshes.Clear();
        HoneyMaterials();
        var mechanism = map.trainMechanism;
        var previous = mechanism.woodland.body;
        var engine = HoneyEngine(map.train);
        Undo.RegisterCreatedObjectUndo(engine.body, "Build honey oak train");
        Undo.RecordObject(mechanism, "Connect wooden train mechanism");
        if (previous != null)
        {
            Undo.RecordObject(previous, "Replace woodland train");
            previous.SetActive(false);
        }
        mechanism.woodland = engine;
        InstallHoneySmoke(map);
        InstallHoneyChug(map);
        if (previous != null && previous.name == "HoneyOakToyEngine") Undo.DestroyObjectImmediate(previous);
        var view = map.illustratedMenu;
        var rig = map.GetComponent<SuitcaseLocomotiveView>();
        Undo.RecordObjects(new Object[] { map, map.train, view, view.locomotive, rig }, "Align wooden train to track");
        map.wheels = engine.wheels.Concat(mechanism.moonlight.wheels).Select(w => w.pivot).ToArray();
        map.train.localScale = Vector3.one * 1.45f;
        map.trainProjectionDepth = Mathf.Sqrt(2);
        map.trainHeadingSpan = .35f;
        map.trainWheelTravelScale = view.forestTrainDisplayScale;
        // Preserve the same pixels per world unit and ground contact as the railway.
        rig.verticalSize = 3.8f;
        rig.centerOffset = new Vector3(0, 2f, 0);
        rig.cameraOffset = new Vector3(0, 8, -8);
        view.locomotive.sizeDelta = new Vector2(228, 152);
        BuildTrainStopClearance.Configure(map);
        view.locomotive.GetComponent<UnityEngine.UI.RawImage>().raycastTarget = false;
        view.forestTrainGroundOffset = 2f * Mathf.Sqrt(.5f) * 20;
        map.steamOrigin = new Vector3(1.06f, 2.81f, 0);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(TwilightRailwayMap).GetMethod("Cache", flags).Invoke(map, null);
        typeof(TwilightRailwayMap).GetMethod("Pose", flags).Invoke(map, null);
        view.PoseTrain(); rig.Frame();
        view.GetComponent<ForestHarborMapPresentation>().Refresh();
        PrefabUtility.SaveAsPrefabAsset(engine.body, Folder + "/HoneyOakToyEngine.prefab");
        foreach (var item in new Object[] { mechanism, map, view, rig }) EditorUtility.SetDirty(item);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        EditorApplication.QueuePlayerLoopUpdate();
        return "Saved honey oak engine, teal open-window cab, four red wooden wheels and two animated wooden coupling rods.";
    }

    static Material HoneyMaterial(string name, Color color, float metal, float gloss, float grain)
    {
        var material = Mat("HoneyOak-" + name, color, metal, gloss);
        material.shader = Shader.Find("BUCA/Woodland Toy");
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metal); material.SetFloat("_Smoothness", gloss);
        material.SetFloat("_GrainStrength", grain); material.SetFloat("_TextureTint", 0);
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ToyBoxMenu/Materials/WorkshopMaplePhoto.png"));
        material.SetTextureScale("_BaseMap", new Vector2(.75f, 1.5f));
        EditorUtility.SetDirty(material); return material;
    }

    static void HoneyMaterials()
    {
        honeyWood = HoneyMaterial("GoldenMaple", new Color(1f, .60f, .21f), .015f, .38f, .90f);
        honeyEdge = HoneyMaterial("EndGrain", new Color(.80f, .38f, .105f), .015f, .32f, .85f);
        honeyTeal = HoneyMaterial("TealPaintedWood", new Color(.045f, .44f, .47f), .015f, .42f, .6f);
        honeyRed = HoneyMaterial("RedWoodWheels", new Color(.80f, .055f, .025f), .015f, .36f, .8f);
        honeyBrass = HoneyMaterial("WarmBrass", new Color(.95f, .65f, .24f), .78f, .72f, 0);
        honeyDark = HoneyMaterial("DarkCabInterior", new Color(.085f, .047f, .018f), 0, .15f, .6f);
        honeyLamp = HoneyMaterial("IvoryHeadlight", new Color(1, .91f, .62f), .05f, .52f, 0);
        honeyLamp.SetColor("_EmissionColor", new Color(.20f, .13f, .045f));
        EditorUtility.SetDirty(honeyLamp);
    }

    static SuitcaseTrainMechanism.Engine HoneyEngine(Transform parent, bool combineBody = true)
    {
        var root = Group("HoneyOakToyEngine", parent);
        var body = Group("WoodenBody", root);
        Box("MapleChassis", body, new Vector3(0, .91f, 0), new Vector3(3.52f, .24f, 1.68f), .075f, honeyWood);
        Box("DarkUndercarriage", body, new Vector3(-.04f, .68f, 0), new Vector3(2.9f, .21f, 1.45f), .06f, honeyDark);
        HoneyDisc("SolidOakBoiler", body, new Vector3(.48f, 1.58f, 0), .58f, 1.96f, honeyWood, AlongX, .07f);
        foreach (float x in new[] { -.32f, .93f })
            HoneyDisc("FlatBrassBoilerBand", body, new Vector3(x, 1.58f, 0), .591f, .075f, honeyBrass, AlongX, .009f);
        HoneyDisc("OakFrontFace", body, new Vector3(1.51f, 1.58f, 0), .55f, .18f, honeyWood, AlongX, .045f);
        HoneyDisc("TealHeadlightRing", body, new Vector3(1.625f, 1.58f, 0), .455f, .09f, honeyTeal, AlongX, .023f);
        HoneyDisc("BrassLampBezel", body, new Vector3(1.69f, 1.58f, 0), .37f, .10f, honeyBrass, AlongX, .025f);
        HoneyDisc("IvoryLampGlass", body, new Vector3(1.753f, 1.58f, 0), .302f, .036f, honeyLamp, AlongX, .014f);
        // Open teal funnel with a substantial brass lip, as in option 3.
        var funnel = Lathe("TealWoodFunnel", new[] { V(.19f, 0), V(.20f, .055f), V(.15f, .12f),
            V(.18f, .35f), V(.245f, .55f), V(.25f, .61f) }, honeyTeal);
        Place(funnel, body, new Vector3(1.04f, 2.08f, 0));
        var lip = Lathe("BrassFunnelLip", new[] { V(.23f, 0), V(.29f, .025f), V(.29f, .16f),
            V(.275f, .19f), V(.225f, .19f), V(.21f, .16f), V(.21f, .03f), V(.23f, 0) }, honeyBrass);
        Place(lip, body, new Vector3(1.04f, 2.61f, 0));
        HoneyDisc("DarkFunnelInterior", body, new Vector3(1.04f, 2.63f, 0), .21f, .012f, honeyDark, Quaternion.identity, .003f);
        var dome = Lathe("BrassSteamDome", new[] { V(0, 0), V(.29f, 0), V(.30f, .045f), V(.255f, .08f),
            V(.255f, .23f), V(.22f, .34f), V(.14f, .43f), V(0, .46f) }, honeyBrass);
        Place(dome, body, new Vector3(-.01f, 2.10f, 0));
        Sphere("BrassDomeKnob", body, new Vector3(-.01f, 2.61f, 0), Vector3.one * .12f, honeyBrass);

        Box("CabFloor", body, new Vector3(-1.08f, 1.07f, 0), new Vector3(1.30f, .20f, 1.48f), .045f, honeyWood);
        Box("CabInteriorFloor", body, new Vector3(-1.08f, 1.19f, 0), new Vector3(1.12f, .05f, 1.30f), .018f, honeyDark);
        // True window holes: wooden posts and arched lintels, not black decals.
        foreach (float side in new[] { -1f, 1f })
        {
            var wall = Group("TealCabSide", body); wall.localPosition = new Vector3(-1.08f, 0, side * .72f);
            HoneyCabWall(wall, 1.30f, side);
        }
        foreach (float x in new[] { -1.73f, -.43f })
        {
            var wall = Group("TealCabEnd", body); wall.localPosition = new Vector3(x, 0, 0);
            wall.localRotation = Quaternion.Euler(0, x < -1 ? 90 : -90, 0);
            HoneyCabWall(wall, 1.40f, 1);
        }
        HoneyRoof(body);
        foreach (float side in new[] { -1f, 1f })
        {
            Box("WoodenCabSill", body, new Vector3(-1.08f, 1.12f, side * .79f), new Vector3(1.40f, .13f, .16f), .04f, honeyWood);
        }
        HoneyDisc("RearAxle", body, new Vector3(-.98f, .53f, 0), .10f, 1.92f, honeyDark, AlongZ, .02f);
        HoneyDisc("FrontAxle", body, new Vector3(.88f, .53f, 0), .10f, 1.92f, honeyDark, AlongZ, .02f);
        Box("RoundedOakBufferBeam", body, new Vector3(1.77f, .91f, 0), new Vector3(.32f, .40f, 1.70f), .09f, honeyWood);
        foreach (float side in new[] { -1f, 1f })
        foreach (float end in new[] { -1f, 1f })
        {
            HoneyDisc("WoodenBufferStem", body, new Vector3(end * 1.93f, .91f, side * .55f), .09f, .19f, honeyEdge, AlongX, .025f);
            HoneyDisc("RoundedWoodBuffer", body, new Vector3(end * 2.04f, .91f, side * .55f), .16f, .10f, honeyWood, AlongX, .035f);
        }
        if (combineBody) Combine(body, "HoneyOakBody");

        var wheels = new List<SuitcaseTrainMechanism.Wheel>();
        var gear = new List<SuitcaseTrainMechanism.SideGear>();
        foreach (float side in new[] { -1f, 1f })
        {
            float phase = side < 0 ? 0 : 90;
            var rear = HoneyWheel(root, wheels, new Vector3(-.98f, .53f, side * .91f), phase, side);
            var front = HoneyWheel(root, wheels, new Vector3(.88f, .53f, side * .91f), phase, side);
            var rod = Box("AnimatedMapleCouplingRod", root, Vector3.zero, new Vector3(1, .115f, .09f), .035f, honeyWood);
            gear.Add(new SuitcaseTrainMechanism.SideGear { rearPin = rear, frontPin = front, couplingRod = rod });
        }
        return new SuitcaseTrainMechanism.Engine { body = root.gameObject, wheels = wheels.ToArray(), gear = gear.ToArray() };
    }

    static void HoneyCabWall(Transform parent, float width, float side)
    {
        const float bottom = 1.18f, top = 2.62f, sill = 1.49f, spring = 2.19f, radius = .205f;
        Box("LowerPaintedPanel", parent, new Vector3(0, (bottom + sill) / 2, 0), new Vector3(width, sill - bottom, .12f), .025f, honeyTeal);
        float half = width / 2, c = width * .25f, edge = c + radius;
        foreach (float sign in new[] { -1f, 1f })
            Box("OuterWindowPost", parent, new Vector3(sign * (half + edge) / 2, (sill + top) / 2, 0),
                new Vector3(half - edge, top - sill, .12f), .018f, honeyTeal);
        Box("MiddleWindowPost", parent, new Vector3(0, (sill + top) / 2, 0), new Vector3(2 * (c - radius), top - sill, .12f), .018f, honeyTeal);
        foreach (float x in new[] { -c, c })
        {
            // Fill above each half-circle while keeping the window itself open.
            var v = new List<Vector3>(); var tr = new List<int>(); var uv = new List<Vector2>();
            for (int i = 0; i <= 24; i++)
            {
                float a = Mathf.PI * i / 24, px = x + Mathf.Cos(a) * radius, py = spring + Mathf.Sin(a) * radius;
                v.Add(new Vector3(px, py, -.06f)); v.Add(new Vector3(px, top, -.06f));
                v.Add(new Vector3(px, py, .06f)); v.Add(new Vector3(px, top, .06f));
                for (int j = 0; j < 4; j++) uv.Add(new Vector2(px, j % 2 == 0 ? py : top));
                if (i < 24)
                {
                    int k = i * 4;
                    tr.AddRange(new[] { k,k+5,k+1,k,k+4,k+5, k+2,k+3,k+7,k+2,k+7,k+6,
                        k,k+2,k+6,k,k+6,k+4 });
                }
            }
            MeshObject("ArchedTealLintel", parent, Vector3.zero, Finish(Key("HoneyWindow", new[] { width, x }), v, tr, null, uv), honeyTeal);
            float z = side * .078f;
            Bar("MapleWindowLeft", parent, new Vector3(x - radius, sill, z), new Vector3(x - radius, spring, z), .026f, honeyWood);
            Bar("MapleWindowRight", parent, new Vector3(x + radius, sill, z), new Vector3(x + radius, spring, z), .026f, honeyWood);
            Bar("MapleWindowSill", parent, new Vector3(x - radius, sill, z), new Vector3(x + radius, sill, z), .026f, honeyWood);
            var trimVertices = new List<Vector3>(); var trimNormals = new List<Vector3>();
            var trimTriangles = new List<int>(); var trimUV = new List<Vector2>();
            for (int i = 0; i <= 32; i++)
            {
                float a = i * Mathf.PI / 32;
                var radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                var center = new Vector3(x, spring, z) + radial * radius;
                for (int j = 0; j <= 10; j++)
                {
                    float b = j * 2 * Mathf.PI / 10;
                    var n = radial * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                    trimVertices.Add(center + n * .026f); trimNormals.Add(n); trimUV.Add(new Vector2(i / 32f, j / 10f));
                    if (i < 32 && j < 10)
                    {
                        int k = i * 11 + j;
                        trimTriangles.AddRange(new[] { k,k+11,k+12,k,k+12,k+1 });
                    }
                }
            }
            MeshObject("SmoothMapleWindowArch", parent, Vector3.zero,
                Finish(Key("HoneySmoothArch", new[] { width, x, side }), trimVertices, trimTriangles, trimNormals, trimUV), honeyWood);
        }
    }

    static void HoneyRoof(Transform body)
    {
        var v = new List<Vector3>(); var tr = new List<int>(); var uv = new List<Vector2>();
        const float halfWidth = .91f, halfLength = .79f, thickness = .15f;
        for (int i = 0; i <= 32; i++)
        {
            float z = Mathf.Lerp(-halfWidth, halfWidth, i / 32f), y = .22f * (1 - z * z / (halfWidth * halfWidth));
            v.Add(new Vector3(-halfLength, y, z)); v.Add(new Vector3(halfLength, y, z));
            v.Add(new Vector3(-halfLength, y - thickness, z)); v.Add(new Vector3(halfLength, y - thickness, z));
            uv.Add(new Vector2(0, z)); uv.Add(new Vector2(1, z)); uv.Add(new Vector2(0, z)); uv.Add(new Vector2(1, z));
            if (i < 32)
            {
                int p = i * 4;
                tr.AddRange(new[] { p,p+4,p+5,p,p+5,p+1, p+2,p+3,p+7,p+2,p+7,p+6,
                    p,p+2,p+6,p,p+6,p+4, p+1,p+5,p+7,p+1,p+7,p+3 });
            }
        }
        tr.AddRange(new[] { 0,1,3,0,3,2,128,130,131,128,131,129 });
        MeshObject("ThickCurvedMapleRoof", body, new Vector3(-1.08f, 2.71f, 0), Finish("HoneyThickRoof", v, tr, null, uv), honeyWood);
    }

    static Transform HoneyDisc(string name, Transform parent, Vector3 p, float radius, float height, Material mat, Quaternion rotation, float bevel)
    {
        string key = Key("HoneyPlanarDisc", new[] { radius, height, bevel });
        if (!meshes.TryGetValue(key, out var mesh))
        {
            mesh = Object.Instantiate(TurnedMesh(new[] { V(0,-height/2),V(radius-bevel,-height/2),V(radius,-height/2+bevel),
                V(radius,height/2-bevel),V(radius-bevel,height/2),V(0,height/2) }));
            var vertices = mesh.vertices; var normals = mesh.normals; var uv = mesh.uv;
            for (int i = 0; i < vertices.Length; i++)
                if (Mathf.Abs(normals[i].y) > .45f) uv[i] = new Vector2(vertices[i].x / (2 * radius) + .5f, vertices[i].z / (2 * radius) + .5f);
                else uv[i] = new Vector2(vertices[i].y / Mathf.Max(height, .01f) + .5f, uv[i].x * 2);
            mesh.uv = uv; mesh.name = key; mesh.RecalculateTangents();
            mesh = Save(mesh, Folder + "/Meshes/" + key + ".asset"); meshes[key] = mesh;
        }
        var t = MeshObject(name, parent, p, mesh, mat); t.localRotation = rotation; return t;
    }

    static Transform HoneyWheel(Transform root, List<SuitcaseTrainMechanism.Wheel> wheels, Vector3 p, float phase, float side)
    {
        const float radius = .53f;
        var pivot = Group("RedWoodWheel" + wheels.Count, root); pivot.localPosition = p;
        var visual = Group("SolidWoodDisc", pivot);
        HoneyDisc("RedWoodWheel", visual, Vector3.zero, radius, .23f, honeyRed, AlongZ, .055f);
        Torus("RoutedConcentricGroove", visual, new Vector3(0, 0, side * .116f), .413f, .010f, honeyEdge, AlongZ);
        HoneyDisc("InsetRedFace", visual, new Vector3(0, 0, side * .117f), .385f, .013f, honeyRed, AlongZ, .004f);
        HoneyDisc("CentralBrassAxle", visual, new Vector3(0, 0, side * .14f), .095f, .045f, honeyBrass, AlongZ, .012f);
        Combine(visual, "HoneyOakWheel" + (side < 0 ? "Left" : "Right"));
        wheels.Add(new SuitcaseTrainMechanism.Wheel { pivot = pivot, radius = radius, phaseDegrees = phase });
        var pin = Group("EccentricBrassPin", pivot); pin.localPosition = new Vector3(.205f, 0, side * .22f);
        HoneyDisc("CrankCollar", pin, Vector3.zero, .13f, .10f, honeyBrass, AlongZ, .025f);
        HoneyDisc("BrassPinCap", pin, new Vector3(0, 0, side * .075f), .093f, .05f, honeyBrass, AlongZ, .018f);
        return pin;
    }
}
#endif
