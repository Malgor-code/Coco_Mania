using System.Collections;
using UnityEngine;
using static MacheteData;

public enum StatSource { Skill, Perk, Relic, Legacy }

public enum StatType
{
    Damage, StaminaMax, SwingReduction, HitRadius, WaterMult,
    CritChance, CritDamage, JackpotChance, JackpotWater, ExtraCoconutChance,
    StreakForgives, StreakStacks, StreakPower,
    LastBreathWater, LastBreathSwing,
    ExecuteDamage, MultiHitDamage, ChainSplash,
    OrderRefund, OrderDiscount, EarlyDayWater,
    StaminaRecoveryChance, StaminaRecoveryAmount, StaminaRecoveryLevel,
    ExtraDays, StartingWater, StartingMachete, StartingCoconuts,
    LegacyMultiplierBonus, CoinChance
}
public class StatsManager : MonoBehaviour
{
    static StatsManager instance;
    public static StatsManager Instance
    {
        get
        {
            if (instance == null) instance = ManagerBootstrap.Ensure<StatsManager>();
            return instance;
        }
    }

    public const float SwingFloor = 0.2f;
    static readonly int SourceCount = System.Enum.GetValues(typeof(StatSource)).Length;
    static readonly int TypeCount = System.Enum.GetValues(typeof(StatType)).Length;

    [Header("Base")]
    public float staminaMaxBase = 15f;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que un coco suelte 1 moneda al morir (base 0.5%).")]
    public float coinDropChance = 0.005f;

    [HideInInspector] public float legacyMultiplier = 1f;
    [HideInInspector] public float perkWaterMultiplicative = 1f;
    [HideInInspector] public bool lastBreathSwingActive;

    public MacheteData CurrentMachete { get; private set; }

    readonly float[,] values = new float[SourceCount, TypeCount];
    float curseSwingPenalty, curseWaterPenalty, curseStaminaPenalty, curseSpawnPenalty;
    int curseGeneration;
    public float CurseWaterPenalty => curseWaterPenalty;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
    public float Get(StatType t)
    {
        int ti = (int)t;
        float sum = 0f;
        for (int s = 0; s < SourceCount; s++) sum += values[s, ti];
        return sum;
    }

    public float GetSource(StatSource s, StatType t) => values[(int)s, (int)t];
    public void Set(StatSource s, StatType t, float v) => values[(int)s, (int)t] = v;
    public void Add(StatSource s, StatType t, float v) => values[(int)s, (int)t] += v;

    public void ClearSource(StatSource s)
    {
        int si = (int)s;
        for (int t = 0; t < TypeCount; t++) values[si, t] = 0f;
    }

    public void ResetRunSources()
    {
        ClearSource(StatSource.Skill);
        ClearSource(StatSource.Perk);
        ClearSource(StatSource.Relic);
        perkWaterMultiplicative = 1f;
        ClearCurse();
    }

    public float Passive(MachetePassive p)
    {
        return (CurrentMachete != null && CurrentMachete.passive == p) ? CurrentMachete.passiveValue : 0f;
    }

    public float RawCritChance => Get(StatType.CritChance) + Passive(MachetePassive.CritChance);
    public float CritChance => Mathf.Clamp01(RawCritChance);

    public float CritMultiplier =>
        1.3f + Get(StatType.CritDamage) + Passive(MachetePassive.CritDamage) + Mathf.Max(0f, RawCritChance - 1f);

    public float StaminaMax =>
        Mathf.Max(1f, staminaMaxBase + Get(StatType.StaminaMax) + Passive(MachetePassive.StaminaMax) - curseStaminaPenalty);

    public float RawSwingInterval(bool includeLastBreath)
    {
        float baseSwing = CurrentMachete != null ? CurrentMachete.baseSwingInterval : 1.2f;
        return baseSwing - Get(StatType.SwingReduction) + curseSwingPenalty
             - (includeLastBreath && lastBreathSwingActive ? Get(StatType.LastBreathSwing) : 0f);
    }

    public void Recalculate(MacheteData machete)
    {
        CurrentMachete = machete;

        float baseDmg = machete != null ? machete.baseDamage : 3f;
        float baseRadius = machete != null ? machete.baseHitRadius : .8f;
        int damage = Mathf.RoundToInt(baseDmg + Get(StatType.Damage));

        MacheteController mc = MacheteController.Instance;
        if (mc != null)
        {
            mc.damageOverride = damage;
            mc.hitRadius = baseRadius + Get(StatType.HitRadius);
            mc.swingInterval = Mathf.Max(SwingFloor, RawSwingInterval(true));
        }
    }
    public void ApplyTemporaryCurse(float durationSeconds)
    {
        StartCoroutine(TemporaryCurseRoutine(durationSeconds));
    }

    IEnumerator TemporaryCurseRoutine(float duration)
    {
        int gen = curseGeneration;
        int effect = Random.Range(0, 4);
        float amount = 0f;

        switch (effect)
        {
            case 0:
                amount = 0.15f; curseSwingPenalty += amount;
                Recalculate(CurrentMachete);
                GameLog.Say("Maldicion: golpes mas lentos por un rato...");
                break;
            case 1:
                amount = 0.15f; curseWaterPenalty += amount;
                GameLog.Say("Maldicion: menos agua por un rato...");
                break;
            case 2:
                amount = staminaMaxBase * 0.2f; curseStaminaPenalty += amount;
                GameLog.Say("Maldicion: menos resistencia por un rato...");
                break;
            case 3:
                if (CoconutSpawner.Instance == null) yield break;
                amount = 1.5f; curseSpawnPenalty += amount;
                CoconutSpawner.Instance.spawnInterval += amount;
                GameLog.Say("Maldicion: los cocos tardan mas en aparecer...");
                break;
        }

        yield return new WaitForGameSeconds(duration);

        if (gen != curseGeneration) yield break;

        switch (effect)
        {
            case 0: curseSwingPenalty -= amount; Recalculate(CurrentMachete); break;
            case 1: curseWaterPenalty -= amount; break;
            case 2: curseStaminaPenalty -= amount; break;
            case 3:
                curseSpawnPenalty -= amount;
                if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.spawnInterval -= amount;
                break;
        }
    }

    public void ClearCurse()
    {
        curseGeneration++;
        if (curseSpawnPenalty > 0f && CoconutSpawner.Instance != null)
            CoconutSpawner.Instance.spawnInterval -= curseSpawnPenalty;
        curseSwingPenalty = curseWaterPenalty = curseStaminaPenalty = curseSpawnPenalty = 0f;
    }
}