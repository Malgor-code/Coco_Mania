using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using static MacheteData;

public partial class GameManager
{

    public float GetCritChance() => Mathf.Clamp01(GetRawCritChance());
    float GetRawCritChance() => skillCritChanceBonus + perkCritChanceBonus + legacyCritChanceBonus + relicCritChanceBonus + PassiveValue(MachetePassive.CritChance);
    public float GetCritMultiplier() => 1.3f + skillCritDamageBonus + PassiveValue(MachetePassive.CritDamage) + Mathf.Max(0f, GetRawCritChance() - 1f);

    float PassiveValue(MachetePassive p)
    {
        MacheteData m = GetCurrentMachete();
        return (m != null && m.passive == p) ? m.passiveValue : 0f;
    }

    public void ApplyStaminaPenalty(float amount)
    {
        stamina = Mathf.Max(0f, stamina - amount);
    }

    public void RegisterSwingResult(bool hit)
    {
        daySwings++;
        if (hit)
        {
            dayHits++;
            currentHitStreak++;
        }
        else if (streakForgivesLeft > 0 && currentHitStreak >= streakThreshold)
        {
            streakForgivesLeft--;
        }
        else
        {
            currentHitStreak = 0;
        }
    }

    int CurrentStreakStacks()
    {
        if (!StreakUnlocked) return 0;
        int stacks = currentHitStreak - streakThreshold + 1;
        int max = Mathf.Min(StreakHardCap, skillStreakStackBonus + legacyStreakStacks);
        return Mathf.Clamp(stacks, 0, max);
    }

    public float GetStreakDamageMultiplier() =>
        1f + CurrentStreakStacks() * Mathf.Min(StreakPowerCap, streakDamageBonusPerHit + skillStreakPowerBonus);

    public float GetStreakWaterMultiplier() =>
        1f + CurrentStreakStacks() * Mathf.Min(StreakPowerCap, streakWaterBonusPerHit + skillStreakPowerBonus);

    public float GetLegacyHpScale()
    {
        float raw = 1f + legacyHpPerStep * Mathf.Pow(totalLegacyPointsEarned, legacyHpExponent);
        raw = Mathf.Min(raw, legacyHpCap);
        float ramp = Mathf.Clamp01((billCycle - 1f) / Mathf.Max(1, legacyHpRampCycles));
        return Mathf.Lerp(1f, raw, ramp);
    }

    public float GetLegacyLootScale() => 1f + (GetLegacyHpScale() - 1f) * legacyLootCompensation;

    void RecalcLegacyMultiplier()
    {
        legacyMultiplier = 1f + totalLegacyPointsEarned * 0.1f * (1f + legacyMultiplierBonus);
    }
    public void ApplyTemporaryCurse(float durationSeconds)
    {
        StartCoroutine(TemporaryCurseRoutine(durationSeconds));
    }

    private IEnumerator TemporaryCurseRoutine(float duration)
    {
        int gen = curseGeneration;
        int effect = Random.Range(0, 4);
        float amount = 0f;

        switch (effect)
        {
            case 0:
                amount = 0.15f; curseSwingPenalty += amount;
                RecalculateAllStats();
                Log("Maldicion: golpes mas lentos por un rato...");
                break;
            case 1:
                amount = 0.15f; curseWaterPenalty += amount;
                Log("Maldicion: menos agua por un rato...");
                break;
            case 2:
                amount = staminaMaxBase * 0.2f; curseStaminaPenalty += amount;
                Log("Maldicion: menos resistencia por un rato...");
                break;
            case 3:
                if (CoconutSpawner.Instance == null) yield break;
                amount = 1.5f; curseSpawnPenalty += amount;
                CoconutSpawner.Instance.spawnInterval += amount;
                Log("Maldicion: los cocos tardan mas en aparecer...");
                break;
        }

        yield return new WaitForSeconds(duration);

        if (gen != curseGeneration) yield break;

        switch (effect)
        {
            case 0: curseSwingPenalty -= amount; RecalculateAllStats(); break;
            case 1: curseWaterPenalty -= amount; break;
            case 2: curseStaminaPenalty -= amount; break;
            case 3:
                curseSpawnPenalty -= amount;
                if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.spawnInterval -= amount;
                break;
        }
    }

