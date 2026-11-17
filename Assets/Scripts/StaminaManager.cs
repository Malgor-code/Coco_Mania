using UnityEngine;

public class StaminaManager : MonoBehaviour
{
    static StaminaManager instance;
    public static StaminaManager Instance
    {
        get
        {
            if (instance == null) instance = ManagerBootstrap.Ensure<StaminaManager>();
            return instance;
        }
    }

    [Header("Ritmo del dia")]
    [Tooltip("Segundos al inicio del dia en los que la stamina todavia no baja.")]
    public float startDelay = 2f;

    public float Current { get; set; } = 15f;
    public float DayElapsed { get; set; }
    public float RecoveredToday { get; set; }

    static readonly float[] RecoveryCaps = { 4f, 6f, 9f, 12f, 16f };
    const float RecoveryBase = 1f;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public float Max => StatsManager.Instance.StaminaMax;

    public void RefillToMax() => Current = Max;
    public void ApplyPenalty(float amount) => Current = Mathf.Max(0f, Current - amount);

    public void ResetDay()
    {
        DayElapsed = 0f;
        RecoveredToday = 0f;
    }

    float RecoveryCap()
    {
        int level = Mathf.RoundToInt(StatsManager.Instance.Get(StatType.StaminaRecoveryLevel));
        if (level <= 0) return 0f;
        return RecoveryCaps[Mathf.Min(level, RecoveryCaps.Length) - 1];
    }

    public float TryRecoverOnKill()
    {
        StatsManager stats = StatsManager.Instance;
        float chance = stats.Get(StatType.StaminaRecoveryChance);
        float cap = RecoveryCap();

        if (chance > 0f && RecoveredToday < cap && Random.value < chance)
        {
            float amount = Mathf.Min(RecoveryBase + stats.Get(StatType.StaminaRecoveryAmount), cap - RecoveredToday);
            Current = Mathf.Min(Max, Current + amount);
            RecoveredToday += amount;
            return amount;
        }
        return 0f;
    }
}