#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using Object=UnityEngine.Object;

// Film-only, deterministic collection sequence. The live collection and save are never edited.
public static partial class BucaFeatureFilmCapture
{
 static float PickupDuration=>stationPickups?28f:17f;
 static float PickupPause=>stationPickups?2.4f:1.6f;
 static float PickupLanding=>stationPickups?1.65f:1.1f;
 static bool pickupJourney,stationPickups;
 static AnimalTrainCollection emptyCollection;
 static AnimalTrainCollection.Friend[] pickupFriends;
 static PremiumPassengerPortrait[] pickupPortraits;
 static List<Transform> pickupCars;
 static RectTransform[] pickupSources;
 static int[] savedAnimals;
 static readonly float[] ShortPickupSampleTimes={0,1.7f,2.05f,2.55f,3.2f,4.7f,5.05f,5.55f,6.2f,7.7f,8.05f,8.55f,9.2f,10.7f,11.05f,11.55f,12.2f,13.7f,14.05f,14.55f,15.2f,16.95f};
 static readonly float[] LongPickupSampleTimes={0,2.9f,3.2f,4f,5.1f,7.7f,8f,8.8f,9.9f,12.5f,12.8f,13.6f,14.7f,17.3f,17.6f,18.4f,19.5f,22.1f,22.4f,23.2f,24.3f,27.95f};
 static float[] PickupSampleTimes=>stationPickups?LongPickupSampleTimes:ShortPickupSampleTimes;
 static float Smooth(float x){x=Mathf.Clamp01(x);return x*x*(3-2*x);}
 static float PickupArrival(int i)=>stationPickups?3f+4.8f*i:1.8f+3*i;
 static void SetupPickups()
 {
  var catalog=AnimalTrainCollection.Load();pickupFriends=catalog.friends;
  if(pickupFriends.Length!=5)throw new Exception("The film requires the five selected World 1 friends");
  emptyCollection=ScriptableObject.CreateInstance<AnimalTrainCollection>();
  typeof(AnimalTrainCarriages).GetField("collection",Flags).SetValue(cars,emptyCollection);
  savedAnimals=new int[5];pickupPortraits=new PremiumPassengerPortrait[5];
  pickupSources=root.GetComponentInChildren<AnimalSurpriseMarkers>(true).markers;
  pickupCars=(List<Transform>)typeof(AnimalTrainCarriages).GetField("cars",Flags).GetValue(cars);
  for(int i=0;i<5;i++)
  {
   if(pickupFriends[i].rewardLevel!=(i+1)*5)throw new Exception("Animal milestone order changed");
   savedAnimals[i]=PlayerPrefs.GetInt("BucaAnimal_"+pickupFriends[i].id,-1);
   typeof(AnimalTrainCarriages).GetMethod("Add",Flags).Invoke(cars,new object[]{pickupFriends[i]});
   pickupPortraits[i]=pickupCars[i].GetComponentInChildren<PremiumPassengerPortrait>(true);
   pickupPortraits[i].enabled=false;pickupPortraits[i].GetComponent<SpriteRenderer>().enabled=false;
  }
  foreach(var badge in root.GetComponentsInChildren<TrainLevelBadge>(true))badge.gameObject.SetActive(false);
  string metadata="{\"duration\":"+PickupDuration.ToString(CultureInfo.InvariantCulture)+",\"pause\":"+PickupPause.ToString(CultureInfo.InvariantCulture)+",\"fps\":30,\"pickups\":[";
  for(int i=0;i<5;i++)metadata+=(i==0?"":",")+"{\"animal\":\""+pickupFriends[i].id+"\",\"level\":"+pickupFriends[i].rewardLevel+",\"arrive\":"+PickupArrival(i).ToString(CultureInfo.InvariantCulture)+",\"land\":"+(PickupArrival(i)+PickupLanding).ToString(CultureInfo.InvariantCulture)+"}";
  File.WriteAllText(Folder+"/pickup-events.json",metadata+"]}");
 }
 static float PickupRoute(float t)
 {
  int from=0;float depart=stationPickups?.6f:.4f;
  for(int i=0;i<5;i++)
  {
   int to=pickupFriends[i].rewardLevel-1;float arrive=PickupArrival(i);
   if(t<arrive)return Mathf.Lerp(map.stops[from].distance,map.stops[to].distance,Smooth((t-depart)/(arrive-depart)));
   if(t<arrive+PickupPause)return map.stops[to].distance;
   from=to;depart=arrive+PickupPause;
  }
  return Mathf.Lerp(map.stops[from].distance,map.stops[29].distance,Smooth((t-depart)/(PickupDuration-.3f-depart)));
 }
 static void SamplePickups(float t)
 {
  for(int i=0;i<5;i++)
  {
   var portrait=pickupPortraits[i];var sprite=portrait.GetComponent<SpriteRenderer>();
   float age=t-PickupArrival(i);sprite.enabled=age>=(stationPickups?0:-.05f);
   if(!sprite.enabled)continue;
   portrait.FaceCamera();var seated=portrait.transform.position;
   float anticipation=stationPickups?.4f:.25f;
   float jump=Mathf.Clamp01((age-anticipation)/(PickupLanding-anticipation));float p=Smooth(jump);
   var cam=portrait.renderingCamera;float unit=portrait.transform.parent.lossyScale.x;
   // The clearer cut starts at the actual numbered reminder, not beside the destination wagon.
   var approach=(-cam.transform.right*.9f-cam.transform.up*1.45f)*unit;
   if(stationPickups)
   {
    var image=map.illustratedMenu.locomotive;
    var floor=portrait.transform.parent.TransformPoint(new Vector3(0,.98f,0));
    var local=image.InverseTransformPoint(pickupSources[i].TransformPoint(new Vector3(0,-pickupSources[i].rect.height*.4f,0)));
    var uv=new Vector3(local.x/image.rect.width+.5f,local.y/image.rect.height+.5f,cam.WorldToViewportPoint(floor).z);
    approach=cam.ViewportToWorldPoint(uv)-floor;
   }
   float arc=Mathf.Sin(jump*Mathf.PI)*2.45f*unit;
   float settle=age>PickupLanding?Mathf.Sin((age-PickupLanding)*18)*Mathf.Exp(-(age-PickupLanding)*13)*.14f*unit:0;
   portrait.transform.position=seated+approach*(1-p)+cam.transform.up*(arc+settle);
   sprite.color=new Color(1,1,1,Smooth((age+(stationPickups?0:.05f))/.18f));
  }
  // The static reminder disappears only when that friend boards; other reminders stay put.
  foreach(var reminders in root.GetComponentsInChildren<AnimalSurpriseMarkers>(true))
   for(int i=0;i<reminders.markers.Length;i++)reminders.markers[i].gameObject.SetActive(t<PickupArrival(i)+(stationPickups?0:.25f));
 }
 static Vector3 PickupFocus()
 {
  var rig=map.GetComponent<SuitcaseLocomotiveView>();var image=map.illustratedMenu.locomotive;
  var middle=(map.train.position+pickupCars[4].position)*.5f;
  var uv=rig.trainCamera.WorldToViewportPoint(middle);
  return camera.WorldToScreenPoint(image.TransformPoint(new Vector3((uv.x-.5f)*image.rect.width,(uv.y-.5f)*image.rect.height,0)));
 }
 static void RecordPickup(float t)
 {
  int boarded=0,visible=0;foreach(var portrait in pickupPortraits)if(portrait.GetComponent<SpriteRenderer>().enabled)visible++;
  for(int i=0;i<5;i++)if(t>=PickupArrival(i)+PickupLanding)boarded++;
  File.AppendAllText(Folder+(preview?"/pickup-samples.csv":"/pickup-state.csv"),frame+","+t.ToString(CultureInfo.InvariantCulture)+","+(map.Parked+1)+","+visible+","+boarded+"\n");
 }
 static void ValidatePickupPreferences()
 {
  for(int i=0;i<5;i++)if(PlayerPrefs.GetInt("BucaAnimal_"+pickupFriends[i].id,-1)!=savedAnimals[i])throw new Exception("Animal preference changed during capture");
 }
 static void CleanupPickups()
 {
  if(emptyCollection!=null)Object.DestroyImmediate(emptyCollection);
  emptyCollection=null;pickupFriends=null;pickupPortraits=null;pickupCars=null;pickupSources=null;savedAnimals=null;
 }
}
#endif
