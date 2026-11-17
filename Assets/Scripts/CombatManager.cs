using System.Collections.Generic;
using UnityEngine;
using static MacheteData;

public class CombatManager : MonoBehaviour
{
    static CombatManager instance;
    public static CombatManager Instance
    {
        get
        {
            if (instance == null) instance = ManagerBootstrap.Ensure<CombatManager>();
            return instance;
        }
    }

    [Header("Agua que suelta cada coco al morir")]
    public float baseWaterPerKill = 40f;
    public float waterVariance = 90f;

    [Header("Jackpot de agua")]
    [Tooltip("El Jackpot suelta ESTE multiplo extra del agua que dio el coco (1.5 = +150%).")]
    public float jackpotBaseMultiplier = 1.5f;

    [Header("Racha de aciertos")]
    [Tooltip("A partir de cuantos golpes seguidos conectados empieza a sumar bono.")]
    public int streakThreshold = 3;
    public float streakDamageBonusPerHit = 0.05f;
    public float streakWaterBonusPerHit = 0.05f;
    public int streakMaxStacks = 6;
    const int StreakHardCap = 12;
    const float StreakPowerCap = 0.10f;

    [Header("Otros")]
    public float executeHpThreshold = 0.5f;
    public float earlyDayDuration = 5f;

    public int CurrentHitStreak { get; set; }
    public int StreakForgivesLeft { get; set; }
    bool isApplyingSplash;

    [Header("Estadisticas (solo lectura en juego)")]
    public int totalCoconutsKilled;
    public int runCoconutsKilled;
    public int runMoneyEarned;
    public int bestRunKills;

    public int dayCoconutsKilled;
    public int dayMoneyEarned;
    public int daySwings;
    public int dayHits;
    public int dayCoinsFound;
    public int killsAtDayStart;

    [Header("Vida base de los cocos")]
    public int coconutHpMax = 20;

    [HideInInspector] public bool lastKillFoundCoin;
    [HideInInspector] public bool lastKillHadJackpot;

    public void ResetDay()
    {
        dayCoconutsKilled = 0;
        dayMoneyEarned = 0;
        daySwings = 0;
        dayHits = 0;
        dayCoinsFound = 0;
        CurrentHitStreak = 0;
        killsAtDayStart = totalCoconutsKilled;
    }

