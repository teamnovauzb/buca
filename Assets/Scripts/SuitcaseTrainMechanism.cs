using System;
using UnityEngine;

/// <summary>Distance-driven wheels and quartered crank gear for both suitcase locomotives.</summary>
public sealed class SuitcaseTrainMechanism : MonoBehaviour
{
    [Serializable] public class Wheel
    {
        public Transform pivot;
        public float radius;
        public float phaseDegrees;
    }
    [Serializable] public class SideGear
    {
        public Transform rearPin, frontPin, couplingRod, driveRod, crosshead;
        public float sliderHeight, rodLength;
    }
    [Serializable] public class Engine
    {
        public GameObject body;
        public Wheel[] wheels;
        public SideGear[] gear;
    }
    public Engine woodland, moonlight;
    public bool IsMoonlight => moonlight != null && moonlight.body != null && moonlight.body.activeSelf;

    public void Pose(float routeDistance, float trainScale, bool inWorldTwo)
    {
        // Use local travelled distance: a whole circumference is exactly one revolution.
        float distance = routeDistance / Mathf.Max(.001f, Mathf.Abs(trainScale));
        PoseEngine(woodland, distance);
        PoseEngine(moonlight, distance);
        if (woodland.body.activeSelf == inWorldTwo) woodland.body.SetActive(!inWorldTwo);
        if (moonlight.body.activeSelf != inWorldTwo) moonlight.body.SetActive(inWorldTwo);
    }

    static void PoseEngine(Engine engine, float distance)
    {
        if (engine == null || engine.body == null) return;
        foreach (var wheel in engine.wheels)
        {
            float angle = wheel.phaseDegrees - distance / wheel.radius * Mathf.Rad2Deg;
            wheel.pivot.localRotation = Quaternion.AngleAxis(Mathf.Repeat(angle, 360), Vector3.forward);
        }
        Transform root = engine.body.transform;
        foreach (var side in engine.gear)
        {
            Vector3 rear = root.InverseTransformPoint(side.rearPin.position);
            Vector3 front = root.InverseTransformPoint(side.frontPin.position);
            Link(side.couplingRod, rear, front);
            // Wooden toy engines use a coupling bar without a separate piston.
            if (side.crosshead == null || side.driveRod == null) continue;
            // Slider-crank geometry keeps the piston on its horizontal guide throughout 360°.
            float dy = front.y - side.sliderHeight;
            Vector3 slider = new Vector3(front.x + Mathf.Sqrt(Mathf.Max(0, side.rodLength * side.rodLength - dy * dy)),
                side.sliderHeight, front.z);
            side.crosshead.localPosition = slider;
            Link(side.driveRod, front, slider);
        }
    }

    static void Link(Transform rod, Vector3 a, Vector3 b)
    {
        Vector3 delta = b - a;
        rod.localPosition = (a + b) * .5f;
        rod.localRotation = Quaternion.FromToRotation(Vector3.right, delta.normalized);
        rod.localScale = new Vector3(delta.magnitude, 1, 1);
    }
}
