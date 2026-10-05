using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int version = 1;
    public int legacyPoints;
    public int totalLegacyPointsEarned;
    public int coconutCoins;
    public int bestRunKills;
    public int bestCycleReached = 1;
    public List<string> purchasedLegacyItems = new List<string>();
}

public static class SaveSystem
{
    static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");
    static string BackupPath => FilePath + ".bak";

    public static void Save(SaveData data)
    {
        try
        {
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
            if (File.Exists(FilePath)) File.Copy(FilePath, BackupPath, true);
            File.Copy(tmp, FilePath, true);
            File.Delete(tmp);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveSystem] No se pudo guardar: " + e.Message);
        }
    }

    public static SaveData Load()
    {
        SaveData d = TryRead(FilePath);
        if (d == null) d = TryRead(BackupPath);
        return d;
    }

    static SaveData TryRead(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveSystem] No se pudo leer " + path + ": " + e.Message);
            return null;
        }
    }

    public static void Delete()
    {
        try
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
        }
        catch (Exception e) { Debug.LogWarning("[SaveSystem] " + e.Message); }
    }
}