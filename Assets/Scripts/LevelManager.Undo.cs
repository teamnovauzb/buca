using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public partial class LevelManager
{
    sealed class ShotUndo
    {
        public readonly List<Pose> path=new List<Pose>();
        public Vector3 position;
        public Quaternion rotation;
        public int shots, lives, maxLives, bonus, lit;
        public RailLight[] rails;
        public bool[] railLit;
        public ScorePickup[] pickups;
        public bool[] collected;
    }
    readonly Stack<ShotUndo> _undoShots=new Stack<ShotUndo>();
    bool _undoWasHeld, _rewindingShot;
    Coroutine _rewindRoutine;
    ShotUndo _recordingShot;
    bool _undoKinematic, _undoCollisions;
    public bool IsRewindingShot => _rewindingShot;
    void FixedUpdate()
    {
        if(_recordingShot==null || _rewindingShot || puck==null)return;
        if(!_undoShots.Contains(_recordingShot)){_recordingShot=null;return;}
        var pose=new Pose(puck.transform.position,puck.transform.rotation);
        var path=_recordingShot.path;
        if(path.Count==0 || Vector3.SqrMagnitude(path[path.Count-1].position-pose.position)>.000001f || Quaternion.Angle(path[path.Count-1].rotation,pose.rotation)>.1f)
            path.Add(pose);
        if(!_shotInProgress && path.Count>1 && puckRigidbody.linearVelocity.sqrMagnitude<.0001f)_recordingShot=null;
    }
    void CancelShotRewind()
    {
        if (bestShotReplay != null) bestShotReplay.CancelRecording();
        if(_rewindRoutine!=null)StopCoroutine(_rewindRoutine);
        _rewindRoutine=null;_recordingShot=null;
        if(_rewindingShot && puckRigidbody!=null){puckRigidbody.isKinematic=_undoKinematic;puckRigidbody.detectCollisions=_undoCollisions;}
        _rewindingShot=false;
    }
    public bool CanUndoShot => GameplayInputAllowed && !_shotInProgress && _undoShots.Count>0
        && puckRigidbody!=null && puckRigidbody.linearVelocity.sqrMagnitude<.0625f;

    // Capture before velocity changes, for both mouse and cabinet shots.
    public void CaptureUndoShot()
    {
        if(!GameplayInputAllowed || _currentInstance==null || puck==null) return;
        if(bestShotReplay!=null)
            bestShotReplay.BeginShot(_currentInstance,puck,_currentIndex+1,_camRestPos,_camRestRot,_baseFov);
        var state=new ShotUndo { position=puck.transform.position, rotation=puck.transform.rotation,
            shots=_shotCount, lives=_lives, maxLives=_currentMaxLives, bonus=_bonusScoreThisLevel, lit=_litRailCount,
            rails=_currentInstance.GetComponentsInChildren<RailLight>(true),
            pickups=_currentInstance.GetComponentsInChildren<ScorePickup>(true) };
        state.railLit=new bool[state.rails.Length];
        for(int i=0;i<state.rails.Length;i++) state.railLit[i]=state.rails[i].IsLit;
        state.collected=new bool[state.pickups.Length];
        for(int i=0;i<state.pickups.Length;i++) state.collected[i]=state.pickups[i].Collected;
        state.path.Add(new Pose(state.position,state.rotation));
        _recordingShot=state;
        _undoShots.Push(state);
    }
    void TickUndoInput()
    {
        bool held=ToyBoxGameplayHud.PanelUndo || ArcadeInputAdapter.GetButton(ArcadeInputAdapter.Button.Green) || Input.GetKey(KeyCode.U);
        bool pressed=held&&!_undoWasHeld;
        _undoWasHeld=held;
        if(pressed) UndoLastShot();
    }
    public void UndoLastShot()
    {
        if(!CanUndoShot) return;
        if(bestShotReplay!=null)bestShotReplay.CancelRecording();
        var state=_undoShots.Pop();
        _recordingShot=null;
        state.path.Add(new Pose(puck.transform.position,puck.transform.rotation));
        _rewindRoutine=StartCoroutine(RewindShot(state));
    }
    IEnumerator RewindShot(ShotUndo state)
    {
        if(heartRewardPresentation!=null) heartRewardPresentation.Clear();
        _rewindingShot=true;
        _undoKinematic=puckRigidbody.isKinematic;_undoCollisions=puckRigidbody.detectCollisions;
        puckController.ResetInputState();
        puckRigidbody.linearVelocity=Vector3.zero;
        puckRigidbody.angularVelocity=Vector3.zero;
        puckRigidbody.isKinematic=true;puckRigidbody.detectCollisions=false;
        ShowBanner("REWINDING SHOT");
        float duration=Mathf.Clamp((state.path.Count-1)*Time.fixedDeltaTime*.65f,.65f,4f);
        float elapsed=0;
        while(elapsed<duration)
        {
            elapsed+=Time.deltaTime;
            float fraction=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/duration));
            float index=(state.path.Count-1)*(1-fraction);
            int a=Mathf.FloorToInt(index), b=Mathf.Min(a+1,state.path.Count-1);
            // A portal is a discontinuity in the recorded path, not a flight
            // through the walls between its entrance and exit.
            bool portalJump=Vector3.Distance(state.path[a].position,state.path[b].position)>1.5f;
            Vector3 position=portalJump
                ? (index-a<.5f?state.path[a].position:state.path[b].position)
                : Vector3.Lerp(state.path[a].position,state.path[b].position,index-a);
            Quaternion rotation=Quaternion.Slerp(state.path[a].rotation,state.path[b].rotation,index-a);
            puckRigidbody.position=position;puckRigidbody.rotation=rotation;
            puck.transform.SetPositionAndRotation(position,rotation);
            yield return null;
        }
        puckRigidbody.position=state.position;
        puckRigidbody.rotation=state.rotation;
        puck.transform.SetPositionAndRotation(state.position,state.rotation);
        _shotCount=state.shots; _lives=state.lives; _currentMaxLives=state.maxLives; _bonusScoreThisLevel=state.bonus; _litRailCount=state.lit;
        _puckWasStopped=true; _shotInProgress=false; _comboChain=0; _comboShownMult=0;
        _lastMagnetAssistTime=-999f;
        for(int i=0;i<state.rails.Length;i++) if(state.rails[i]!=null) {state.rails[i].Reset(); if(state.railLit[i]) state.rails[i].LightUp();}
        for(int i=0;i<state.pickups.Length;i++) if(state.pickups[i]!=null) state.pickups[i].RestoreCollected(state.collected[i]);
        Physics.SyncTransforms();
        UpdateLivesDisplay();
        if(shotCounter!=null) shotCounter.text=FormatStrokeHud();
        if(comboSign!=null) comboSign.Hide();
        puckRigidbody.isKinematic=_undoKinematic;puckRigidbody.detectCollisions=_undoCollisions;
        _rewindingShot=false;_rewindRoutine=null;
        ShowBanner("SHOT UNDONE");
    }
}
