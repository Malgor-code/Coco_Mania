using UnityEngine;
public static class BetaLogger
{
    static float runStartTime;

    public static void StartRun() => runStartTime = Time.time;

    public static void Log(string evt)
    {
        try
        {
            CombatManager c = CombatManager.Instance;
            DebtManager d = DebtManager.Instance;
            string line = System.DateTime.Now.ToString("HH:mm:ss") + "," + evt
                + ",run_t=" + (Time.time - runStartTime).ToString("F0")
                + ",cycle=" + d.BillCycle + ",days=" + d.DaysLeft
                + ",water=" + ResourcesManager.Instance.Water.ToString("F0") + ",target=" + d.TargetWater.ToString("F0")
                + ",machete=" + MacheteShopManager.Instance.EquippedIndex + ",skills=" + SkillTreeManager.Instance.Unlocked.Count
                + ",swings_per_kill=" + (c.dayCoconutsKilled > 0 ? (c.daySwings / (float)c.dayCoconutsKilled).ToString("F1") : "0")
                + ",legacy=" + LegacyManager.Instance.TotalEarned + ",hpscale=" + LegacyManager.Instance.GetHpScale(d.BillCycle).ToString("F2")
                + ",kills=" + c.totalCoconutsKilled + "\n";
            System.IO.File.AppendAllText(System.IO.Path.Combine(Application.persistentDataPath, "beta_log.csv"), line);
        }
        catch { }
    }
}