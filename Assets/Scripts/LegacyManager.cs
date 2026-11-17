using System.Collections.Generic;
using UnityEngine;
using static MacheteData;
using SS = StatSource;
using ST = StatType;

public class LegacyManager : MonoBehaviour
{
    static LegacyManager instance;
    public static LegacyManager Instance
    {
        get
        {
            if (instance == null) instance = ManagerBootstrap.Ensure<LegacyManager>();
            return instance;
        }
    }

    [Header("Dificultad por Legado (los cocos escalan segun cuanto legado llevas)")]
    [Tooltip("Cuanto HP extra por 'paso' de legado. Sube = mas dificil.")]
    public float legacyHpPerStep = 0.03f;
    [Tooltip("Exponente de la curva. Menor a 1 = crece cada vez mas despacio.")]
    public float legacyHpExponent = 0.7f;
    [Tooltip("Multiplicador maximo de HP por legado.")]
    public float legacyHpCap = 3f;
    [Tooltip("Ciclos que tarda en aplicarse al 100% (arranca en el ciclo 1).")]
    public int legacyHpRampCycles = 6;
    [Range(0f, 1f)]
    [Tooltip("Que parte del HP extra se devuelve como agua extra (0 = nada, 1 = proporcional).")]
    public float legacyLootCompensation = 0.4f;

    public int Points { get; set; }
    public int TotalEarned { get; set; }
    public HashSet<string> Purchased { get; } = new HashSet<string>();
    public List<LegacyItem> Items => GameData.LegacyItems;

    public enum BuyResult { Ok, NotFound, AlreadyOwned, NotEnoughPoints, NeedsPrevious }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public LegacyItem GetItem(string itemName) => Items.Find(i => i.itemName == itemName);
    public bool IsPurchased(string itemName) => Purchased.Contains(itemName);

    string PreviousTier(string n)
    {
        string basePart = null;
        if (n.EndsWith(" IV")) basePart = n.Substring(0, n.Length - 3) + " III";
        else if (n.EndsWith(" III")) basePart = n.Substring(0, n.Length - 4) + " II";
        else if (n.EndsWith(" II")) basePart = n.Substring(0, n.Length - 3);
        if (basePart == null) return null;
        if (GetItem(basePart) == null && GetItem(basePart + " I") != null) return basePart + " I";
        return basePart;
    }

    public BuyResult TryBuy(string itemName, out LegacyItem item, out string missingPrevious)
    {
        missingPrevious = null;
        item = GetItem(itemName);
        if (item == null) return BuyResult.NotFound;
        if (Purchased.Contains(itemName)) return BuyResult.AlreadyOwned;
        if (Points < item.cost) return BuyResult.NotEnoughPoints;

        string prev = PreviousTier(itemName);
        if (prev != null && !Purchased.Contains(prev)) { missingPrevious = prev; return BuyResult.NeedsPrevious; }

        Points -= item.cost;
        Purchased.Add(itemName);
        ApplyEffect(item);
        EventBus.Publish(new LegacyPurchased(itemName));
        return BuyResult.Ok;
    }

    public void ApplyEffect(LegacyItem item)
    {
        StatsManager st = StatsManager.Instance;
        float v = item.value;
        int n = Mathf.RoundToInt(v);

        switch (item.effect)
        {
            case LegacyEffect.PermanentDamageBonus: st.Add(SS.Legacy, ST.Damage, v); break;
            case LegacyEffect.PermanentStaminaMaxBonus: st.Add(SS.Legacy, ST.StaminaMax, v); break;
            case LegacyEffect.PermanentMoneyMultiplierBonus: st.Add(SS.Legacy, ST.WaterMult, v); break;
            case LegacyEffect.PermanentCritChanceBonus: st.Add(SS.Legacy, ST.CritChance, v); break;
            case LegacyEffect.PermanentStartingCoconutBonus:
                st.Add(SS.Legacy, ST.StartingCoconuts, n);
                if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += n;
                break;
            case LegacyEffect.PermanentLegacyMultiplierBonus:
                st.Add(SS.Legacy, ST.LegacyMultiplierBonus, v);
                RecalcMultiplier();
                break;
            case LegacyEffect.PermanentCoinChanceBonus:
                st.Add(SS.Legacy, ST.CoinChance, v);
                st.coinDropChance += v;
                break;
            case LegacyEffect.PermanentExtraDay: st.Add(SS.Legacy, ST.ExtraDays, n); break;
            case LegacyEffect.PermanentStartingWater: st.Add(SS.Legacy, ST.StartingWater, v); break;
            case LegacyEffect.PermanentJackpotChance: st.Add(SS.Legacy, ST.JackpotChance, v); break;
            case LegacyEffect.PermanentStreakStacks: st.Add(SS.Legacy, ST.StreakStacks, n); break;
            case LegacyEffect.PermanentHitRadius: st.Add(SS.Legacy, ST.HitRadius, v); break;
            case LegacyEffect.PermanentOrderDiscount: st.Add(SS.Legacy, ST.OrderDiscount, v); break;
            case LegacyEffect.PermanentStartingMachete:
                st.Add(SS.Legacy, ST.StartingMachete, n);
                MacheteShopManager shop = MacheteShopManager.Instance;
                shop.EquippedIndex = Mathf.Max(shop.EquippedIndex, Mathf.RoundToInt(st.GetSource(SS.Legacy, ST.StartingMachete)));
                break;
        }

        MacheteShopManager.Instance.RecalculateStats();
    }

    public float GetHpScale(int billCycle)
    {
        float raw = 1f + legacyHpPerStep * Mathf.Pow(TotalEarned, legacyHpExponent);
        raw = Mathf.Min(raw, legacyHpCap);
        float ramp = Mathf.Clamp01((billCycle - 1f) / Mathf.Max(1, legacyHpRampCycles));
        return Mathf.Lerp(1f, raw, ramp);
    }

    public float GetLootScale(int billCycle) => 1f + (GetHpScale(billCycle) - 1f) * legacyLootCompensation;

    public void RecalcMultiplier()
    {
        StatsManager st = StatsManager.Instance;
        st.legacyMultiplier = 1f + TotalEarned * 0.1f * (1f + st.Get(ST.LegacyMultiplierBonus));
    }
}