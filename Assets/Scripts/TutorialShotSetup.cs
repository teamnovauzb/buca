using System;
using UnityEngine;

/// <summary>Authored demonstration shots for boards with an obstructed direct approach.</summary>
public sealed class TutorialShotSetup : MonoBehaviour
{
    [Serializable]
    public struct Shot
    {
        public string mechanic;
        public float yaw;
        [Range(0,1)] public float firstPower, secondPower;
    }
    public Shot[] shots;
    public bool TryGet(string mechanic, out Shot shot)
    {
        if(shots!=null)foreach(var candidate in shots)
            if(candidate.mechanic==mechanic){shot=candidate;return true;}
        shot=default;return false;
    }
}
