using System;
using System.Collections.Generic;

[Serializable]
public class BagEntry
{
    public string rewardId;
    public int count;
}

[Serializable]
public class EquippedSlot
{
    public string rewardId;
    public RewardEffect effect;
    public float value;
    public bool isTemporary;
    public int daysRemaining;

    public bool IsEmpty => string.IsNullOrEmpty(rewardId);

    public void Clear()
    {
        rewardId = null;
        effect = default;
        value = 0f;
        isTemporary = false;
        daysRemaining = 0;
    }
}

[Serializable]
public class CurrentRunData
{
    public int pullsSinceEpic = 0;
    public int pullsSinceLegendary = 0;
    public List<BagEntry> bag = new List<BagEntry>();
    public List<EquippedSlot> slots = new List<EquippedSlot>();
}

[Serializable]
public class PersistentData
{
    public HashSet<string> unlockedRewardIds = new HashSet<string>();
}