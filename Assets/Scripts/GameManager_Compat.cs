using System.Collections.Generic;
using UnityEngine;
using static MacheteData;
using SS = StatSource;
using ST = StatType;

public partial class GameManager
{
    static StatsManager Stats => StatsManager.Instance;
    static ResourcesManager Res => ResourcesManager.Instance;
    static StaminaManager Stam => StaminaManager.Instance;
    static CombatManager Combat => CombatManager.Instance;
    static MacheteShopManager Machetes => MacheteShopManager.Instance;
    static SkillTreeManager Skills => SkillTreeManager.Instance;
    static PerkManager Perks => PerkManager.Instance;
    static LegacyManager Legacy => LegacyManager.Instance;
    static DebtManager Debt => DebtManager.Instance;

    static float Src(SS s, ST t) => Stats.GetSource(s, t);
    static void Src(SS s, ST t, float v) => Stats.Set(s, t, v);

    public int billCycle { get => Debt.BillCycle; set => Debt.BillCycle = value; }
    public int daysLeft { get => Debt.DaysLeft; set => Debt.DaysLeft = value; }
    public int bestCycleReached { get => Debt.BestCycleReached; set => Debt.BestCycleReached = value; }
    public float waterTargetML => Debt.TargetWater;  
    public float waterCurrentML { get => Res.Water; set => Res.SetWater(value); }
    public int coconutCoins { get => Res.Coins; set => Res.SetCoins(value); }

    public float stamina { get => Stam.Current; set => Stam.Current = value; }
    public float staminaMaxBase { get => Stats.staminaMaxBase; set => Stats.staminaMaxBase = value; }
    public float staminaStartDelay { get => Stam.startDelay; set => Stam.startDelay = value; }
    float dayElapsed { get => Stam.DayElapsed; set => Stam.DayElapsed = value; }
    float dayStaminaRecovered { get => Stam.RecoveredToday; set => Stam.RecoveredToday = value; }

    public float legacyMultiplier { get => Stats.legacyMultiplier; set => Stats.legacyMultiplier = value; }
    public float coinDropChance { get => Stats.coinDropChance; set => Stats.coinDropChance = value; }
    float perkWaterMultiplicative { get => Stats.perkWaterMultiplicative; set => Stats.perkWaterMultiplicative = value; }
    bool lastBreathSwingActive { get => Stats.lastBreathSwingActive; set => Stats.lastBreathSwingActive = value; }

    public float baseWaterPerKill { get => Combat.baseWaterPerKill; set => Combat.baseWaterPerKill = value; }
    public float waterVariance { get => Combat.waterVariance; set => Combat.waterVariance = value; }
    public float executeHpThreshold { get => Combat.executeHpThreshold; set => Combat.executeHpThreshold = value; }
    public float earlyDayDuration { get => Combat.earlyDayDuration; set => Combat.earlyDayDuration = value; }
    public float jackpotBaseMultiplier { get => Combat.jackpotBaseMultiplier; set => Combat.jackpotBaseMultiplier = value; }
    public int streakThreshold { get => Combat.streakThreshold; set => Combat.streakThreshold = value; }
    public float streakDamageBonusPerHit { get => Combat.streakDamageBonusPerHit; set => Combat.streakDamageBonusPerHit = value; }
    public float streakWaterBonusPerHit { get => Combat.streakWaterBonusPerHit; set => Combat.streakWaterBonusPerHit = value; }
    public int streakMaxStacks { get => Combat.streakMaxStacks; set => Combat.streakMaxStacks = value; }
    int currentHitStreak { get => Combat.CurrentHitStreak; set => Combat.CurrentHitStreak = value; }
    int streakForgivesLeft { get => Combat.StreakForgivesLeft; set => Combat.StreakForgivesLeft = value; }

    public int totalCoconutsKilled { get => Combat.totalCoconutsKilled; set => Combat.totalCoconutsKilled = value; }
    public int runCoconutsKilled { get => Combat.runCoconutsKilled; set => Combat.runCoconutsKilled = value; }
    public int runMoneyEarned { get => Combat.runMoneyEarned; set => Combat.runMoneyEarned = value; }
    public int bestRunKills { get => Combat.bestRunKills; set => Combat.bestRunKills = value; }
    public int dayCoconutsKilled { get => Combat.dayCoconutsKilled; set => Combat.dayCoconutsKilled = value; }
    public int dayMoneyEarned { get => Combat.dayMoneyEarned; set => Combat.dayMoneyEarned = value; }
    public int daySwings { get => Combat.daySwings; set => Combat.daySwings = value; }
    public int dayHits { get => Combat.dayHits; set => Combat.dayHits = value; }
    public int dayCoinsFound { get => Combat.dayCoinsFound; set => Combat.dayCoinsFound = value; }
    public bool lastKillFoundCoin { get => Combat.lastKillFoundCoin; set => Combat.lastKillFoundCoin = value; }
    public bool lastKillHadJackpot { get => Combat.lastKillHadJackpot; set => Combat.lastKillHadJackpot = value; }

