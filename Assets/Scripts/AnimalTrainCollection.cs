using System;
using System.Collections;
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
            if(friend.rewardLevel!=level||Has(friend.id)||friend.celebration==null)continue;
            var reward=Instantiate(friend.celebration);
            reward.rewardLevel=level;
            yield return reward.Play();
            Collect(friend.id);
            Destroy(reward.gameObject);
        }
    }
}
