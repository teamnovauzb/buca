using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="BUCA/Animal Train Collection")]
public sealed class AnimalTrainCollection : ScriptableObject
{
    [Serializable] public sealed class Friend
    {
        public string id;
        public int rewardLevel;
        public RabbitGiftReward celebration;
        public GameObject passenger;
    }
    public Friend[] friends = Array.Empty<Friend>();
    // Passengers belong to this journey; saved animal rewards and level unlocks are retained.
    static readonly HashSet<string> journeyFriends = new HashSet<string>();
    static readonly HashSet<string> waitingToBoard = new HashSet<string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void BeginJourney() { journeyFriends.Clear(); waitingToBoard.Clear(); }
    public static bool IsRiding(string id) => journeyFriends.Contains(id);
    public static bool IsWaitingToBoard(string id) => waitingToBoard.Contains(id);
    public static void FinishBoarding(string id) => waitingToBoard.Remove(id);
    public void PrepareBoarding(int completedLevel)
    {
        foreach (var friend in friends)
            if (friend.rewardLevel == completedLevel && journeyFriends.Add(friend.id))
                waitingToBoard.Add(friend.id);
    }
    public static AnimalTrainCollection Load() => Resources.Load<AnimalTrainCollection>("AnimalTrainCollection");
    public static bool Has(string id) => PlayerPrefs.GetInt("BucaAnimal_"+id,0)==1;
    public static bool Collect(string id)
    {
        if(string.IsNullOrWhiteSpace(id)||Has(id))return false;
        PlayerPrefs.SetInt("BucaAnimal_"+id,1);PlayerPrefs.Save();return true;
    }
    public IEnumerator RewardLevel(int level)
    {
        foreach(var friend in friends)
        {
            if(friend.rewardLevel!=level)continue;
            if(!Has(friend.id) && friend.celebration!=null)
            {
                var reward=Instantiate(friend.celebration);
                reward.rewardLevel=level;
                yield return reward.Play();
                Destroy(reward.gameObject);
            }
            Collect(friend.id);
            PrepareBoarding(level);
        }
    }
}