    public float skillCritChanceBonus { get => Src(SS.Skill, ST.CritChance); set => Src(SS.Skill, ST.CritChance, value); }
    float skillDamageBonus { get => Src(SS.Skill, ST.Damage); set => Src(SS.Skill, ST.Damage, value); }
    float skillStaminaBonus { get => Src(SS.Skill, ST.StaminaMax); set => Src(SS.Skill, ST.StaminaMax, value); }
    float skillSwingIntervalReduction { get => Src(SS.Skill, ST.SwingReduction); set => Src(SS.Skill, ST.SwingReduction, value); }
    float skillHitRadiusBonus { get => Src(SS.Skill, ST.HitRadius); set => Src(SS.Skill, ST.HitRadius, value); }
    float skillWaterMultiplierBonus { get => Src(SS.Skill, ST.WaterMult); set => Src(SS.Skill, ST.WaterMult, value); }
    float skillCritDamageBonus { get => Src(SS.Skill, ST.CritDamage); set => Src(SS.Skill, ST.CritDamage, value); }
    float skillJackpotChance { get => Src(SS.Skill, ST.JackpotChance); set => Src(SS.Skill, ST.JackpotChance, value); }
    float skillJackpotMultBonus { get => Src(SS.Skill, ST.JackpotWater); set => Src(SS.Skill, ST.JackpotWater, value); }
    float skillExtraCoconutChance { get => Src(SS.Skill, ST.ExtraCoconutChance); set => Src(SS.Skill, ST.ExtraCoconutChance, value); }
    int skillStreakForgives { get => Mathf.RoundToInt(Src(SS.Skill, ST.StreakForgives)); set => Src(SS.Skill, ST.StreakForgives, value); }
    int skillStreakStackBonus { get => Mathf.RoundToInt(Src(SS.Skill, ST.StreakStacks)); set => Src(SS.Skill, ST.StreakStacks, value); }
    float skillStreakPowerBonus { get => Src(SS.Skill, ST.StreakPower); set => Src(SS.Skill, ST.StreakPower, value); }
    float skillLastBreathBonus { get => Src(SS.Skill, ST.LastBreathWater); set => Src(SS.Skill, ST.LastBreathWater, value); }
    float skillLastBreathSwingReduction { get => Src(SS.Skill, ST.LastBreathSwing); set => Src(SS.Skill, ST.LastBreathSwing, value); }
    float skillExecuteBonus { get => Src(SS.Skill, ST.ExecuteDamage); set => Src(SS.Skill, ST.ExecuteDamage, value); }
    float skillMultiHitBonus { get => Src(SS.Skill, ST.MultiHitDamage); set => Src(SS.Skill, ST.MultiHitDamage, value); }
    float skillChainSplashPercent { get => Src(SS.Skill, ST.ChainSplash); set => Src(SS.Skill, ST.ChainSplash, value); }
    float skillOrderRefund { get => Src(SS.Skill, ST.OrderRefund); set => Src(SS.Skill, ST.OrderRefund, value); }
    float skillEarlyDayBonus { get => Src(SS.Skill, ST.EarlyDayWater); set => Src(SS.Skill, ST.EarlyDayWater, value); }
    float skillStaminaRecoveryChance { get => Src(SS.Skill, ST.StaminaRecoveryChance); set => Src(SS.Skill, ST.StaminaRecoveryChance, value); }
    float skillStaminaRecoveryAmount { get => Src(SS.Skill, ST.StaminaRecoveryAmount); set => Src(SS.Skill, ST.StaminaRecoveryAmount, value); }
    int skillStaminaRecoveryLevel { get => Mathf.RoundToInt(Src(SS.Skill, ST.StaminaRecoveryLevel)); set => Src(SS.Skill, ST.StaminaRecoveryLevel, value); }

    public float perkDamageBonus { get => Src(SS.Perk, ST.Damage); set => Src(SS.Perk, ST.Damage, value); }
    public float perkStaminaMaxBonus { get => Src(SS.Perk, ST.StaminaMax); set => Src(SS.Perk, ST.StaminaMax, value); }
    public float perkWaterMultiplierBonus { get => Src(SS.Perk, ST.WaterMult); set => Src(SS.Perk, ST.WaterMult, value); }
    public float perkSwingIntervalReduction { get => Src(SS.Perk, ST.SwingReduction); set => Src(SS.Perk, ST.SwingReduction, value); }
    public float perkCritChanceBonus { get => Src(SS.Perk, ST.CritChance); set => Src(SS.Perk, ST.CritChance, value); }

