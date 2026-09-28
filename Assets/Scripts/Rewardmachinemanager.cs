using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardMachineManager : MonoBehaviour
{
    public static RewardMachineManager Instance;

    [Header("Base de datos de recompensas")]
    public RewardDatabase database;

    [Header("Costo de tirada")]
    public int pullCost = 100;

    [Header("Limite de reliquias activas por intento")]
    public int maxRelics = 5;

    [Header("Probabilidades base (deberian sumar 100)")]
    [Range(0f, 100f)] public float commonChance = 55f;
    [Range(0f, 100f)] public float uncommonChance = 28f;
    [Range(0f, 100f)] public float rareChance = 12f;
    [Range(0f, 100f)] public float epicChance = 4f;
    [Range(0f, 100f)] public float legendaryChance = 1f;

    [Header("Pity / garantia")]
    public int pityEpicThreshold = 20;
    public int pityLegendaryThreshold = 50;

    [Header("Fragmentos de Cobrador otorgados por duplicado, segun rareza")]
    public int fragmentsCommon = 1;
    public int fragmentsUncommon = 2;
    public int fragmentsRare = 5;
    public int fragmentsEpic = 15;
    public int fragmentsLegendary = 50;

    [Header("Precios de la tienda de fragmentos")]
    [Tooltip("Cualquier recompensa de rareza Comun (cualquier categoria).")]
    public int shopPriceCommon = 10;
    [Tooltip("Cualquier recompensa de categoria Permanente (cualquier rareza).")]
    public int shopPricePermanent = 25;
    [Tooltip("Cualquier objeto fisico (cualquier rareza).")]
    public int shopPricePhysical = 75;
    [Tooltip("Cualquier recompensa Legendaria (cualquier categoria).")]
    public int shopPriceLegendary = 150;

    [Header("Datos en tiempo de ejecucion (visibles para depurar)")]
    public CurrentRunData currentRun = new CurrentRunData();
    public PersistentData persistent = new PersistentData();
    public event Action<RewardData, bool> OnPullResult;
    public event Action OnStateChanged;
    public event Action<RewardData> OnPhysicalItemTriggered;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayStarted += HandleDayStarted;
            GameManager.Instance.OnRunReset += HandleRunReset;
        }
        else
        {
            Debug.LogWarning("[RewardMachineManager] No se encontro GameManager.Instance en Start(). " +
                              "Verifica el orden de inicializacion de escena (el GameManager debe existir antes).");
        }

        if (database == null)
        {
            Debug.LogWarning("[RewardMachineManager] No hay ninguna RewardDatabase asignada en el Inspector.");
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayStarted -= HandleDayStarted;
            GameManager.Instance.OnRunReset -= HandleRunReset;
        }
    }

    void Update()
    {
        if (database == null || currentRun.physicalItems.Count == 0) return;

        for (int i = currentRun.physicalItems.Count - 1; i >= 0; i--)
        {
            var inst = currentRun.physicalItems[i];
            var data = database.GetById(inst.rewardId);
            if (data == null) continue;
            if (data.triggerMode != PhysicalTriggerMode.RealTimeInterval) continue;

            inst.secondsSinceLastTrigger += Time.deltaTime;
            if (inst.secondsSinceLastTrigger >= Mathf.Max(0.01f, data.intervalSeconds))
            {
                inst.secondsSinceLastTrigger = 0f;
                ExecuteAutomaticActivation(inst, data);
            }
        }
    }

    void ExecuteAutomaticActivation(PhysicalItemInstance inst, RewardData data)
    {
        ExecutePhysicalEffect(data);
        OnPhysicalItemTriggered?.Invoke(data);

        if (inst.usesRemaining > 0)
        {
            inst.usesRemaining--;
            if (inst.usesRemaining == 0) currentRun.physicalItems.Remove(inst);
        }

        OnStateChanged?.Invoke();
    }

    public bool CanPull()
    {
        return GameManager.Instance != null && database != null && GameManager.Instance.coconutCoins >= pullCost;
    }

    public RewardData Pull()
    {
        if (!CanPull())
        {
            OnPullResult?.Invoke(null, false);
            return null;
        }

        GameManager.Instance.coconutCoins -= pullCost;

        currentRun.pullsSinceEpic++;
        currentRun.pullsSinceLegendary++;

        RewardRarity rarity = RollRarity();
        RewardData data = PickRewardOfRarity(rarity);

        if (data == null)
        {
            Debug.LogWarning($"[RewardMachineManager] No hay ninguna RewardData de rareza {rarity} (ni de una rareza menor) en la base de datos. Se devolvio la moneda gastada.");
            GameManager.Instance.coconutCoins += pullCost; 
            currentRun.pullsSinceEpic--;
            currentRun.pullsSinceLegendary--;
            OnStateChanged?.Invoke();
            OnPullResult?.Invoke(null, false);
            return null;
        }

        bool wasDuplicate = GrantReward(data);

        OnPullResult?.Invoke(data, wasDuplicate);
        OnStateChanged?.Invoke();
        return data;
    }

    RewardRarity RollRarity()
    {
        bool forceLegendary = currentRun.pullsSinceLegendary >= pityLegendaryThreshold;
        bool forceEpicOrBetter = !forceLegendary && currentRun.pullsSinceEpic >= pityEpicThreshold;

        RewardRarity result;

        if (forceLegendary)
        {
            result = RewardRarity.Legendary;
        }
        else if (forceEpicOrBetter)
        {
            float epicW = Mathf.Max(0.0001f, epicChance);
            float legW = Mathf.Max(0.0001f, legendaryChance);
            result = UnityEngine.Random.value < (legW / (epicW + legW)) ? RewardRarity.Legendary : RewardRarity.Epic;
        }
        else
        {
            result = RollNormalRarity();
        }

        if (result == RewardRarity.Legendary)
        {
            currentRun.pullsSinceEpic = 0;
            currentRun.pullsSinceLegendary = 0;
        }
        else if (result == RewardRarity.Epic)
        {
            currentRun.pullsSinceEpic = 0;
        }

        return result;
    }

    RewardRarity RollNormalRarity()
    {
        float total = commonChance + uncommonChance + rareChance + epicChance + legendaryChance;
        if (total <= 0f) return RewardRarity.Common;

        float roll = UnityEngine.Random.value * total;

        if ((roll -= commonChance) < 0f) return RewardRarity.Common;
        if ((roll -= uncommonChance) < 0f) return RewardRarity.Uncommon;
        if ((roll -= rareChance) < 0f) return RewardRarity.Rare;
        if ((roll -= epicChance) < 0f) return RewardRarity.Epic;
        return RewardRarity.Legendary;
    }

    RewardData PickRewardOfRarity(RewardRarity rarity)
    {
        for (int r = (int)rarity; r >= 0; r--)
        {
            var pool = database.GetByRarity((RewardRarity)r);
            if (pool.Count > 0) return pool[UnityEngine.Random.Range(0, pool.Count)];
        }
        return null;
    }
    bool GrantReward(RewardData data)
    {
        bool isDuplicate = !data.canRepeat && IsCurrentlyOwned(data);

        if (isDuplicate)
        {
            int frags = GetFragmentsForRarity(data.rarity, data.fragmentOverride);
            persistent.fragments += frags;
            return true;
        }

        persistent.unlockedRewardIds.Add(data.id);

        switch (data.category)
        {
            case RewardCategory.Temporary: ApplyTemporaryReward(data); break;
            case RewardCategory.PermanentRelic: ApplyRelicReward(data); break;
            case RewardCategory.PhysicalItem: ApplyPhysicalReward(data); break;
        }

        return false;
    }

    bool IsCurrentlyOwned(RewardData data)
    {
        switch (data.category)
        {
            case RewardCategory.Temporary:
                return currentRun.temporaryRewards.Exists(t => t.rewardId == data.id);
            case RewardCategory.PermanentRelic:
                return currentRun.relics.Exists(r => r.rewardId == data.id);
            case RewardCategory.PhysicalItem:
                return currentRun.physicalItems.Exists(p => p.rewardId == data.id);
        }
        return false;
    }

    int GetFragmentsForRarity(RewardRarity rarity, int overrideValue)
    {
        if (overrideValue >= 0) return overrideValue;

        switch (rarity)
        {
            case RewardRarity.Common: return fragmentsCommon;
            case RewardRarity.Uncommon: return fragmentsUncommon;
            case RewardRarity.Rare: return fragmentsRare;
            case RewardRarity.Epic: return fragmentsEpic;
            default: return fragmentsLegendary;
        }
    }

    void ApplyTemporaryReward(RewardData data)
    {
        currentRun.temporaryRewards.Add(new ActiveTemporaryReward
        {
            rewardId = data.id,
            effect = data.effect,
            value = data.value,
            daysRemaining = Mathf.Max(1, data.durationDays)
        });

        ApplyStatEffect(data.effect, data.value, 1);
    }

    void ApplyRelicReward(RewardData data)
    {
        if (currentRun.relics.Count >= maxRelics)
        {
            int frags = GetFragmentsForRarity(data.rarity, data.fragmentOverride);
            persistent.fragments += frags;
            return;
        }

        currentRun.relics.Add(new ActiveRelic { rewardId = data.id, effect = data.effect, value = data.value });
        ApplyStatEffect(data.effect, data.value, 1);
    }

    void ApplyPhysicalReward(RewardData data)
    {
        currentRun.physicalItems.Add(new PhysicalItemInstance
        {
            rewardId = data.id,
            usesRemaining = data.usesPerRun,
            secondsSinceLastTrigger = 0f
        });
    }

    void ApplyStatEffect(RewardEffect effect, float value, int sign)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        switch (effect)
        {
            case RewardEffect.DamageBonus: gm.relicDamageBonus += sign * value; break;
            case RewardEffect.CritChanceBonus: gm.relicCritChanceBonus += sign * value; break;
            case RewardEffect.WaterMultiplierBonus: gm.relicWaterMultiplierBonus += sign * value; break;
            case RewardEffect.StaminaMaxBonus: gm.relicStaminaMaxBonus += sign * value; break;
            case RewardEffect.HitRadiusBonus: gm.relicHitRadiusBonus += sign * value; break;
            case RewardEffect.SwingIntervalReduction: gm.relicSwingIntervalReduction += sign * value; break;
            case RewardEffect.CoinDropChanceBonus: gm.coinDropChance = Mathf.Clamp01(gm.coinDropChance + sign * value); break;
            default: return; 
        }

        gm.RefreshCombatStats();
    }
    void ExecutePhysicalEffect(RewardData data)
    {
        var gm = GameManager.Instance;

        switch (data.effect)
        {
            case RewardEffect.InstantWaterBurst:
                if (gm != null) gm.waterCurrentML += data.value;
                break;

            case RewardEffect.DestroyRandomCoconuts:
                DestroyRandomCoconuts(Mathf.RoundToInt(data.value));
                break;

            case RewardEffect.SkipNextDayCounter:
                if (gm != null) gm.skipNextDayDecrement = true;
                break;

            case RewardEffect.TempCoinMagnet:
                StartCoroutine(TempCoinChanceBoost(data.value, data.durationSeconds));
                break;

            case RewardEffect.TempWaterBasket:
                StartCoroutine(TempWaterMultBoost(data.value, data.durationSeconds));
                break;
        }
    }

    void DestroyRandomCoconuts(int count)
    {
        if (CoconutSpawner.Instance == null) return;

        var active = CoconutSpawner.Instance.GetActiveCoconuts();
        for (int i = 0; i < count && active.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, active.Count);
            var target = active[idx];
            active.RemoveAt(idx);
            if (target != null) target.TakeDamage(999999, false);
        }
    }

    IEnumerator TempCoinChanceBoost(float amount, float duration)
    {
        var gm = GameManager.Instance;
        if (gm == null) yield break;

        gm.coinDropChance = Mathf.Clamp01(gm.coinDropChance + amount);
        yield return new WaitForSeconds(duration);

        if (GameManager.Instance != null)
            GameManager.Instance.coinDropChance = Mathf.Clamp01(GameManager.Instance.coinDropChance - amount);
    }

    IEnumerator TempWaterMultBoost(float amount, float duration)
    {
        var gm = GameManager.Instance;
        if (gm == null) yield break;

        gm.relicWaterMultiplierBonus += amount;
        yield return new WaitForSeconds(duration);

        if (GameManager.Instance != null)
            GameManager.Instance.relicWaterMultiplierBonus -= amount;
    }
    public List<RewardData> GetShopPool(RewardCategory? category, RewardRarity? rarity)
    {
        List<RewardData> result = new List<RewardData>();
        if (database == null) return result;

        foreach (var r in database.allRewards)
        {
            if (r == null) continue;
            if (category.HasValue && r.category != category.Value) continue;
            if (rarity.HasValue && r.rarity != rarity.Value) continue;
            result.Add(r);
        }
        return result;
    }

    public bool BuyFromFragmentShop(string rewardId, int price)
    {
        if (persistent.fragments < price) return false;

        var data = database != null ? database.GetById(rewardId) : null;
        if (data == null) return false;

        persistent.fragments -= price;
        GrantReward(data);

        OnStateChanged?.Invoke();
        return true;
    }
    void HandleDayStarted()
    {
        for (int i = currentRun.temporaryRewards.Count - 1; i >= 0; i--)
        {
            var t = currentRun.temporaryRewards[i];
            t.daysRemaining--;
            if (t.daysRemaining <= 0)
            {
                ApplyStatEffect(t.effect, t.value, -1);
                currentRun.temporaryRewards.RemoveAt(i);
            }
        }
        if (database != null)
        {
            for (int i = currentRun.physicalItems.Count - 1; i >= 0; i--)
            {
                var inst = currentRun.physicalItems[i];
                var data = database.GetById(inst.rewardId);
                if (data == null) continue;
                if (data.triggerMode != PhysicalTriggerMode.EveryNewDay) continue;

                ExecuteAutomaticActivation(inst, data);
            }
        }

        OnStateChanged?.Invoke();
    }

    void HandleRunReset()
    {
        currentRun.pullsSinceEpic = 0;
        currentRun.pullsSinceLegendary = 0;
        currentRun.relics.Clear();
        currentRun.temporaryRewards.Clear();
        currentRun.physicalItems.Clear();

        OnStateChanged?.Invoke();
    }
}