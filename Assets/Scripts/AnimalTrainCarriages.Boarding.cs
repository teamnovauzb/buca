using UnityEngine;

public sealed partial class AnimalTrainCarriages
{
    const float BoardingSeconds=2.4f;
    int boardingIndex=-1;
    string boardingFriend;
    float boardingTime;
    AnimalSurpriseMarkers reminders;
    public bool Boarding => boardingIndex>=0;

    public void ResetPassengers()
    {
        Initialize();
        aboard.Clear();boardingIndex=-1;boardingFriend=null;boardingTime=0;
        foreach(var passenger in passengerObjects)if(passenger!=null)passenger.SetActive(false);
        RefreshReminders();
    }

    public void BeginPendingBoarding()
    {
        Initialize();
        if(Boarding || collection==null)return;
        foreach(var friend in collection.friends)
        {
            if(!AnimalTrainCollection.IsWaitingToBoard(friend.id))continue;
            Add(friend);
            boardingFriend=friend.id;boardingIndex=carIndices[friend.id];boardingTime=0;
            RefreshReminders();
            if(portraits[boardingIndex]!=null)
                portraits[boardingIndex].GetComponent<SpriteRenderer>().color=Color.clear;
            return;
        }
    }

    void RefreshReminders()
    {
        if(reminders==null && map.illustratedMenu!=null && map.illustratedMenu.artwork!=null)
            reminders=map.illustratedMenu.artwork.GetComponentInChildren<AnimalSurpriseMarkers>(true);
        if(reminders!=null)reminders.Refresh();
    }

    void TickBoarding(float dt)
    {
        if(!Boarding)return;
        boardingTime+=Mathf.Max(0,dt);
        var portrait=portraits[boardingIndex];
        if(portrait!=null && view!=null)
        {
            var camera=view.trainCamera;
            var seated=portrait.transform.position;
            float unit=Mathf.Abs(cars[boardingIndex].lossyScale.x);
            Vector3 approach=(-camera.transform.right*.9f-camera.transform.up*1.45f)*unit;
            if(reminders!=null)
            {
                int source=System.Array.IndexOf(reminders.friendIds,boardingFriend);
                if(source>=0 && source<reminders.markers.Length && reminders.markers[source]!=null)
                {
                    var marker=reminders.markers[source];
                    var image=map.illustratedMenu.locomotive;
                    var floor=cars[boardingIndex].TransformPoint(new Vector3(0,.98f,0));
                    var point=image.InverseTransformPoint(marker.TransformPoint(new Vector3(0,-marker.rect.height*.4f,0)));
                    var uv=new Vector3(point.x/image.rect.width+.5f,point.y/image.rect.height+.5f,camera.WorldToViewportPoint(floor).z);
                    approach=camera.ViewportToWorldPoint(uv)-floor;
                }
            }
            // A short anticipation, a curved jump into the box, then a gentle landing.
            float jump=Mathf.Clamp01((boardingTime-.4f)/1.25f);
            float ease=jump*jump*(3-2*jump);
            float settle=boardingTime>1.65f?Mathf.Sin((boardingTime-1.65f)*18)*Mathf.Exp(-(boardingTime-1.65f)*13)*.14f*unit:0;
            portrait.transform.position=seated+approach*(1-ease)+camera.transform.up*(Mathf.Sin(jump*Mathf.PI)*2.45f*unit+settle);
            portrait.GetComponent<SpriteRenderer>().color=new Color(1,1,1,Mathf.SmoothStep(0,1,boardingTime/.18f));
        }
        if(boardingTime<BoardingSeconds)return;
        AnimalTrainCollection.FinishBoarding(boardingFriend);
        if(portrait!=null){portrait.GetComponent<SpriteRenderer>().color=Color.white;portrait.FaceCamera();}
        boardingIndex=-1;boardingFriend=null;
        RefreshReminders();
        BeginPendingBoarding();
    }
}