    public float relicDamageBonus { get => Src(SS.Relic, ST.Damage); set => Src(SS.Relic, ST.Damage, value); }
    public float relicStaminaMaxBonus { get => Src(SS.Relic, ST.StaminaMax); set => Src(SS.Relic, ST.StaminaMax, value); }
    public float relicWaterMultiplierBonus { get => Src(SS.Relic, ST.WaterMult); set => Src(SS.Relic, ST.WaterMult, value); }
    public float relicSwingIntervalReduction { get => Src(SS.Relic, ST.SwingReduction); set => Src(SS.Relic, ST.SwingReduction, value); }
    public float relicHitRadiusBonus { get => Src(SS.Relic, ST.HitRadius); set => Src(SS.Relic, ST.HitRadius, value); }
    public float relicCritChanceBonus { get => Src(SS.Relic, ST.CritChance); set => Src(SS.Relic, ST.CritChance, value); }

    public float legacyDamageBonus { get => Src(SS.Legacy, ST.Damage); set => Src(SS.Legacy, ST.Damage, value); }
    public float legacyStaminaBonus { get => Src(SS.Legacy, ST.StaminaMax); set => Src(SS.Legacy, ST.StaminaMax, value); }
    public float legacyWaterMultiplierBonus { get => Src(SS.Legacy, ST.WaterMult); set => Src(SS.Legacy, ST.WaterMult, value); }
    public float legacyCritChanceBonus { get => Src(SS.Legacy, ST.CritChance); set => Src(SS.Legacy, ST.CritChance, value); }
    int legacyExtraDays { get => Mathf.RoundToInt(Src(SS.Legacy, ST.ExtraDays)); set => Src(SS.Legacy, ST.ExtraDays, value); }
    float legacyStartingWater { get => Src(SS.Legacy, ST.StartingWater); set => Src(SS.Legacy, ST.StartingWater, value); }
    float legacyJackpotChance { get => Src(SS.Legacy, ST.JackpotChance); set => Src(SS.Legacy, ST.JackpotChance, value); }
    int legacyStreakStacks { get => Mathf.RoundToInt(Src(SS.Legacy, ST.StreakStacks)); set => Src(SS.Legacy, ST.StreakStacks, value); }
    float legacyHitRadiusBonus { get => Src(SS.Legacy, ST.HitRadius); set => Src(SS.Legacy, ST.HitRadius, value); }
    float legacyOrderDiscount { get => Src(SS.Legacy, ST.OrderDiscount); set => Src(SS.Legacy, ST.OrderDiscount, value); }
    int legacyStartingMachete { get => Mathf.RoundToInt(Src(SS.Legacy, ST.StartingMachete)); set => Src(SS.Legacy, ST.StartingMachete, value); }
    int legacyStartingCoconuts { get => Mathf.RoundToInt(Src(SS.Legacy, ST.StartingCoconuts)); set => Src(SS.Legacy, ST.StartingCoconuts, value); }
    float legacyMultiplierBonus { get => Src(SS.Legacy, ST.LegacyMultiplierBonus); set => Src(SS.Legacy, ST.LegacyMultiplierBonus, value); }
    float legacyCoinChanceBonus { get => Src(SS.Legacy, ST.CoinChance); set => Src(SS.Legacy, ST.CoinChance, value); }

    public float GetCritChance() => Stats.CritChance;
    public float GetCritMultiplier() => Stats.CritMultiplier;
    public float GetStaminaMax() => Stats.StaminaMax;
    float GetRawSwingInterval(bool includeLastBreath) => Stats.RawSwingInterval(includeLastBreath);
    void RecalculateAllStats() => Stats.Recalculate(GetCurrentMachete());
    public void RefreshCombatStats() => RecalculateAllStats();
    public void ApplyTemporaryCurse(float durationSeconds) => Stats.ApplyTemporaryCurse(durationSeconds);
    void ClearCurse() => Stats.ClearCurse();
    public void ApplyStaminaPenalty(float amount) => Stam.ApplyPenalty(amount);

    public void RegisterSwingResult(bool hit) => Combat.RegisterSwingResult(hit);
    public float GetStreakDamageMultiplier() => Combat.GetStreakDamageMultiplier();
    public float GetStreakWaterMultiplier() => Combat.GetStreakWaterMultiplier();
    public float GetLastBreathMultiplier() => Combat.GetLastBreathMultiplier();
    public float GetExecuteDamageMultiplier(float targetHpPercent) => Combat.GetExecuteDamageMultiplier(targetHpPercent);
    public float GetMultiHitMultiplier(int targetsHit) => Combat.GetMultiHitMultiplier(targetsHit);
    public int OnCoconutDestroyed(float lootMultiplier, out float waterGained) => Combat.OnCoconutDestroyed(lootMultiplier, out waterGained);
    public int OnCoconutDestroyed(float lootMultiplier = 1f) => Combat.OnCoconutDestroyed(lootMultiplier);
    public void ApplyChainSplash(Vector3 position, int killingDamage, GameObject source) => Combat.ApplyChainSplash(position, killingDamage, source);

