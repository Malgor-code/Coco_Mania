using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RewardDatabase", menuName = "RewardMachine/Reward Database")]
public class RewardDatabase : ScriptableObject
{
    public List<RewardData> allRewards = new List<RewardData>();

    public List<RewardData> GetByRarity(RewardRarity rarity)
    {
        List<RewardData> result = new List<RewardData>();
        foreach (var r in allRewards)
        {
            if (r != null && r.rarity == rarity) result.Add(r);
        }
        return result;
    }

    public RewardData GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var r in allRewards)
        {
            if (r != null && r.id == id) return r;
        }
        return null;
    }
}