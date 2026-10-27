// Temporary deterministic recording harness. Runs outside Assets with run_script.
// Real Level_30 geometry, normal arcade controls and actual PhysX goal entry.
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Newtonsoft.Json;
public static class CaptureLuxoddLevel30
{
 public static string Main(float forcedAngle = -1)
 {
  if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
  var old=GameObject.Find("Temporary Finale Capture");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
  var m=LevelManager.Instance;var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  foreach(var c in UnityEngine.Object.FindObjectsByType<BoardCoachTutorial>(FindObjectsSortMode.None))
  {typeof(BoardCoachTutorial).GetField("_closing",flags).SetValue(c,true);typeof(BoardCoachTutorial).GetMethod("Restore",flags).Invoke(c,null);UnityEngine.Object.DestroyImmediate(c.gameObject);}
  typeof(LevelManager).GetField("_obstacleIntro",flags).SetValue(m,null);
  typeof(LevelManager).GetMethod("FinishObstacleIntro",flags).Invoke(m,null);
  m.luxoddBridge=null;m.LoadLevel(29);Time.timeScale=1;Application.runInBackground=true;
  BoardMechanicClock.EndPlayback();BoardMechanicClock.SetPaused(false);BoardMechanicClock.BeginPlayback();
  typeof(BoardMechanicClock).GetField("playbackStart",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,0f);
  BoardMechanicClock.SetPlaybackTime(6f);
  var p=m.puckController;var candidates=new List<float>();
  for(float deg=75;deg<=115;deg+=.25f){float a=deg*Mathf.Deg2Rad;p.GetTutorialPath(new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),1);if(p.TutorialPathEndsInGoal)candidates.Add(deg);}
  BoardMechanicClock.SetPlaybackTime(0);
  if(candidates.Count==0 && forcedAngle<0)throw new Exception("No valid shot predicted; no fake completion recorded.");
  float chosen=forcedAngle>=0?forcedAngle:candidates[candidates.Count/2];
  var rec=new GameObject("Temporary Finale Capture").AddComponent<FinaleCaptureRecorder>();rec.angle=chosen;
  EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
  EditorApplication.isPaused=false;EditorApplication.QueuePlayerLoopUpdate();
  return "Recording actual level 30; chosen full-power angle="+chosen+"; valid candidates="+candidates.Count;
 }
}
[DefaultExecutionOrder(-50)]
public sealed class FinaleCaptureRecorder:MonoBehaviour
{
 public float angle;
 const string Folder="output/buca-luxodd-attract/revision-4/level30";
 static readonly PropertyInfo Stick=typeof(ToyBoxGameplayHud).GetProperty("PanelStick"),Shoot=typeof(ToyBoxGameplayHud).GetProperty("PanelShoot"),Active=typeof(ToyBoxGameplayHud).GetProperty("PanelInputActive");
 static readonly FieldInfo Transition=typeof(LevelManager).GetField("_isTransitioning",BindingFlags.Instance|BindingFlags.NonPublic);
 RenderTexture target;Texture2D pixels;Camera camera;int frame,oldRate;float oldAspect;bool finished;int win=-1;
 IEnumerator Start()
 {
  Directory.CreateDirectory(Folder+"/frames");oldRate=Time.captureFramerate;Time.captureFramerate=30;
  camera=Camera.main;oldAspect=camera.aspect;camera.aspect=16f/9;
  target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);target.Create();pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
  while(frame<360)
  {
   yield return new WaitForEndOfFrame();
   var m=LevelManager.Instance;if(win<0 && (bool)Transition.GetValue(m))win=frame;
   Camera current=camera;if(m.worldStationJourney!=null && m.worldStationJourney.IsShowing)current=m.worldStationJourney.viewCamera;
   current.aspect=16f/9;RenderPipeline.SubmitRenderRequest(current,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
   var prev=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();RenderTexture.active=prev;
   File.WriteAllBytes(Folder+"/frames/"+frame.ToString("D4")+".jpg",pixels.EncodeToJPG(96));
   if(frame==180 || frame==181 || frame==210)File.AppendAllText(Folder+"/flight.txt",frame+" pos="+m.puck.transform.position+" vel="+m.puckRigidbody.linearVelocity+" clock="+BoardMechanicClock.Time+"\n");
   if(frame%30==0)File.WriteAllText(Folder+"/progress.txt",frame+" / 360; winFrame="+win+"; puck="+m.puck.transform.position);
   frame++;
  }
  File.WriteAllText(Folder+"/capture.json",JsonConvert.SerializeObject(new{level=30,angle=angle,frames=frame,win_frame=win,release_frame=180,source="Actual Level_30, normal joystick and shoot input; unmodified physics"},Formatting.Indented));
  finished=true;Cleanup();EditorApplication.isPaused=true;
 }
 void Update()
 {
  if(finished)return;float t=frame/30f;BoardMechanicClock.SetPlaybackTime(t);
  float a=(angle+Mathf.Lerp(6,0,Mathf.Clamp01((t-.5f)/1.2f)))*Mathf.Deg2Rad;Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
  Vector3 forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized,right=Vector3.ProjectOnPlane(camera.transform.right,Vector3.up).normalized;
  Vector2 stick=t>=.5f && t<6f?new Vector2(Vector3.Dot(dir,right),Vector3.Dot(dir,forward))*.30f:Vector2.zero;
  Active.SetValue(null,true);Stick.SetValue(null,stick);Shoot.SetValue(null,t>=3.6f && t<6f);
 }
 void OnDestroy(){Cleanup();}
 void Cleanup(){Time.captureFramerate=oldRate;BoardMechanicClock.EndPlayback();Active.SetValue(null,false);Stick.SetValue(null,Vector2.zero);Shoot.SetValue(null,false);if(camera!=null)camera.aspect=oldAspect;if(target!=null){target.Release();Destroy(target);target=null;}if(pixels!=null){Destroy(pixels);pixels=null;}}
}
