using System.IO;
using UnityEngine;
using SS = StatSource;
using ST = StatType;

public static class SaveManager
{
    const string FileName = "savegame.json";
    static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
    static string BackupPath => FilePath + ".bak";
    static string TempPath => FilePath + ".tmp";
    public static bool HasSaveFile => File.Exists(FilePath) || File.Exists(BackupPath);

    public static void SaveNow()
    {
        try
        {
            GameSaveData d = new GameSaveData();
            CaptureProfile(d);
            RunManager.Instance.CaptureRun(d);
            Write(d);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveManager] No se pudo guardar: " + e);
        }
    }

    static void CaptureProfile(GameSaveData d)
    {
        LegacyManager legacy = LegacyManager.Instance;
        d.legacyPoints = legacy.Points;
        d.totalLegacyPointsEarned = legacy.TotalEarned;
        d.coins = ResourcesManager.Instance.Coins;
        d.bestRunKills = CombatManager.Instance.bestRunKills;
        d.bestCycleReached = DebtManager.Instance.BestCycleReached;
        d.purchasedLegacyItems = new System.Collections.Generic.List<string>(legacy.Purchased);
    }

    static void Write(GameSaveData d)
    {
        string json = JsonUtility.ToJson(d, true);
        File.WriteAllText(TempPath, json);
        if (File.Exists(FilePath)) File.Replace(TempPath, FilePath, BackupPath);
        else File.Move(TempPath, FilePath);
    }
    public static void LoadAll()
    {
        GameSaveData d = Read();
        if (d == null) return;

        RestoreProfile(d);
        RunManager.Instance.RestoreRun(d);
    }

    static GameSaveData Read()
    {
        GameSaveData d = TryRead(FilePath) ?? TryRead(BackupPath);
        if (d != null) return d;
        SaveData old = SaveSystem.Load();
        if (old == null) return null;
        return new GameSaveData
        {
            legacyPoints = old.legacyPoints,
            totalLegacyPointsEarned = old.totalLegacyPointsEarned,
            coins = old.coconutCoins,
            bestRunKills = old.bestRunKills,
            bestCycleReached = Mathf.Max(1, old.bestCycleReached),
            purchasedLegacyItems = old.purchasedLegacyItems ?? new System.Collections.Generic.List<string>()
        };
    }

    static GameSaveData TryRead(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[SaveManager] Archivo ilegible (" + path + "): " + e.Message);
            return null;
        }
    }

    static void RestoreProfile(GameSaveData d)
    {
        LegacyManager legacy = LegacyManager.Instance;
        StatsManager stats = StatsManager.Instance;

        legacy.Points = d.legacyPoints;
        legacy.TotalEarned = d.totalLegacyPointsEarned;
        ResourcesManager.Instance.SetCoins(d.coins);
        CombatManager.Instance.bestRunKills = d.bestRunKills;
        DebtManager.Instance.BestCycleReached = Mathf.Max(1, d.bestCycleReached);

        stats.ClearSource(SS.Legacy);
        MacheteShopManager.Instance.EquippedIndex = 0;
        stats.coinDropChance = 0.005f;
        if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts = 4;

        legacy.Purchased.Clear();
        if (d.purchasedLegacyItems != null)
        {
            foreach (string name in d.purchasedLegacyItems)
            {
                LegacyItem item = legacy.GetItem(name);
                if (item == null) continue;
                legacy.Purchased.Add(name);
                legacy.ApplyEffect(item);
            }
        }
        legacy.RecalcMultiplier();
        MacheteShopManager.Instance.RecalculateStats();
    }

    public static void Delete()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        if (File.Exists(BackupPath)) File.Delete(BackupPath);
        if (File.Exists(TempPath)) File.Delete(TempPath);
        SaveSystem.Delete();
    }
}