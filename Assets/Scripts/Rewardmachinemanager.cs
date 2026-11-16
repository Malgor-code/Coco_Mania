using System;
using System.Collections.Generic;
using UnityEngine;

public enum PullTier { Normal, Boosted, Premium }

public class RewardMachineManager : MonoBehaviour
{
    public static RewardMachineManager Instance;

    [Header("Base de datos de recompensas")]
    public RewardDatabase database;

    [Header("Ranuras activas (cuantos objetos puedes tener activos a la vez)")]
    public int slotCount = 3;
    [Header("Tirada NORMAL")]
    public int pullCost = 1;
    [Tooltip("Probabilidades base (deberian sumar 100).")]
    [Range(0f, 100f)] public float commonChance = 60f;
    [Range(0f, 100f)] public float uncommonChance = 25f;
    [Range(0f, 100f)] public float rareChance = 10f;
    [Range(0f, 100f)] public float epicChance = 4f;
    [Range(0f, 100f)] public float legendaryChance = 1f;

    [Header("Tirada MEJORADA (Raro / Epico / Legendario)")]
    public int boostedCost = 5;
    [Range(0f, 100f)] public float boostedRare = 55f;
    [Range(0f, 100f)] public float boostedEpic = 35f;
    [Range(0f, 100f)] public float boostedLegendary = 10f;

    [Header("Tirada PREMIUM (solo Epico / Legendario)")]
    public int premiumCost = 15;
    [Range(0f, 100f)] public float premiumEpic = 70f;
    [Range(0f, 100f)] public float premiumLegendary = 30f;

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

    public int GetCost(PullTier tier)
    {
        switch (tier)
        {
            case PullTier.Boosted: return boostedCost;
            case PullTier.Premium: return premiumCost;
            default: return pullCost;
        }
    }
    public float[] GetOdds(PullTier tier)
    {
        switch (tier)
        {
            case PullTier.Boosted: return new float[] { 0f, 0f, boostedRare, boostedEpic, boostedLegendary };
            case PullTier.Premium: return new float[] { 0f, 0f, 0f, premiumEpic, premiumLegendary };
            default: return new float[] { commonChance, uncommonChance, rareChance, epicChance, legendaryChance };
        }
    }

    RewardRarity MinRarity(PullTier tier)
    {
        switch (tier)
        {
            case PullTier.Boosted: return RewardRarity.Rare;
            case PullTier.Premium: return RewardRarity.Epic;
            default: return RewardRarity.Common;
        }
    }

    public bool CanPull(PullTier tier = PullTier.Normal)
    {
        return GameManager.Instance != null && database != null && GameManager.Instance.coconutCoins >= GetCost(tier);
    }
    public RewardData Pull(PullTier tier = PullTier.Normal)
    {
        if (!CanPull(tier))
        {
            OnPullResult?.Invoke(null, false);
            return null;
        }

        int cost = GetCost(tier);
        GameManager.Instance.coconutCoins -= cost;

        RewardRarity rarity = RollRarity(GetOdds(tier));
        RewardData data = PickRewardOfRarity(rarity, MinRarity(tier));

        if (data == null)
        {
            Debug.LogWarning($"[RewardMachineManager] No hay ninguna RewardData de rareza {rarity} (ni cercana) en la base de datos. Se devolvieron las monedas.");
            GameManager.Instance.coconutCoins += cost;
            OnStateChanged?.Invoke();
            OnPullResult?.Invoke(null, false);
            return null;
        }

        bool wasDuplicate = GrantReward(data);
        if (wasDuplicate) GameManager.Instance.coconutCoins += cost;

        OnPullResult?.Invoke(data, wasDuplicate);
        OnStateChanged?.Invoke();
        return data;
    }

    static RewardRarity RollRarity(float[] odds)
    {
        float total = 0f;
        for (int i = 0; i < odds.Length; i++) total += odds[i];
        if (total <= 0f) return RewardRarity.Common;

        float roll = UnityEngine.Random.value * total;
        for (int i = 0; i < odds.Length; i++)
        {
            if ((roll -= odds[i]) < 0f) return (RewardRarity)i;
        }
        return RewardRarity.Legendary;
    }

    RewardData PickRewardOfRarity(RewardRarity rarity, RewardRarity floor)
    {
        for (int r = (int)rarity; r >= (int)floor; r--)
        {
            var data = PickFromPool((RewardRarity)r);
            if (data != null) return data;
        }
        for (int r = (int)rarity + 1; r <= (int)RewardRarity.Legendary; r++)
        {
            var data = PickFromPool((RewardRarity)r);
            if (data != null) return data;
        }
        return null;
    }

    RewardData PickFromPool(RewardRarity rarity)
    {
        var pool = database.GetByRarity(rarity);
        pool.RemoveAll(x => x.category != RewardCategory.Temporary && x.category != RewardCategory.PermanentRelic);
        return pool.Count > 0 ? pool[UnityEngine.Random.Range(0, pool.Count)] : null;
    }

    bool GrantReward(RewardData data)
    {
        if (data.IsWorldObject)
        {
            if (LegendaryWorldSpawner.Instance == null)
            {
                Debug.LogWarning("[RewardMachineManager] Falta LegendaryWorldSpawner en la escena. Se devuelve la moneda.");
                return true;
            }
            persistent.unlockedRewardIds.Add(data.id);
            return !LegendaryWorldSpawner.Instance.Spawn(data);
        }
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
        currentRun.bag.Clear();
        foreach (var slot in currentRun.slots) slot.Clear();
        if (LegendaryWorldSpawner.Instance != null) LegendaryWorldSpawner.Instance.ClearAll();
        OnStateChanged?.Invoke();
    }

    [ContextMenu("Balance: comparar tiradas especiales vs normales")]
    void LogBalance()
    {
        float[] n = GetOdds(PullTier.Normal);
        float nTotal = 0f; foreach (float f in n) nTotal += f;
        if (nTotal <= 0f || pullCost <= 0) { Debug.Log("[Balance] Revisa las probabilidades/costo normales."); return; }

        float nRareUp = (n[2] + n[3] + n[4]) / nTotal;
        float nEpicUp = (n[3] + n[4]) / nTotal;
        float nLeg = n[4] / nTotal;

        LogTier(PullTier.Boosted, boostedCost, nRareUp, nEpicUp, nLeg);
        LogTier(PullTier.Premium, premiumCost, nRareUp, nEpicUp, nLeg);
    }

    void LogTier(PullTier tier, int cost, float nRareUp, float nEpicUp, float nLeg)
    {
        float[] o = GetOdds(tier);
        float total = 0f; foreach (float f in o) total += f;
        if (total <= 0f) return;

        int pulls = Mathf.Max(1, cost / Mathf.Max(1, pullCost));
        float tRareUp = (o[2] + o[3] + o[4]) / total;
        float tEpicUp = (o[3] + o[4]) / total;
        float tLeg = o[4] / total;

        Debug.Log($"[Balance] {tier} ({cost} monedas) vs {pulls} tiradas normales:\n" +
                  $"  Raro o mejor  -> 1 tirada {tRareUp:P0} | {pulls} normales (al menos una) {1f - Mathf.Pow(1f - nRareUp, pulls):P0}\n" +
                  $"  Epico o mejor -> 1 tirada {tEpicUp:P0} | {pulls} normales (al menos una) {1f - Mathf.Pow(1f - nEpicUp, pulls):P0}\n" +
                  $"  Legendario    -> 1 tirada {tLeg:P0} | {pulls} normales (al menos una) {1f - Mathf.Pow(1f - nLeg, pulls):P0}");
    }
}