using System;
using UnityEditor;
using UnityEngine;

public static class ToyBoxWinValidation
{
    public static void Check(LevelManager manager)
    {
        var award=manager.premiumWinCelebration;
        if(award==null || award.sparkles.Length!=6 || Mathf.Abs(award.Duration-1.6f)>.01f) throw new Exception("Small saved hole sparkle missing.");
        foreach(var mesh in award.GetComponentsInChildren<MeshFilter>(true))
            if(!AssetDatabase.Contains(mesh.sharedMesh)) throw new Exception("Unsaved win geometry.");
        if(award.GetComponentsInChildren<MeshFilter>(true).Length!=7) throw new Exception("Large award geometry remains.");
        var camera=Camera.main;
        Vector3 cameraPosition=camera.transform.position; Quaternion cameraRotation=camera.transform.rotation; float fov=camera.fieldOfView;
        var hole=manager.CurrentLevelRoot.transform.Find("Hole");
        var anchor=new Vector3(hole.position.x,.1f,hole.position.z);
        var hud=UnityEngine.Object.FindAnyObjectByType<ToyBoxGameplayHud>();
        bool hudActive=hud!=null && hud.gameObject.activeSelf;
        if(hud!=null) hud.gameObject.SetActive(false);
        try
        {
            award.Play(anchor);
            for(int step=0;step<=16;step++)
            {
                award.Sample(step*.1f);
                if(award.presentation.position!=anchor) throw new Exception("Sparkles drift away from the hole.");
                foreach(var sparkle in award.sparkles)
                    if(sparkle.localPosition.magnitude>2.2f) throw new Exception("Sparkles cover too much of the board.");
            }
            award.Sample(.60f);
            ToyBoxMenuValidation.RenderCamera(camera,"Hole-Sparkle-Win",1600,1000);
            if(camera.transform.position!=cameraPosition || camera.transform.rotation!=cameraRotation || camera.fieldOfView!=fov) throw new Exception("Celebration moved the camera.");
            award.Sample(award.Duration);
            if(award.goldRing.localScale.sqrMagnitude>.001f) throw new Exception("Ring did not finish shrinking.");
            foreach(var sparkle in award.sparkles) if(sparkle.localScale.sqrMagnitude>.001f) throw new Exception("Sparkle did not finish shrinking.");
            award.Hide();
            if(award.IsPlaying || award.presentation.gameObject.activeSelf) throw new Exception("Award did not clean up.");
            var other=anchor+new Vector3(2,0,-3); award.Play(other);
            if(award.presentation.position!=other) throw new Exception("Next hole anchor was not applied.");
        }
        finally { award.Hide(); if(hud!=null) hud.gameObject.SetActive(hudActive); }
        Debug.Log("HOLE_SPARKLE_VALIDATION_PASSED: seven saved meshes, hole-local bounds, 1.6-second duration, fixed camera, full exit and replay at a new hole.");
    }
}
