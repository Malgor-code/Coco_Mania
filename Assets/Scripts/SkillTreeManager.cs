using System.Collections.Generic;
using UnityEngine;
using static MacheteData;
using SS = StatSource;
using ST = StatType;
        
public class SkillTreeManager : MonoBehaviour
{
    static SkillTreeManager instance;
    public static SkillTreeManager Instance
    {
        get
        {
            if (instance == null) instance = ManagerBootstrap.Ensure<SkillTreeManager>();
            return instance;
        }
    }

    public List<SkillNode> Nodes => GameData.Skills;
    public HashSet<string> Unlocked { get; } = new HashSet<string>();

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public SkillNode GetNode(string id) => Nodes.Find(n => n.id == id);
    public bool IsUnlocked(string id) => Unlocked.Contains(id);

    public void ResetRun() => Unlocked.Clear();

    public bool CanUnlock(string id)
    {
        SkillNode node = GetNode(id);
        return node != null && CanUnlock(node);
    }

    bool CanUnlock(SkillNode node)
    {
        if (Unlocked.Contains(node.id)) return false;
        if (!ResourcesManager.Instance.CanAffordWater(node.cost)) return false;

        if (node.prerequisiteIds != null)
        {
            foreach (var pre in node.prerequisiteIds)
            {
                string trimmed = string.IsNullOrEmpty(pre) ? pre : pre.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !Unlocked.Contains(trimmed)) return false;
            }
        }
        return true;
    }

    public bool CanAffordAny()
    {
        List<SkillNode> nodes = Nodes;
        if (nodes == null) return false;

        foreach (var node in nodes)
        {
            if (Unlocked.Contains(node.id)) continue;
            if (node.cost > ResourcesManager.Instance.Water) continue;

            bool prereqsOk = true;
            if (node.prerequisiteIds != null)
            {
                foreach (var p in node.prerequisiteIds)
                {
                    if (!Unlocked.Contains(p)) { prereqsOk = false; break; }
                }
            }
            if (prereqsOk) return true;
        }
        return false;
    }

    public bool TryUnlock(string id, out SkillNode node)
    {
        node = GetNode(id);
        if (node == null || !CanUnlock(node)) return false;

        ResourcesManager.Instance.TrySpendWater(node.cost);
        Unlocked.Add(id);
        ApplyEffect(node);
        EventBus.Publish(new SkillUnlocked(id));
        return true;
    }

    void ApplyEffect(SkillNode node)
    {
        StatsManager st = StatsManager.Instance;
        float v = node.effectValue;
        int n = Mathf.RoundToInt(v);

        switch (node.effect)
        {
            case SkillEffect.DamageFlatBonus: st.Add(SS.Skill, ST.Damage, v); break;
            case SkillEffect.StaminaMaxFlatBonus: st.Add(SS.Skill, ST.StaminaMax, v); break;
            case SkillEffect.SwingIntervalReduction: st.Add(SS.Skill, ST.SwingReduction, v); break;
            case SkillEffect.HitRadiusBonus: st.Add(SS.Skill, ST.HitRadius, v); break;
            case SkillEffect.MoneyMultiplierBonus: st.Add(SS.Skill, ST.WaterMult, v); break;
            case SkillEffect.WaterMultiplierBonus: st.Add(SS.Skill, ST.WaterMult, v); break;
            case SkillEffect.ExtraStartingCoconut:
                if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += n;
                break;
            case SkillEffect.SpawnIntervalReduction:
                if (CoconutSpawner.Instance != null)
                    CoconutSpawner.Instance.spawnInterval = Mathf.Max(0.5f, CoconutSpawner.Instance.spawnInterval - v);
                break;
            case SkillEffect.CoinChanceBonus: st.coinDropChance += v; break;
            case SkillEffect.CritChanceBonus: st.Add(SS.Skill, ST.CritChance, v); break;
            case SkillEffect.JackpotChanceBonus: st.Add(SS.Skill, ST.JackpotChance, v); break;
            case SkillEffect.JackpotWaterBonus: st.Add(SS.Skill, ST.JackpotWater, v); break;
            case SkillEffect.CritDamageBonus: st.Add(SS.Skill, ST.CritDamage, v); break;
            case SkillEffect.ExtraCoconutChanceBonus:
                st.Set(SS.Skill, ST.ExtraCoconutChance, Mathf.Min(0.5f, st.GetSource(SS.Skill, ST.ExtraCoconutChance) + v));
                break;
            case SkillEffect.StreakForgiveness:
                st.Add(SS.Skill, ST.StreakForgives, n);
                CombatManager.Instance.StreakForgivesLeft += n;
                break;
            case SkillEffect.OrderDiscount: st.Add(SS.Skill, ST.OrderDiscount, v); break;
            case SkillEffect.LastBreathBonus: st.Add(SS.Skill, ST.LastBreathWater, v); break;
            case SkillEffect.ChainSplash: st.Add(SS.Skill, ST.ChainSplash, v); break;
            case SkillEffect.LastBreathSwingBonus: st.Add(SS.Skill, ST.LastBreathSwing, v); break;
            case SkillEffect.ExecuteDamageBonus: st.Add(SS.Skill, ST.ExecuteDamage, v); break;
            case SkillEffect.OrderRefundBonus: st.Add(SS.Skill, ST.OrderRefund, v); break;
            case SkillEffect.EarlyDayWaterBonus: st.Add(SS.Skill, ST.EarlyDayWater, v); break;
            case SkillEffect.MultiHitDamageBonus: st.Add(SS.Skill, ST.MultiHitDamage, v); break;
            case SkillEffect.StreakMaxStacksBonus: st.Add(SS.Skill, ST.StreakStacks, n); break;
            case SkillEffect.StreakPowerBonus: st.Add(SS.Skill, ST.StreakPower, v); break;
            case SkillEffect.StaminaRecoveryChanceBonus:
                st.Set(SS.Skill, ST.StaminaRecoveryChance, Mathf.Min(0.35f, st.GetSource(SS.Skill, ST.StaminaRecoveryChance) + v));
                st.Add(SS.Skill, ST.StaminaRecoveryLevel, 1f);
                break;
            case SkillEffect.StaminaRecoveryAmountBonus: st.Add(SS.Skill, ST.StaminaRecoveryAmount, v); break;
        }

        MacheteShopManager.Instance.RecalculateStats();
    }
}