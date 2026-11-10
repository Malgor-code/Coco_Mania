using System;
using System.Collections.Generic;
using UnityEngine;
#if STEAMWORKS_NET
using Steamworks;
#endif

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    public const string ACH_TREE = "ACH_ARBOL_COMPLETO";
    public const string ACH_MACHETES = "ACH_MACHETES_COMPLETOS";
    public const string ACH_LEGACY = "ACH_LEGADO_COMPLETO";
    public const string ACH_COCONUTS = "ACH_TODOS_LOS_COCOS";

    public event Action<string> OnAchievementUnlocked;

    const string KeyUnlocked = "ach_unlocked";
    readonly HashSet<string> unlocked = new HashSet<string>();
    bool steamSynced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        new GameObject("AchievementManager").AddComponent<AchievementManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        string csv = PlayerPrefs.GetString(KeyUnlocked, "");
        foreach (var s in csv.Split(',')) if (s.Length > 0) unlocked.Add(s);
    }

#if STEAMWORKS_NET
    void Update()
    {
        // Cuando Steam este listo, sube los logros que ya estaban desbloqueados localmente
        if (!steamSynced && SteamManager.Initialized)
        {
            steamSynced = true;
            foreach (var id in unlocked) SteamSet(id);
            SteamUserStats.StoreStats();
        }
    }

    void SteamSet(string id)
    {
        if (SteamUserStats.GetAchievement(id, out bool already) && already) return;
        SteamUserStats.SetAchievement(id);
    }
#endif

    public bool IsUnlocked(string id) => unlocked.Contains(id);

    public void Unlock(string id)
    {
        if (!unlocked.Add(id)) return;
        PlayerPrefs.SetString(KeyUnlocked, string.Join(",", unlocked));
        PlayerPrefs.Save();
        Debug.Log("[Logro] Desbloqueado: " + id);
#if STEAMWORKS_NET
        if (SteamManager.Initialized) { SteamSet(id); SteamUserStats.StoreStats(); }
#endif
        OnAchievementUnlocked?.Invoke(id);
    }
}