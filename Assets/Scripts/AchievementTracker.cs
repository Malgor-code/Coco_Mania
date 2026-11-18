using UnityEngine;

public static class AchievementTracker
{
    public static void Check()
    {
        var am = AchievementManager.Instance;
        if (am == null) return;

        var nodes = SkillTreeManager.Instance.Nodes;
        if (nodes != null && nodes.Count > 0)
        {
            bool all = true;
            foreach (var n in nodes)
                if (!SkillTreeManager.Instance.IsUnlocked(n.id)) { all = false; break; }
            if (all) am.Unlock(AchievementManager.ACH_TREE);
        }

        var machetes = MacheteShopManager.Instance.Options;
        if (machetes != null && machetes.Count > 0 &&
            MacheteShopManager.Instance.EquippedIndex >= machetes.Count - 1)
            am.Unlock(AchievementManager.ACH_MACHETES);

        var legacy = LegacyManager.Instance;
        if (legacy.Items != null && legacy.Items.Count > 0)
        {
            bool all = true;
            foreach (var it in legacy.Items)
                if (!legacy.IsPurchased(it.itemName)) { all = false; break; }
            if (all) am.Unlock(AchievementManager.ACH_LEGACY);
        }

        var spawner = CoconutSpawner.Instance;
        if (spawner != null && spawner.coconutTypes != null && spawner.coconutTypes.Count > 0)
        {
            int needed = 0;
            foreach (var t in spawner.coconutTypes)
                if (t != null) needed = Mathf.Max(needed, t.unlockCycle);
            if (DebtManager.Instance.BillCycle >= needed) am.Unlock(AchievementManager.ACH_COCONUTS);
        }
    }
}