    void ClearCurse()
    {
        curseGeneration++;
        if (curseSpawnPenalty > 0f && CoconutSpawner.Instance != null)
            CoconutSpawner.Instance.spawnInterval -= curseSpawnPenalty;
        curseSwingPenalty = curseWaterPenalty = curseStaminaPenalty = curseSpawnPenalty = 0f;
    }
    void RecalculateAllStats()
    {
        MacheteData m = GetCurrentMachete();
        float baseDmg = m != null ? m.baseDamage : 3f;
        float baseSwing = m != null ? m.baseSwingInterval : 1.2f;
        float baseRadius = m != null ? m.baseHitRadius : .8f;
        int damage = Mathf.RoundToInt(baseDmg + skillDamageBonus + perkDamageBonus + legacyDamageBonus + relicDamageBonus);

        if (MacheteController.Instance != null)
        {
            MacheteController.Instance.damageOverride = damage;
            MacheteController.Instance.hitRadius = baseRadius + skillHitRadiusBonus + relicHitRadiusBonus + legacyHitRadiusBonus;
            MacheteController.Instance.swingInterval = Mathf.Max(SwingFloor, GetRawSwingInterval(true));
        }
    }

    public void RefreshCombatStats() => RecalculateAllStats();

    float GetRawSwingInterval(bool includeLastBreath)
    {
        MacheteData m = GetCurrentMachete();
        float baseSwing = m != null ? m.baseSwingInterval : 1.2f;
        return baseSwing - skillSwingIntervalReduction - perkSwingIntervalReduction - relicSwingIntervalReduction
             + curseSwingPenalty - (includeLastBreath && lastBreathSwingActive ? skillLastBreathSwingReduction : 0f);
    }

    public float GetStaminaMax()
    {
        return Mathf.Max(1f, staminaMaxBase + skillStaminaBonus + perkStaminaMaxBonus
            + legacyStaminaBonus + relicStaminaMaxBonus + PassiveValue(MachetePassive.StaminaMax) - curseStaminaPenalty);
    }

    float GetStaminaRecoveryCap()
    {
        if (skillStaminaRecoveryLevel <= 0) return 0f;
        return StaminaRecoveryCaps[Mathf.Min(skillStaminaRecoveryLevel, StaminaRecoveryCaps.Length) - 1];
    }

    [System.Obsolete("Renombrado conceptualmente: ahora devuelve el bonus de AGUA extra (no de dinero, que ya no existe). Se deja el nombre por compatibilidad con UI existente.")]
    public float GetSkillMoneyMultiplierBonusPercent()
    {
        return skillWaterMultiplierBonus * 100f;
    }

    public float GetSkillWaterMultiplierBonusPercent()
    {
        return skillWaterMultiplierBonus * 100f;
    }
    public float GetLastBreathMultiplier()
    {
        if (skillLastBreathBonus <= 0f || currentPhase != Phase.Hitting) return 1f;
        return stamina <= GetStaminaMax() * 0.2f ? 1f + skillLastBreathBonus : 1f;
    }

    public float GetExecuteDamageMultiplier(float targetHpPercent)
    {
        return (skillExecuteBonus > 0f && targetHpPercent < executeHpThreshold) ? 1f + skillExecuteBonus : 1f;
    }

    public float GetMultiHitMultiplier(int targetsHit)
    {
        float bonus = skillMultiHitBonus + PassiveValue(MachetePassive.MultiHit);
        return (bonus > 0f && targetsHit >= 2) ? 1f + bonus : 1f;
    }

