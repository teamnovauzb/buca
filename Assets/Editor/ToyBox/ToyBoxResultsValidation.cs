using System;
using UnityEngine;
using UnityEditor;

public static class ToyBoxResultsValidation
{
    public static void Check(LevelManager manager)
    {
        var panel=manager.levelCompletePanel;
        var view=panel.premiumResults;
        if(view==null) throw new Exception("3D results not assigned.");
        foreach(var mesh in view.GetComponentsInChildren<MeshFilter>(true))
            if(mesh.GetComponent<TMPro.TMP_Text>()==null && !AssetDatabase.Contains(mesh.sharedMesh)) throw new Exception("Unsaved result geometry.");
        var score=new ScoreCalculator.ScoreBreakdown {basePoints=500,timeBonus=72,railBonus=27,strokeBonus=25,total=981,stars=3,comboMultiplier=1.1f,comboName="NICE SAVE",strokesUsed=3,par=4,strokesToPar=-1};
        var originalCamera=Camera.main;
        var originalTarget=originalCamera.targetTexture; var originalProjection=originalCamera.projectionMatrix;
        bool done=false;
        panel.Show(score,()=>done=true);
        view.Tick(.4f);
        if(view.Remaining!=5 || view.leftBoard.localPosition.y>=0) throw new Exception("Win entrance/countdown staging failed.");
        view.Tick(1.4f);
        foreach(var target in new[]{view.levelCapture,view.blurScratch,view.blurredLevel})
            if(target==null || !AssetDatabase.Contains(target) || !target.IsCreated()) throw new Exception("Saved blur render target not populated.");
        if(originalCamera.targetTexture!=originalTarget || originalCamera.projectionMatrix!=originalProjection) throw new Exception("Backdrop capture changed gameplay camera state.");
        if(view.roomRoot!=null || view.levelBackdrop==null) throw new Exception("Results still use a separate room.");
        if(!view.IsShowing || originalCamera.enabled || !view.resultsCamera.isActiveAndEnabled) throw new Exception("Results camera isolation failed.");
        if(panel.golfScorecard!=null && panel.golfScorecard.gameObject.activeInHierarchy) throw new Exception("Old golf scorecard still visible.");
        if(view.totalDigits.Length!=10 || view.totalDigits[0].sharedMesh!=view.digitMeshes[9] || view.totalDigits[1].sharedMesh!=view.digitMeshes[8] || view.totalDigits[2].sharedMesh!=view.digitMeshes[1] || view.totalDigits[3].gameObject.activeSelf) throw new Exception("Saved solid score digits incorrect.");
        if(view.podiumBase==null || view.presentation.transform.Find("Leaderboard")!=null) throw new Exception("Leaderboard must be absent from level results.");
        if(view.scoreValues[4].text!="981") throw new Exception("Incorrect result total.");
        view.resultsCamera.aspect=1.6f; view.Fit();
        ToyBoxMenuValidation.RenderCamera(view.resultsCamera,"Results-ScoreOnly",1600,1000);
        view.resultsCamera.aspect=.5625f; view.Fit();
        ToyBoxMenuValidation.RenderCamera(view.resultsCamera,"Results-ScoreOnly-Portrait",900,1600);
        view.resultsCamera.aspect=1.6f; view.Fit();
        Physics.SyncTransforms();
        Vector3 point=view.resultsCamera.WorldToViewportPoint(view.nextHit.bounds.center);
        if(point.x<=0 || point.x>=1 || point.y<=0 || point.y>=1 || !view.nextHit.Raycast(view.resultsCamera.ViewportPointToRay(point),out var hit,100)) throw new Exception("Next button outside view.");
        view.ToggleScorecard();
        view.Tick(60);
        if(!view.ScorecardShowing || view.Remaining!=5 || done) throw new Exception("Scorecard did not pause auto advance.");
        var entries=(System.Collections.Generic.List<LevelManager.HoleScoreEntry>)manager.CampaignScorecard;
        entries.Add(new LevelManager.HoleScoreEntry(1,3,4));
        try
        {
            view.TurnScorecardPage(0);
            if(!view.scorecardRows[0].text.Contains("3 SHOTS - 1 UNDER PAR")) throw new Exception("Golf scorecard row is incorrect.");
            ToyBoxMenuValidation.RenderCamera(view.resultsCamera,"Results-My-Shots",1600,1000);
            view.TurnScorecardPage(-1);
            if(!view.scorecardTitle.text.Contains("25 TO 30")) throw new Exception("Scorecard paging failed.");
            view.TurnScorecardPage(1);
        }
        finally { entries.RemoveAt(entries.Count-1); }
        view.ToggleScorecard();
        if(view.ScorecardShowing || !view.nextRoot.gameObject.activeSelf) throw new Exception("Could not return from scorecard.");
        view.Tick(4.9f);
        if(done || !view.IsShowing) throw new Exception("Results advanced too soon.");
        view.Tick(.11f);
        if(!done || view.IsShowing || !originalCamera.enabled) throw new Exception("Results did not return to game.");
        done=false; panel.Show(score,()=>done=true); view.PressContinue(); view.Tick(.17f);
        if(!done || view.IsShowing) throw new Exception("Next button failed.");
        var ace=ScoreCalculator.Calculate(1,20,45,0,4,4,4);
        panel.Show(ace,()=>{}); view.Tick(1.8f);
        if(!view.holeInOneTitle.activeSelf || view.regularTitle.activeSelf || !string.IsNullOrEmpty(view.comboText.text)) throw new Exception("Hole in one reward not explained.");
        ToyBoxMenuValidation.RenderCamera(view.resultsCamera,"Results-Hole-In-One",1600,1000);
        view.Hide(false);
        Debug.Log("FEEDBACK_SCORECARD_PASSED: readable golf results, paging, countdown pause, return and distinct one-shot award.");
        Debug.Log("RESULTS_SCORE_ONLY_VALIDATION_PASSED: saved 3D score panel, no leaderboard, correct values, blurred level, portrait layout, countdown and Next.");
    }
}
