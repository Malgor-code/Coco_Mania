using UnityEngine;

public partial class GameManager
{
    void CheckAchievements()
    {
        var am = AchievementManager.Instance;
        if (am == null) return;

        if (skillTree != null && skillTree.Count > 0)
        {
            bool all = true;
            foreach (var n in skillTree)
                if (!unlockedSkillIds.Contains(n.id)) { all = false; break; }
            if (all) am.Unlock(AchievementManager.ACH_TREE);
        }

        if (macheteOptions != null && macheteOptions.Count > 0 &&
            equippedMacheteIndex >= macheteOptions.Count - 1)
            am.Unlock(AchievementManager.ACH_MACHETES);

        if (legacyShop != null && legacyShop.Count > 0)
        {
            bool all = true;
            foreach (var it in legacyShop)
                if (!purchasedLegacyItems.Contains(it.itemName)) { all = false; break; }
            if (all) am.Unlock(AchievementManager.ACH_LEGACY);
        }

        if (coconutUnlocks != null && coconutUnlocks.Count > 0)
        {
            int needed = 0;
            foreach (var c in coconutUnlocks) needed = Mathf.Max(needed, c.cycleRequired);
            if (billCycle >= needed) am.Unlock(AchievementManager.ACH_COCONUTS);
        }
    }
}