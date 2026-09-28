using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ActiveRelic
{
    public string rewardId;
    public RewardEffect effect;
    public float value;
}

[Serializable]
public class ActiveTemporaryReward
{
    public string rewardId;
    public RewardEffect effect;
    public float value;
    public int daysRemaining;
}

[Serializable]
public class PhysicalItemInstance
{
    public string rewardId;
    public int usesRemaining;
    [Tooltip("Solo relevante si el RewardData tiene TriggerMode = RealTimeInterval.")]
    public float secondsSinceLastTrigger;
}
[Serializable]
public class CurrentRunData
{
    public int pullsSinceEpic = 0;
    public int pullsSinceLegendary = 0;
    public List<ActiveRelic> relics = new List<ActiveRelic>();
    public List<ActiveTemporaryReward> temporaryRewards = new List<ActiveTemporaryReward>();
    public List<PhysicalItemInstance> physicalItems = new List<PhysicalItemInstance>();
}
[Serializable]
public class PersistentData
{
    public HashSet<string> unlockedRewardIds = new HashSet<string>();
    public int fragments = 0;
}