using System.Collections.Generic;
using UnityEngine;
using static MacheteData;
using SS = StatSource;
using ST = StatType;

public class PerkManager : MonoBehaviour
{
    static PerkManager instance;
    public static PerkManager Instance
    {
        get
        {
            if (instance == null) instance = ManagerBootstrap.Ensure<PerkManager>();
            return instance;
        }
    }

    [Tooltip("Cuantas veces puede salir el mismo perk en una partida.")]
    public int maxCopiesPerPerk = 3;

    public List<PerkOption> Pool => GameData.Perks;
    public Dictionary<string, int> Copies { get; } = new Dictionary<string, int>();

    static readonly Dictionary<string, int> PerkMinCycle = new Dictionary<string, int>
    {
        { "Madrugador", 2 }, { "Buen Regateo", 3 }, { "Racha Viva", 3 },
        { "Pulso Firme", 4 }, { "Doble Golpe", 4 },
        { "Corazón de Jackpot", 5 }, { "Último Aliento", 5 }, { "Machete Pesado", 5 },
        { "Instinto Cazador", 6 }, { "Chispa en Cadena", 6 },
        { "Cosecha Abundante", 7 }, { "Manos de Hierro", 7 }, { "Pulmones de Acero", 7 },
        { "Pacto del Cobrador", 8 }, { "Ojo de Halcón", 8 },
        { "Racha Imparable", 10 },
        { "Coco Dorado", 11 }, { "Manos de Titán", 11 }, { "Jackpot Mayor", 11 }, { "Cazador Maestro", 11 },
    };

    static readonly Dictionary<string, int> PerkMaxCycle = new Dictionary<string, int>
    {
        { "Manos Firmes", 10 }, { "Segundo Aire", 10 }, { "Buen Trato", 10 },
        { "Golpe Certero", 10 }, { "Golpe de Suerte", 10 }, { "Cosecha Extra", 10 },
    };

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void ResetRun() => Copies.Clear();

    public void RegisterChosen(string perkName)
    {
        Copies[perkName] = Copies.ContainsKey(perkName) ? Copies[perkName] + 1 : 1;
    }

    public PerkOption[] PickRandom(int count, int billCycle)
    {
        bool streakUnlocked = SkillTreeManager.Instance.IsUnlocked("racha_1");
        float rawSwing = StatsManager.Instance.RawSwingInterval(false);

        List<PerkOption> pool = Pool.FindAll(p =>
        {
            int min = PerkMinCycle.TryGetValue(p.perkName, out int mn) ? mn : 1;
            int max = PerkMaxCycle.TryGetValue(p.perkName, out int mx) ? mx : int.MaxValue;
            bool cycleOk = billCycle >= min && billCycle < max;
            bool copiesOk = !Copies.ContainsKey(p.perkName) || Copies[p.perkName] < maxCopiesPerPerk;
            if (p.effect == PerkEffect.SwingIntervalReduction && rawSwing - p.value < StatsManager.SwingFloor) return false;
            if (!streakUnlocked && (p.effect == PerkEffect.StreakMaxStacksBonus || p.effect == PerkEffect.StreakPowerBonus)) return false;
            return cycleOk && copiesOk;
        });

        PerkOption[] result = new PerkOption[count];
        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) { result[i] = null; continue; }
            int idx = Random.Range(0, pool.Count);
            result[i] = pool[idx];
            pool.RemoveAt(idx);
        }
        return result;
    }

    public void Apply(PerkOption perk)
    {
        StatsManager st = StatsManager.Instance;
        float v = perk.value;
        int n = Mathf.RoundToInt(v);

        switch (perk.effect)
        {
            case PerkEffect.DamageBonus: st.Add(SS.Perk, ST.Damage, v); break;
            case PerkEffect.StaminaMaxBonus: st.Add(SS.Perk, ST.StaminaMax, v); break;
            case PerkEffect.MoneyMultiplierBonus: st.Add(SS.Perk, ST.WaterMult, v); break;
            case PerkEffect.SwingIntervalReduction: st.Add(SS.Perk, ST.SwingReduction, v); break;
            case PerkEffect.ExtraCoconutNextCycle:
                if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += n;
                break;
            case PerkEffect.CritChanceBonus: st.Add(SS.Perk, ST.CritChance, v); break;
            case PerkEffect.WaterMultiplierBonus: st.Add(SS.Perk, ST.WaterMult, v); break;
            case PerkEffect.WaterForSwingPenalty:
                st.perkWaterMultiplicative *= 1f + v;
                st.Add(SS.Perk, ST.SwingReduction, -v * 0.48f);
                break;
            case PerkEffect.HitRadiusBonus: st.Add(SS.Perk, ST.HitRadius, v); break;
            case PerkEffect.JackpotChanceBonus: st.Add(SS.Perk, ST.JackpotChance, v); break;
            case PerkEffect.StreakMaxStacksBonus: st.Add(SS.Perk, ST.StreakStacks, n); break;
            case PerkEffect.StreakPowerBonus: st.Add(SS.Perk, ST.StreakPower, v); break;
            case PerkEffect.OrderDiscount: st.Add(SS.Perk, ST.OrderDiscount, v); break;
            case PerkEffect.CoinChanceBonus: st.coinDropChance += v; break;
            case PerkEffect.ExecuteDamageBonus: st.Add(SS.Perk, ST.ExecuteDamage, v); break;
            case PerkEffect.ChainSplash: st.Add(SS.Perk, ST.ChainSplash, v); break;
            case PerkEffect.LastBreathBonus: st.Add(SS.Perk, ST.LastBreathWater, v); break;
            case PerkEffect.EarlyDayWaterBonus: st.Add(SS.Perk, ST.EarlyDayWater, v); break;
            case PerkEffect.MultiHitDamageBonus: st.Add(SS.Perk, ST.MultiHitDamage, v); break;
            case PerkEffect.DamageForStaminaPenalty:
                st.Add(SS.Perk, ST.Damage, v);
                st.Add(SS.Perk, ST.StaminaMax, -v * 0.5f);
                break;
        }

        MacheteShopManager.Instance.RecalculateStats();
    }
}