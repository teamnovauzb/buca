#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Deterministic validation for the trajectory preview's planar reflection
/// math. Runs after script reload and is also available from the RealBuca menu.
/// </summary>
[InitializeOnLoad]
public static class TrajectoryPreviewValidation
{
    const float Tolerance = 0.0001f;

    static TrajectoryPreviewValidation()
    {
        EditorApplication.delayCall += ValidateAfterReload;
    }

    [MenuItem("RealBuca/Validate Trajectory Preview")]
    public static void ValidateFromMenu()
    {
        int cases = RunValidation();
        EditorUtility.DisplayDialog("Trajectory preview validated",
            $"Passed {cases} direction, angle, contaminated-normal, and moving-surface cases.",
            "OK");
    }

    // Callable with Unity -batchmode -executeMethod for CI or release checks.
    public static void RunBatch()
    {
        int cases = RunValidation();
        Debug.Log($"[TrajectoryPreviewValidation] PASS: {cases} deterministic cases.");
    }

    static void ValidateAfterReload()
    {
        try
        {
            int cases = RunValidation();
            Debug.Log($"[TrajectoryPreviewValidation] PASS: {cases} deterministic cases.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    static int RunValidation()
    {
        int cases = 0;

        // Static walls: angle-in must equal angle-out for a wide sweep of
        // shallow, direct, and steep approaches.
        Vector3 wallNormal = Vector3.forward;
        for (int degrees = -85; degrees <= 85; degrees++)
        {
            float radians = degrees * Mathf.Deg2Rad;
            Vector3 incoming = new Vector3(Mathf.Sin(radians), 0f, -Mathf.Cos(radians)) * 12f;
            Vector3 expected = Vector3.Reflect(incoming, wallNormal);

            Vector3 actual = PuckController.CalculatePlanarBounceVelocity(
                incoming, wallNormal, Vector3.zero);
            AssertNear(expected, actual, $"static wall at {degrees} degrees");
            cases++;

            // Beveled/edge contacts can report a Y component even though the
            // puck is constrained to XZ. It must not alter the planar result.
            Vector3 contaminatedNormal = new Vector3(0f, 0.8f, 1f).normalized;
            actual = PuckController.CalculatePlanarBounceVelocity(
                incoming, contaminatedNormal, Vector3.zero);
            AssertNear(expected, actual, $"beveled normal at {degrees} degrees");
            cases++;
        }

        // Moving wall: reflect relative velocity, then restore wall velocity.
        Vector3 movingIncoming = new Vector3(-6f, 0f, 2f);
        Vector3 movingNormal = Vector3.right;
        Vector3 surfaceVelocity = new Vector3(1.5f, 0f, 0f);
        Vector3 movingExpected = new Vector3(9f, 0f, 2f);
        Vector3 movingActual = PuckController.CalculatePlanarBounceVelocity(
            movingIncoming, movingNormal, surfaceVelocity);
        AssertNear(movingExpected, movingActual, "moving wall relative velocity");
        cases++;

        // A vertical-only normal is a floor/ceiling contact and must leave the
        // horizontal trajectory unchanged.
        Vector3 planar = new Vector3(3f, 0f, -7f);
        Vector3 verticalActual = PuckController.CalculatePlanarBounceVelocity(
            planar, Vector3.up, Vector3.zero);
        AssertNear(planar, verticalActual, "vertical-only normal");
        cases++;

        // If the puck is already travelling away from a surface, do not reflect
        // it back inward (important for zero-distance post-bounce contacts).
        Vector3 departing = new Vector3(2f, 0f, 4f);
        Vector3 departingActual = PuckController.CalculatePlanarBounceVelocity(
            departing, Vector3.forward, Vector3.zero);
        AssertNear(departing, departingActual, "departing contact");
        cases++;

        // The project's real puck material has 0.75 restitution and 0.1
        // dynamic friction. Verify that the visual rebound accounts for both
        // its normal energy loss and its tangential friction impulse.
        Vector3 materialIncoming = new Vector3(4f, 0f, -10f);
        Vector3 materialExpected = new Vector3(2.25f, 0f, 7.5f);
        Vector3 materialActual = PuckController.CalculatePlanarBounceVelocity(
            materialIncoming, Vector3.forward, Vector3.zero, 0.75f, 0.1f);
        AssertNear(materialExpected, materialActual, "puck physics material response");
        cases++;

        // A sufficiently small sideways component should be fully consumed by
        // friction rather than creating a false diagonal preview segment.
        Vector3 frictionStopActual = PuckController.CalculatePlanarBounceVelocity(
            new Vector3(0.5f, 0f, -10f), Vector3.forward, Vector3.zero, 0.75f, 0.1f);
        AssertNear(new Vector3(0f, 0f, 7.5f), frictionStopActual,
            "material friction stopping tangential motion");
        cases++;

        // Below the project's PhysX bounce threshold, restitution is disabled
        // and the preview must show a slide/contact response instead of a
        // false full bounce. ResolvePreviewCollisionVelocity supplies zero
        // restitution for this case.
        Vector3 thresholdActual = PuckController.CalculatePlanarBounceVelocity(
            new Vector3(4f, 0f, -1f), Vector3.forward, Vector3.zero, 0f, 0.1f);
        AssertNear(new Vector3(3.9f, 0f, 0f), thresholdActual,
            "sub-threshold glancing contact");
        cases++;

        // SphereCastNonAlloc may return two corner faces in either order. The
        // accumulated normal, and therefore the rebound, must be identical.
        Vector3 cornerDirection = new Vector3(-1f, 0f, -1f).normalized;
        Vector3 cornerNormalA = Vector3.zero;
        cornerNormalA = PuckController.AccumulatePlanarContactNormal(
            cornerDirection, cornerNormalA, Vector3.right);
        cornerNormalA = PuckController.AccumulatePlanarContactNormal(
            cornerDirection, cornerNormalA, Vector3.forward);

        Vector3 cornerNormalB = Vector3.zero;
        cornerNormalB = PuckController.AccumulatePlanarContactNormal(
            cornerDirection, cornerNormalB, Vector3.forward);
        cornerNormalB = PuckController.AccumulatePlanarContactNormal(
            cornerDirection, cornerNormalB, Vector3.right);
        AssertNear(cornerNormalA.normalized, cornerNormalB.normalized,
            "coincident corner hit order");
        cases++;

        Vector3 cornerIncoming = cornerDirection * 10f;
        Vector3 cornerOutgoing = PuckController.CalculatePlanarBounceVelocity(
            cornerIncoming, cornerNormalA.normalized, Vector3.zero);
        AssertNear(-cornerIncoming, cornerOutgoing, "coincident corner rebound");
        cases++;

        cases += ValidateLevelPrefabCoverage();

        return cases;
    }

    static int ValidateLevelPrefabCoverage()
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:Prefab", new[] { "Assets/Prefabs/Levels" });
        if (guids.Length == 0)
            throw new InvalidOperationException(
                "Trajectory validation found no level prefabs.");

        int validated = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject level = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (level == null) continue;

            Transform puckStart = level.transform.Find("PuckStart");
            if (puckStart == null)
                throw new InvalidOperationException(
                    $"Trajectory validation: {path} has no PuckStart marker.");

            Collider[] colliders = level.GetComponentsInChildren<Collider>(true);
            int solidColliderCount = 0;
            foreach (Collider collider in colliders)
                if (collider != null && !collider.isTrigger) solidColliderCount++;

            if (solidColliderCount == 0)
                throw new InvalidOperationException(
                    $"Trajectory validation: {path} has no solid trajectory surfaces.");
            validated++;
        }

        if (validated != guids.Length)
            throw new InvalidOperationException(
                $"Trajectory validation loaded {validated} of {guids.Length} level prefabs.");
        return validated;
    }

    static void AssertNear(Vector3 expected, Vector3 actual, string scenario)
    {
        if ((expected - actual).sqrMagnitude > Tolerance * Tolerance)
            throw new InvalidOperationException(
                $"Trajectory validation failed for {scenario}. Expected {expected}, got {actual}.");
        if (Mathf.Abs(actual.y) > Tolerance)
            throw new InvalidOperationException(
                $"Trajectory validation left the gameplay plane for {scenario}: {actual}.");
    }
}
#endif
