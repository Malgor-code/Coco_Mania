using UnityEngine;

public partial class GameManager
{
    public bool CanAffordAnySkill()
    {
        if (skillTree == null) return false;

        foreach (var node in skillTree)
        {
            if (unlockedSkillIds.Contains(node.id)) continue;
            if (node.cost > waterCurrentML) continue;

            bool prereqsOk = true;
            if (node.prerequisiteIds != null)
            {
                foreach (var p in node.prerequisiteIds)
                {
                    if (!unlockedSkillIds.Contains(p)) { prereqsOk = false; break; }
                }
            }
            if (prereqsOk) return true;
        }
        return false;
    }
    public bool CanAffordAnyMachete()
    {
        if (macheteOptions == null) return false;

        for (int i = 0; i < macheteOptions.Count; i++)
        {
            if (IsMacheteOwned(i)) continue;
            if (macheteOptions[i].unlockCost <= waterCurrentML) return true;
        }
        return false;
    }
    public bool CanUseGacha()
    {
        return coconutCoins >= GetGachaCost();
    }

    bool IsMacheteOwned(int index)
    {
        return index == 0 || index == equippedMacheteIndex;
    }

    int GetGachaCost()
    {
        return 1;
    }
}