    [System.Obsolete("Ahora devuelve el bonus de AGUA extra (no de dinero). Se deja el nombre por compatibilidad con la UI existente.")]
    public float GetSkillMoneyMultiplierBonusPercent() => skillWaterMultiplierBonus * 100f;
    public float GetSkillWaterMultiplierBonusPercent() => skillWaterMultiplierBonus * 100f;

    public List<MacheteData> macheteOptions => Machetes.Options;
    public int equippedMacheteIndex { get => Machetes.EquippedIndex; set => Machetes.EquippedIndex = value; }
    public MacheteData GetMacheteData(int index) => Machetes.Get(index);
    public MacheteData GetCurrentMachete() => Machetes.Current;
    public MacheteData GetNextMachete() => Machetes.Next;
    bool IsMacheteOwned(int index) => Machetes.IsOwned(index);
    public bool CanAffordAnyMachete() => Machetes.CanAffordAny();

    public void BuyNextMachete()
    {
        if (!Machetes.TryBuyNext(out MacheteData bought)) return;

        if (machete != null && machete.activeInHierarchy && MacheteController.Instance != null)
            MacheteController.Instance.EquipModel(equippedMacheteIndex);

        BetaLog("machete_" + equippedMacheteIndex);
        Log(Loc("log_new_machete", bought.macheteName));
        RefreshAllUI();
        CheckAchievements();
    }
    public List<SkillNode> skillTree => Skills.Nodes;
    HashSet<string> unlockedSkillIds => Skills.Unlocked;
    public SkillNode GetSkillNode(string id) => Skills.GetNode(id);
    public bool IsSkillUnlocked(string id) => Skills.IsUnlocked(id);
    public bool CanUnlockSkillPublic(string id) => Skills.CanUnlock(id);
    public bool CanAffordAnySkill() => Skills.CanAffordAny();

    public void UnlockSkill(string id)
    {
        if (!Skills.TryUnlock(id, out SkillNode node)) return;
        BetaLog("skill_" + id);
        Log(Loc("log_upgrade_bought", node.nodeName));
        RefreshAllUI();
        CheckAchievements();
    }

    public List<PerkOption> perkPool => Perks.Pool;
    public int maxCopiesPerPerk { get => Perks.maxCopiesPerPerk; set => Perks.maxCopiesPerPerk = value; }
    Dictionary<string, int> perkCopies => Perks.Copies;
    PerkOption[] PickRandomPerks(int count) => Perks.PickRandom(count, billCycle);
    void ApplyPerk(PerkOption perk) => Perks.Apply(perk);

    public int legacyPoints { get => Legacy.Points; set => Legacy.Points = value; }
    public int totalLegacyPointsEarned { get => Legacy.TotalEarned; set => Legacy.TotalEarned = value; }
    public List<LegacyItem> legacyShop => Legacy.Items;
    HashSet<string> purchasedLegacyItems => Legacy.Purchased;
    public LegacyItem GetLegacyItem(string itemName) => Legacy.GetItem(itemName);
    public bool IsLegacyItemPurchased(string itemName) => Legacy.IsPurchased(itemName);
    void ApplyLegacyEffect(LegacyItem item) => Legacy.ApplyEffect(item);
    void RecalcLegacyMultiplier() => Legacy.RecalcMultiplier();
    public float GetLegacyHpScale() => Legacy.GetHpScale(billCycle);
    public float GetLegacyLootScale() => Legacy.GetLootScale(billCycle);

    public void BuyLegacyItem(string itemName)
    {
        if (currentPhase != Phase.BetweenRuns) return;

        LegacyManager.BuyResult result = Legacy.TryBuy(itemName, out LegacyItem item, out string missing);
        if (result == LegacyManager.BuyResult.NeedsPrevious) { Log("Primero necesitas: " + missing); return; }
        if (result != LegacyManager.BuyResult.Ok) return;

        Log(Loc("log_legacy_bought", item.itemName));
        RefreshAllUI();
        SaveGame();
        CheckAchievements();
    }

    void CheckAchievements() => AchievementTracker.Check();
    public bool CanUseGacha() => coconutCoins >= GetGachaCost();
    int GetGachaCost() => 1;
}