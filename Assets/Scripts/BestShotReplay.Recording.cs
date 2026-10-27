using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class BestShotReplay
{
    // Only transform/visibility samples are recorded. Meshes, materials, cameras,
    // display targets and renderer slots are authored assets, never generated here.
    const int MaxFrames = 602;
    const float SampleInterval = 1f / 30f;
    const float MaxRecordedSeconds = 20f;
    public Transform[] environmentRoots;

    struct VisualPose
    {
        public Vector3 position, scale;
        public Quaternion rotation;
        public bool visible;
    }
    sealed class Visual
    {
        public Mesh mesh;
        public Material[] materials;
        public VisualPose initial;
        public UnityEngine.Rendering.ShadowCastingMode shadows;
        public bool receiveShadows;
    }
    sealed class Clip
    {
        public Visual[] moving, scenery;
        public VisualPose[] poses;
        public readonly float[] times = new float[MaxFrames];
        public int count, level, strokes;
        public float distance, fov, goalDistance;
        public bool completedGoal;
        public Vector3 cameraPosition;
        public Quaternion cameraRotation;
        public float Duration => count > 0 ? times[count - 1] : 0;
    }
    Clip _working = new Clip(), _best;
    readonly List<Clip> _levelShots = new List<Clip>();
    int _replayLevel;
    Vector3 _recordedGoal;
    MeshRenderer[] _sources;
    Transform _recordedPuck;
    Vector3 _lastPuck;
    float _recordStarted, _nextSample;
    bool _capturing;

    public bool HasBestShot => _best != null && _best.level > 1 && _best.count > 2;
    public int BestLevelNumber => HasBestShot ? _best.level : 0;
    public int BestStrokeCount => HasBestShot ? _best.strokes : 0;
    public int BestFrameCount => HasBestShot ? _best.count : 0;
    public bool IsRecording => _capturing;

    // World 1 keeps its campaign highlight. City replays belong to the current
    // level, including completed misses when the player has not sunk a shot yet.
    public void BeginLevel(int levelNumber)
    {
        CancelRecording();
        if (levelNumber > 30 || _replayLevel > 30)
        {
            _best = null;
            _levelShots.Clear();
        }
        _replayLevel = levelNumber;
    }

    public bool HasBestShotForLevel(int levelNumber)
    {
        return HasBestShot && (levelNumber > 30 ? _best.level == levelNumber : _best.level <= 30);
    }

    public void BeginShot(GameObject board, GameObject puck, int levelNumber,
        Vector3 cameraPosition, Quaternion cameraRotation, float cameraFov)
    {
        if (_replayLevel != levelNumber) BeginLevel(levelNumber);
        else CancelRecording();
        // Level 1 is introductory and does not qualify for the best-shot replay.
        if (levelNumber <= 1 || IsShowing || board == null || puck == null) return;
        var moving = new List<MeshRenderer>();
        AddRenderers(board.transform, moving, false);
        AddRenderers(puck.transform, moving, false);
        var scenery = new List<MeshRenderer>();
        if (environmentRoots != null)
            foreach (var root in environmentRoots) AddRenderers(root, scenery, true);
        if(levelNumber>30)
        {
            var theme=FindAnyObjectByType<EveningGameplayTheme>();
            if(theme!=null && theme.station!=null)
            {
                var city=new List<MeshRenderer>();AddRenderers(theme.station.transform,city,true);
                foreach(var renderer in city)if(!scenery.Contains(renderer))scenery.Add(renderer);
            }
        }
        if (moving.Count == 0 || savedRenderers == null || moving.Count + scenery.Count > savedRenderers.Length)
        {
            Debug.LogWarning("[BestShotReplay] Saved renderer capacity is too small for this board; replay omitted.");
            return;
        }
        _working.count = 0;
        _working.distance = 0;
        _working.level = levelNumber;
        _working.completedGoal = false;
        var hole = board.transform.Find("Hole");
        _recordedGoal = hole != null ? hole.position : Vector3.positiveInfinity;
        _working.cameraPosition = cameraPosition;
        _working.cameraRotation = cameraRotation;
        _working.fov = cameraFov;
        _sources = moving.ToArray();
        _working.moving = CaptureVisuals(_sources);
        _working.scenery = CaptureVisuals(scenery.ToArray());
        int capacity = MaxFrames * _sources.Length;
        if (_working.poses == null || _working.poses.Length != capacity)
            _working.poses = new VisualPose[capacity];
        _recordedPuck = puck.transform;
        _lastPuck = _recordedPuck.position;
        _recordStarted = Time.time;
        _nextSample = SampleInterval;
        _capturing = true;
        CaptureFrame(0);
    }

    static void AddRenderers(Transform root, List<MeshRenderer> output, bool visibleOnly)
    {
        if (root == null) return;
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || renderer.GetComponent<TMPro.TMP_Text>() != null) continue;
            if (visibleOnly && (!renderer.enabled || !renderer.gameObject.activeInHierarchy)) continue;
            output.Add(renderer);
        }
    }
    static Visual[] CaptureVisuals(MeshRenderer[] sources)
    {
        var result = new Visual[sources.Length];
        for (int i = 0; i < sources.Length; i++)
        {
            var source = sources[i];
            result[i] = new Visual { mesh = source.GetComponent<MeshFilter>().sharedMesh,
                materials = source.sharedMaterials, initial = ReadPose(source),
                shadows = source.shadowCastingMode, receiveShadows = source.receiveShadows };
        }
        return result;
    }
    static VisualPose ReadPose(MeshRenderer source)
    {
        if (source == null) return new VisualPose { rotation = Quaternion.identity };
        return new VisualPose { position = source.transform.position, rotation = source.transform.rotation,
            scale = source.transform.lossyScale, visible = source.enabled && source.gameObject.activeInHierarchy };
    }
    void CaptureFrame(float elapsed)
    {
        if (!_capturing || _recordedPuck == null) return;
        if (elapsed > MaxRecordedSeconds || _working.count >= MaxFrames)
        {
            // Never present a truncated flight as a completed shot.
            CancelRecording();
            return;
        }
        int frame = _working.count++;
        _working.times[frame] = elapsed;
        for (int i = 0; i < _sources.Length; i++)
            _working.poses[frame * _sources.Length + i] = ReadPose(_sources[i]);
        float step = Vector3.Distance(_lastPuck, _recordedPuck.position);
        if (step < 1.5f) _working.distance += step;
        _lastPuck = _recordedPuck.position;
    }
    void RecordLateFrame()
    {
        if (!_capturing) return;
        if (_recordedPuck == null) { CancelRecording(); return; }
        float elapsed = Time.time - _recordStarted;
        if (elapsed < _nextSample) return;
        CaptureFrame(elapsed);
        _nextSample = elapsed + SampleInterval;
    }
    public void CommitGoal(int strokes)
    {
        CommitShot(strokes, true);
    }
    public void CommitAttempt(int strokes)
    {
        // Preserve World 1's successful-shot selection rules.
        if (_replayLevel <= 30) { CancelRecording(); return; }
        CommitShot(strokes, false);
    }
    void CommitShot(int strokes, bool completedGoal)
    {
        if (!_capturing) return;
        CaptureFrame(Time.time - _recordStarted);
        if (_capturing && _working.count > 2 && _working.distance > .2f && strokes > 0)
        {
            _working.strokes = strokes;
            _working.completedGoal = completedGoal;
            var toGoal = _recordedPuck.position - _recordedGoal;
            _working.goalDistance = new Vector2(toGoal.x, toGoal.z).sqrMagnitude;
            if (_replayLevel > 30)
            {
                // Retain this level's attempts so Undo can restore the previous
                // best instead of replaying a shot the player has taken back.
                _levelShots.Add(_working);
                if (IsBetterCityShot(_working, _best)) _best = _working;
                _working = new Clip();
            }
            else if (_best == null || strokes < _best.strokes ||
                (strokes == _best.strokes && _working.distance > _best.distance))
            {
                var previous = _best;
                _best = _working;
                _working = previous ?? new Clip();
            }
        }
        CancelRecording();
    }
    static bool IsBetterCityShot(Clip candidate, Clip best)
    {
        if (best == null) return true;
        if (candidate.completedGoal != best.completedGoal) return candidate.completedGoal;
        if (candidate.completedGoal && candidate.strokes != best.strokes)
            return candidate.strokes < best.strokes;
        if (!candidate.completedGoal && candidate.goalDistance != best.goalDistance)
            return candidate.goalDistance < best.goalDistance;
        return candidate.distance > best.distance;
    }
    public void DiscardShotsAfter(int strokes)
    {
        CancelRecording();
        if (_replayLevel <= 30) return;
        _best = null;
        for (int i = _levelShots.Count - 1; i >= 0; i--)
        {
            var shot = _levelShots[i];
            if (shot.strokes > strokes) _levelShots.RemoveAt(i);
            else if (IsBetterCityShot(shot, _best)) _best = shot;
        }
    }
    public void CancelRecording()
    {
        _capturing = false;
        _sources = null;
        _recordedPuck = null;
    }
    public void ClearRun()
    {
        CancelRecording();
        Hide(false);
        _best = null;
        _levelShots.Clear();
        _replayLevel = 0;
    }
    void PrepareRecordedVisuals()
    {
        int slot = 0;
        foreach (var visual in _best.moving) PrepareSlot(slot++, visual);
        foreach (var visual in _best.scenery) PrepareSlot(slot++, visual);
        for (int i = slot; i < savedRenderers.Length; i++) savedRenderers[i].enabled = false;
        playbackCamera.transform.localPosition = _best.cameraPosition;
        playbackCamera.transform.localRotation = _best.cameraRotation;
        playbackCamera.fieldOfView = _best.fov;
        playbackCamera.aspect = (float)replayTexture.width / replayTexture.height;
    }
    void PrepareSlot(int index, Visual visual)
    {
        savedMeshes[index].sharedMesh = visual.mesh;
        savedRenderers[index].sharedMaterials = visual.materials;
        savedRenderers[index].shadowCastingMode = visual.shadows;
        savedRenderers[index].receiveShadows = visual.receiveShadows;
        ApplyPose(index, visual.initial);
    }
    void ApplyPose(int index, VisualPose pose)
    {
        var item = savedMeshes[index].transform;
        item.localPosition = pose.position;
        item.localRotation = pose.rotation;
        item.localScale = pose.scale;
        savedRenderers[index].enabled = pose.visible;
    }
    public void SampleReplay(float normalizedTime)
    {
        if (!HasBestShot) return;
        float start = Mathf.Max(0, _best.Duration - 4.5f);
        float time = Mathf.Lerp(start, _best.Duration, Mathf.Clamp01(normalizedTime));
        int a = 0;
        while (a + 1 < _best.count && _best.times[a + 1] <= time) a++;
        int b = Mathf.Min(a + 1, _best.count - 1);
        float blend = Mathf.InverseLerp(_best.times[a], _best.times[b], time);
        int count = _best.moving.Length;
        for (int i = 0; i < count; i++)
        {
            var first = _best.poses[a * count + i];
            var second = _best.poses[b * count + i];
            // A teleport is an instantaneous jump, not a flight through a wall.
            bool jump = Vector3.SqrMagnitude(first.position - second.position) > 2.25f;
            ApplyPose(i, new VisualPose {
                position = jump ? (blend < .5f ? first.position : second.position) : Vector3.Lerp(first.position, second.position, blend),
                rotation = Quaternion.Slerp(first.rotation, second.rotation, blend),
                scale = Vector3.Lerp(first.scale, second.scale, blend), visible = blend < .5f ? first.visible : second.visible });
        }
        SetProgress(normalizedTime);
    }
}
