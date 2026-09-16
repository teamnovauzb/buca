using UnityEngine;

/// <summary>
/// Adds a clear, optional high-score line to levels whose central mechanic can
/// otherwise be bypassed with no trade-off. Side routes remain valid, but the
/// gold markers make the designed boost/launch route visibly more rewarding.
/// </summary>
public static class PreferredRouteBuilder
{
    const int PointsPerMarker = 125;
    static Material _goldMaterial;
    static Material _goldFloorMaterial;

    public static void Install(GameObject levelRoot, int levelNumber)
    {
        if (levelRoot == null || levelRoot.transform.Find("PreferredRoute_Bonus") != null)
            return;

        Vector3[] route = GetRoute(levelNumber);
        if (route == null || route.Length == 0) return;

        var routeRoot = new GameObject("PreferredRoute_Bonus");
        routeRoot.transform.SetParent(levelRoot.transform, false);

        for (int i = 0; i < route.Length; i++)
            CreateMarker(routeRoot.transform, route[i], i + 1);
    }

    static Vector3[] GetRoute(int levelNumber)
    {
        // Coordinates deliberately follow each level's existing central
        // boost/launch solution. They do not create a new required mechanic.
        switch (levelNumber)
        {
            case 10: // boosted route on the right of the divider
                return Route(2.30f, -2.00f, 2.30f, 0.70f, 1.45f, 3.25f);
            case 15: // diagonal launch-pad route
                return Route(2.60f, -2.20f, 0.80f, 0.75f, -1.35f, 3.35f);
            case 21: // three-pad chain through the middle
                return Route(-1.05f, -1.80f, 0.85f, 1.10f, -1.05f, 4.00f);
            case 25: // central speed-boost weave
                return Route(0.00f, -3.00f, 0.65f, 0.35f, -0.35f, 3.10f);
            case 26: // central launch route between the spinners
                return Route(0.00f, -2.00f, 0.00f, 0.35f, 0.00f, 3.05f);
            default:
                return null;
        }
    }

    static Vector3[] Route(float x1, float z1, float x2, float z2, float x3, float z3)
    {
        return new[]
        {
            new Vector3(x1, 0.22f, z1),
            new Vector3(x2, 0.22f, z2),
            new Vector3(x3, 0.22f, z3)
        };
    }

    static void CreateMarker(Transform parent, Vector3 localPosition, int index)
    {
        // A bright floating orb is the collectible itself.
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = $"GoldRouteMarker_{index}_Plus{PointsPerMarker}";
        orb.transform.SetParent(parent, false);
        orb.transform.localPosition = localPosition;
        orb.transform.localScale = Vector3.one * 0.34f;

        var renderer = orb.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = GetGoldMaterial();

        var collider = orb.GetComponent<SphereCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
            // Slightly generous collection radius makes the reward readable and
            // fair without changing puck movement or collisions.
            collider.radius = 1.45f;
        }

        var pickup = orb.AddComponent<ScorePickup>();
        pickup.bonusPoints = PointsPerMarker;
        pickup.idleSpinSpeed = 115f;
        pickup.idleBobAmplitude = 0.07f;

        // A flat gold halo remains readable against both wood and neon boards.
        GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        halo.name = "GoldRouteHalo";
        halo.transform.SetParent(orb.transform, false);
        halo.transform.localPosition = new Vector3(0f, -0.60f, 0f);
        halo.transform.localScale = new Vector3(1.75f, 0.035f, 1.75f);
        var haloCollider = halo.GetComponent<Collider>();
        if (haloCollider != null) Object.Destroy(haloCollider);
        var haloRenderer = halo.GetComponent<MeshRenderer>();
        if (haloRenderer != null) haloRenderer.sharedMaterial = GetGoldFloorMaterial();
    }

    static Material GetGoldMaterial()
    {
        if (_goldMaterial != null) return _goldMaterial;
        _goldMaterial = CreateMaterial(
            "Preferred Route Gold",
            new Color(1f, 0.62f, 0.06f, 1f),
            new Color(2.6f, 1.15f, 0.08f, 1f));
        return _goldMaterial;
    }

    static Material GetGoldFloorMaterial()
    {
        if (_goldFloorMaterial != null) return _goldFloorMaterial;
        _goldFloorMaterial = CreateMaterial(
            "Preferred Route Halo",
            new Color(0.90f, 0.38f, 0.025f, 1f),
            new Color(1.25f, 0.40f, 0.02f, 1f));
        return _goldFloorMaterial;
    }

    static Material CreateMaterial(string name, Color baseColor, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var material = new Material(shader)
        {
            name = name,
            hideFlags = HideFlags.HideAndDontSave,
            color = baseColor
        };

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        material.renderQueue = 3000;
        return material;
    }
}
