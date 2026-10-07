using UnityEngine;

public sealed class TutorialPracticeContact : MonoBehaviour
{
    public TutorialPractice3D practice;
    public bool goal;
    void OnTriggerEnter(Collider other) { Touch(other.attachedRigidbody); }
    void OnTriggerStay(Collider other) { if(!goal) Touch(other.attachedRigidbody); }
    void OnCollisionEnter(Collision other) { Touch(other.rigidbody); }
    void Touch(Rigidbody body)
    {
        if(body!=practice.puck) return;
        if(goal) practice.ReachGoal(); else practice.UseMechanic();
    }
}