    float GetEarlyDayMultiplier()
    {
        return (skillEarlyDayBonus > 0f && currentPhase == Phase.Hitting && dayElapsed < earlyDayDuration)
            ? 1f + skillEarlyDayBonus : 1f;
    }
    public int OnCoconutDestroyed(float lootMultiplier, out float waterGained)
    {
        totalCoconutsKilled++;
        runCoconutsKilled++;
        if (runCoconutsKilled > bestRunKills) bestRunKills = runCoconutsKilled;
        dayCoconutsKilled++;
        float streakBonus = GetStreakWaterMultiplier() - 1f;
        float earlyBonus = GetEarlyDayMultiplier() - 1f;
        float waterExtraMult = Mathf.Max(0.1f, 1f + skillWaterMultiplierBonus + perkWaterMultiplierBonus + legacyWaterMultiplierBonus
            + relicWaterMultiplierBonus + PassiveValue(MachetePassive.WaterBonus) - curseWaterPenalty + streakBonus + earlyBonus);
        waterGained = (baseWaterPerKill + Random.Range(0f, waterVariance)) * lootMultiplier * waterExtraMult
     * legacyMultiplier * GetLastBreathMultiplier() * perkWaterMultiplicative
     * GetLegacyLootScale();
        float jackpotChance = skillJackpotChance + legacyJackpotChance + PassiveValue(MachetePassive.JackpotChance);
        lastKillHadJackpot = jackpotChance > 0f && Random.value < jackpotChance;
        if (lastKillHadJackpot)
        {
            float jackpotWater = waterGained * (jackpotBaseMultiplier + skillJackpotMultBonus);
            waterGained += jackpotWater;
            Log("¡JACKPOT DE AGUA! +" + FormatWater(jackpotWater));
        }
        waterCurrentML += waterGained;
        int gainedRounded = Mathf.RoundToInt(waterGained);
        runMoneyEarned += gainedRounded;
        dayMoneyEarned += gainedRounded;
        float recoveryCap = GetStaminaRecoveryCap();
        if (skillStaminaRecoveryChance > 0f && dayStaminaRecovered < recoveryCap
            && Random.value < skillStaminaRecoveryChance)
        {
            float amount = Mathf.Min(StaminaRecoveryBase + skillStaminaRecoveryAmount, recoveryCap - dayStaminaRecovered);
            stamina = Mathf.Min(GetStaminaMax(), stamina + amount);
            dayStaminaRecovered += amount;
            Log("¡Recuperaste " + amount.ToString("0.#") + " de resistencia!");
        }

        bool foundCoin = Random.value < coinDropChance;
        lastKillFoundCoin = foundCoin;
        if (foundCoin)
        {
            coconutCoins++;
            dayCoinsFound++;
            Log(Loc("log_coin_found", coconutCoins));
        }
        else if (!lastKillHadJackpot)
        {
            Log(Loc("log_money_earned", FormatWater(waterGained)));
        }
        if (skillExtraCoconutChance > 0f && currentPhase == Phase.Hitting
            && Random.value < skillExtraCoconutChance && CoconutSpawner.Instance != null)
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
        float pct = Mathf.Min(0.5f, skillChainSplashPercent + PassiveValue(MachetePassive.ChainSplash));
        if (pct <= 0f || isApplyingSplash) return;
        isApplyingSplash = true;
        int splashDamage = Mathf.Max(1, Mathf.RoundToInt(killingDamage * pct));
        float radius = MacheteController.Instance != null ? MacheteController.Instance.hitRadius : 1.5f;
        var already = new HashSet<CoconutTarget>();
        foreach (var col in Physics.OverlapSphere(position, radius))
        {
            var target = col.GetComponentInParent<CoconutTarget>();
            if (target == null || target.gameObject == source || !already.Add(target)) continue;
            target.TakeDamage(splashDamage);
        }
        isApplyingSplash = false;
    }
}