    public void ResetRun()
    {
        totalCoconutsKilled = 0;
        killsAtDayStart = 0;
        CurrentHitStreak = 0;
        StreakForgivesLeft = 0;
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    static string Loc(string key, params object[] args) =>
        LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(key, args) : key;

    static bool IsHitting => RunManager.Instance.IsHitting;

    public void RegisterSwingResult(bool hit)
    {
        daySwings++;
        if (hit)
        {
            dayHits++;
            CurrentHitStreak++;
        }
        else if (StreakForgivesLeft > 0 && CurrentHitStreak >= streakThreshold)
        {
            StreakForgivesLeft--;
        }
        else
        {
            CurrentHitStreak = 0;
        }
    }

    int CurrentStreakStacks()
    {
        if (!SkillTreeManager.Instance.StreakUnlocked) return 0;

        int stacks = CurrentHitStreak - streakThreshold + 1;
        int max = Mathf.Min(StreakHardCap, Mathf.RoundToInt(StatsManager.Instance.Get(StatType.StreakStacks)));
        return Mathf.Clamp(stacks, 0, max);
    }

    public float GetStreakDamageMultiplier() =>
        1f + CurrentStreakStacks() * Mathf.Min(StreakPowerCap, streakDamageBonusPerHit + StatsManager.Instance.Get(StatType.StreakPower));

    public float GetStreakWaterMultiplier() =>
        1f + CurrentStreakStacks() * Mathf.Min(StreakPowerCap, streakWaterBonusPerHit + StatsManager.Instance.Get(StatType.StreakPower));

    public float GetLastBreathMultiplier()
    {
        float bonus = StatsManager.Instance.Get(StatType.LastBreathWater);
        if (bonus <= 0f || !IsHitting) return 1f;
        StaminaManager st = StaminaManager.Instance;
        return st.Current <= st.Max * 0.2f ? 1f + bonus : 1f;
    }

    public float GetExecuteDamageMultiplier(float targetHpPercent)
    {
        float bonus = StatsManager.Instance.Get(StatType.ExecuteDamage);
        return (bonus > 0f && targetHpPercent < executeHpThreshold) ? 1f + bonus : 1f;
    }

    public float GetMultiHitMultiplier(int targetsHit)
    {
        StatsManager stats = StatsManager.Instance;
        float bonus = stats.Get(StatType.MultiHitDamage) + stats.Passive(MachetePassive.MultiHit);
        return (bonus > 0f && targetsHit >= 2) ? 1f + bonus : 1f;
    }

    float GetEarlyDayMultiplier()
    {
        float bonus = StatsManager.Instance.Get(StatType.EarlyDayWater);
        return (bonus > 0f && IsHitting && StaminaManager.Instance.DayElapsed < earlyDayDuration) ? 1f + bonus : 1f;
    }

    public int OnCoconutDestroyed(float lootMultiplier, out float waterGained)
    {
        StatsManager stats = StatsManager.Instance;
        ResourcesManager res = ResourcesManager.Instance;

        totalCoconutsKilled++;
        runCoconutsKilled++;
        if (runCoconutsKilled > bestRunKills) bestRunKills = runCoconutsKilled;
        dayCoconutsKilled++;

        float streakBonus = GetStreakWaterMultiplier() - 1f;
        float earlyBonus = GetEarlyDayMultiplier() - 1f;
        float waterExtraMult = Mathf.Max(0.1f, 1f + stats.Get(StatType.WaterMult)
            + stats.Passive(MachetePassive.WaterBonus) - stats.CurseWaterPenalty + streakBonus + earlyBonus);

        float legacyLootScale = LegacyManager.Instance.GetLootScale(DebtManager.Instance.BillCycle);

        waterGained = (baseWaterPerKill + Random.Range(0f, waterVariance)) * lootMultiplier * waterExtraMult
                    * stats.legacyMultiplier * GetLastBreathMultiplier() * stats.perkWaterMultiplicative
                    * legacyLootScale;

        float jackpotChance = stats.Get(StatType.JackpotChance) + stats.Passive(MachetePassive.JackpotChance);
        lastKillHadJackpot = jackpotChance > 0f && Random.value < jackpotChance;
        if (lastKillHadJackpot)
        {
            float jackpotWater = waterGained * (jackpotBaseMultiplier + stats.Get(StatType.JackpotWater));
            waterGained += jackpotWater;
            GameLog.Say("¡JACKPOT DE AGUA! +" + ResourcesManager.FormatWater(jackpotWater));
        }

        res.AddWater(waterGained);
        int gainedRounded = Mathf.RoundToInt(waterGained);
        runMoneyEarned += gainedRounded;
        dayMoneyEarned += gainedRounded;

        float recovered = StaminaManager.Instance.TryRecoverOnKill();
        if (recovered > 0f) GameLog.Say("¡Recuperaste " + recovered.ToString("0.#") + " de resistencia!");

        bool foundCoin = Random.value < stats.coinDropChance;
        lastKillFoundCoin = foundCoin;
        if (foundCoin)
        {
            res.AddCoins(1);
            dayCoinsFound++;
            GameLog.Say(Loc("log_coin_found", res.Coins));
        }
        else if (!lastKillHadJackpot)
        {
            GameLog.Say(Loc("log_money_earned", ResourcesManager.FormatWater(waterGained)));
        }

        float extraChance = stats.Get(StatType.ExtraCoconutChance);
        if (extraChance > 0f && IsHitting && Random.value < extraChance && CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.SpawnExtraCoconut();
        }
        return gainedRounded;
    }

    public int OnCoconutDestroyed(float lootMultiplier = 1f)
    {
        return OnCoconutDestroyed(lootMultiplier, out _);
    }

    public void ApplyChainSplash(Vector3 position, int killingDamage, GameObject source)
    {
        StatsManager stats = StatsManager.Instance;
        float pct = Mathf.Min(0.5f, stats.Get(StatType.ChainSplash) + stats.Passive(MachetePassive.ChainSplash));
        if (pct <= 0f || isApplyingSplash) return;

        isApplyingSplash = true;
        try
        {
            int splashDamage = Mathf.Max(1, Mathf.RoundToInt(killingDamage * pct));
            float radius = MacheteController.Instance != null ? MacheteController.Instance.hitRadius : 1.5f;
            var already = new HashSet<CoconutTarget>();
            foreach (var col in Physics.OverlapSphere(position, radius))
            {
                var target = col.GetComponentInParent<CoconutTarget>();
                if (target == null || target.gameObject == source || !already.Add(target)) continue;
                target.TakeDamage(splashDamage);
            }
        }
        finally
        {
            isApplyingSplash = false;
        }
    }
}