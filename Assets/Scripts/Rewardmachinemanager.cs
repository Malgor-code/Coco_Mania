using System;
using System.Collections.Generic;
using UnityEngine;

public class RewardMachineManager : MonoBehaviour
{
    public static RewardMachineManager Instance;

    [Header("Base de datos de recompensas")]
    public RewardDatabase database;

    [Header("Costo de tirada")]
    public int pullCost = 100;

    [Header("Ranuras activas (cuantos objetos puedes tener activos a la vez)")]
    public int slotCount = 3;

    [Header("Probabilidades base (deberian sumar 100)")]
    [Range(0f, 100f)] public float commonChance = 55f;
    [Range(0f, 100f)] public float uncommonChance = 28f;
    [Range(0f, 100f)] public float rareChance = 12f;
    [Range(0f, 100f)] public float epicChance = 4f;
    [Range(0f, 100f)] public float legendaryChance = 1f;

    [Header("Pity / garantia")]
    public int pityEpicThreshold = 20;
    public int pityLegendaryThreshold = 50;

    [Header("Datos en tiempo de ejecucion (visibles para depurar)")]
    public CurrentRunData currentRun = new CurrentRunData();
    public PersistentData persistent = new PersistentData();
    public event Action<RewardData, bool> OnPullResult;
    public event Action OnStateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureSlots();
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

    void EnsureSlots()
    {
        slotCount = Mathf.Max(1, slotCount);
        while (currentRun.slots.Count < slotCount) currentRun.slots.Add(new EquippedSlot());
    }

    public int GetBagCount(string rewardId)
    {
        foreach (var e in currentRun.bag)
            if (e.rewardId == rewardId) return e.count;
        return 0;
    }

    public bool IsEquipped(string rewardId)
    {
        foreach (var s in currentRun.slots)
            if (!s.IsEmpty && s.rewardId == rewardId) return true;
        return false;
    }
    public bool IsRelicOwned(string rewardId)
    {
        return GetBagCount(rewardId) > 0 || IsEquipped(rewardId);
    }

    public bool HasFreeSlot()
    {
        foreach (var s in currentRun.slots)
            if (s.IsEmpty) return true;
        return false;
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
        if (wasDuplicate) GameManager.Instance.coconutCoins += pullCost;

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
            pool.RemoveAll(x => x.category != RewardCategory.Temporary && x.category != RewardCategory.PermanentRelic);
            if (pool.Count > 0) return pool[UnityEngine.Random.Range(0, pool.Count)];
        }
        return null;
    }
    bool GrantReward(RewardData data)
    {
        bool isTemporary = data.category == RewardCategory.Temporary;

        if (!isTemporary && IsRelicOwned(data.id)) return true;

        persistent.unlockedRewardIds.Add(data.id);
        AddToBag(data.id);
        return false;
    }

    void AddToBag(string rewardId)
    {
        foreach (var e in currentRun.bag)
        {
            if (e.rewardId == rewardId) { e.count++; return; }
        }
        currentRun.bag.Add(new BagEntry { rewardId = rewardId, count = 1 });
    }

    void RemoveOneFromBag(string rewardId)
    {
        for (int i = 0; i < currentRun.bag.Count; i++)
        {
            var e = currentRun.bag[i];
            if (e.rewardId != rewardId) continue;
            e.count--;
            if (e.count <= 0) currentRun.bag.RemoveAt(i);
            return;
        }
    }

    public bool Equip(string rewardId)
    {
        var data = database != null ? database.GetById(rewardId) : null;
        if (data == null || GetBagCount(rewardId) <= 0) return false;

        EnsureSlots();
        int free = currentRun.slots.FindIndex(s => s.IsEmpty);
        if (free < 0) return false;

        RemoveOneFromBag(rewardId);

        var slot = currentRun.slots[free];
        slot.rewardId = data.id;
        slot.effect = data.effect;
        slot.value = data.value;
        slot.isTemporary = data.category == RewardCategory.Temporary;
        slot.daysRemaining = slot.isTemporary ? Mathf.Max(1, data.durationDays) : 0;

        ApplyStatEffect(slot.effect, slot.value, 1);
        OnStateChanged?.Invoke();
        return true;
    }
    public bool CanUnequip(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentRun.slots.Count) return false;
        var slot = currentRun.slots[slotIndex];
        return !slot.IsEmpty && !slot.isTemporary;
    }

    public bool Unequip(int slotIndex)
    {
        if (!CanUnequip(slotIndex)) return false;

        var slot = currentRun.slots[slotIndex];
        ApplyStatEffect(slot.effect, slot.value, -1);
        AddToBag(slot.rewardId);
        slot.Clear();

        OnStateChanged?.Invoke();
        return true;
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

    void HandleDayStarted()
    {
        foreach (var slot in currentRun.slots)
        {
            if (slot.IsEmpty || !slot.isTemporary) continue;

            slot.daysRemaining--;
            if (slot.daysRemaining <= 0)
            {
                ApplyStatEffect(slot.effect, slot.value, -1);
                slot.Clear();
            }
        }

        OnStateChanged?.Invoke();
    }

    void HandleRunReset()
    {
        currentRun.pullsSinceEpic = 0;
        currentRun.pullsSinceLegendary = 0;
        currentRun.bag.Clear();
        foreach (var slot in currentRun.slots) slot.Clear();

        OnStateChanged?.Invoke();
    }
}