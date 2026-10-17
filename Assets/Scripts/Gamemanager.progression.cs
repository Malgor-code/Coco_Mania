using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using static MacheteData;

public partial class GameManager
{
    public MacheteData GetMacheteData(int index)
    {
        if (index < 0 || index >= macheteOptions.Count) return null;
        return macheteOptions[index];
    }

    public MacheteData GetCurrentMachete() => GetMacheteData(equippedMacheteIndex);
    public MacheteData GetNextMachete() => GetMacheteData(equippedMacheteIndex + 1);

    public void BuyNextMachete()
    {
        MacheteData next = GetNextMachete();
        if (next == null) return;

        if (waterCurrentML < next.unlockCost) return;
        waterCurrentML -= next.unlockCost;
        equippedMacheteIndex++;
        BetaLog("machete_" + equippedMacheteIndex);
        RecalculateAllStats();
        Log(Loc("log_new_machete", next.macheteName));
        RefreshAllUI();
    }

    // ---------------- Arbol de mejoras ----------------

    public SkillNode GetSkillNode(string id) => skillTree.Find(n => n.id == id);

    public bool IsSkillUnlocked(string id) => unlockedSkillIds.Contains(id);

    public bool CanUnlockSkillPublic(string id)
    {
        SkillNode node = GetSkillNode(id);
        return node != null && CanUnlockSkill(node);
    }

    bool CanUnlockSkill(SkillNode node)
    {
        if (unlockedSkillIds.Contains(node.id)) return false;
        if (waterCurrentML < node.cost) return false;

        if (node.prerequisiteIds != null)
        {
            foreach (var pre in node.prerequisiteIds)
            {
                string trimmedPre = string.IsNullOrEmpty(pre) ? pre : pre.Trim();
                if (!string.IsNullOrEmpty(trimmedPre) && !unlockedSkillIds.Contains(trimmedPre)) return false;
            }
        }
        return true;
    }

    public void UnlockSkill(string id)
    {
        SkillNode node = GetSkillNode(id);
        if (node == null || !CanUnlockSkill(node)) return;

        waterCurrentML -= node.cost;
        unlockedSkillIds.Add(id);
        ApplySkillEffect(node);
        BetaLog("skill_" + id);
        Log(Loc("log_upgrade_bought", node.nodeName));
        RefreshAllUI();
    }

    void ApplySkillEffect(SkillNode node)
    {
        switch (node.effect)
        {
            case SkillEffect.DamageFlatBonus: skillDamageBonus += node.effectValue; break;
            case SkillEffect.StaminaMaxFlatBonus: skillStaminaBonus += node.effectValue; break;
            case SkillEffect.SwingIntervalReduction: skillSwingIntervalReduction += node.effectValue; break;
            case SkillEffect.HitRadiusBonus: skillHitRadiusBonus += node.effectValue; break;
            case SkillEffect.MoneyMultiplierBonus: skillWaterMultiplierBonus += node.effectValue; break;
            case SkillEffect.WaterMultiplierBonus: skillWaterMultiplierBonus += node.effectValue; break;
            case SkillEffect.ExtraStartingCoconut:
                if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += Mathf.RoundToInt(node.effectValue);
                break;
            case SkillEffect.SpawnIntervalReduction:
                if (CoconutSpawner.Instance != null)
                    CoconutSpawner.Instance.spawnInterval = Mathf.Max(0.5f, CoconutSpawner.Instance.spawnInterval - node.effectValue); break;
            case SkillEffect.CoinChanceBonus: coinDropChance += node.effectValue; break;
            case SkillEffect.CritChanceBonus: skillCritChanceBonus += node.effectValue; break;
            case SkillEffect.JackpotChanceBonus: skillJackpotChance += node.effectValue; break;
            case SkillEffect.JackpotWaterBonus: skillJackpotMultBonus += node.effectValue; break;
            case SkillEffect.CritDamageBonus: skillCritDamageBonus += node.effectValue; break;
            case SkillEffect.ExtraCoconutChanceBonus: skillExtraCoconutChance = Mathf.Min(0.5f, skillExtraCoconutChance + node.effectValue); break;
            case SkillEffect.StreakForgiveness: skillStreakForgives += Mathf.RoundToInt(node.effectValue); streakForgivesLeft += Mathf.RoundToInt(node.effectValue); break;
            case SkillEffect.OrderDiscount: waterTargetML *= 1f - node.effectValue; break;
            case SkillEffect.LastBreathBonus: skillLastBreathBonus += node.effectValue; break;
            case SkillEffect.ChainSplash: skillChainSplashPercent += node.effectValue; break;
            case SkillEffect.LastBreathSwingBonus: skillLastBreathSwingReduction += node.effectValue; break;
            case SkillEffect.ExecuteDamageBonus: skillExecuteBonus += node.effectValue; break;
            case SkillEffect.OrderRefundBonus: skillOrderRefund += node.effectValue; break;
            case SkillEffect.EarlyDayWaterBonus: skillEarlyDayBonus += node.effectValue; break;
            case SkillEffect.MultiHitDamageBonus: skillMultiHitBonus += node.effectValue; break;
            case SkillEffect.StreakMaxStacksBonus: skillStreakStackBonus += Mathf.RoundToInt(node.effectValue); break;
            case SkillEffect.StreakPowerBonus: skillStreakPowerBonus += node.effectValue; break;
            case SkillEffect.StaminaRecoveryChanceBonus: skillStaminaRecoveryChance = Mathf.Min(0.35f, skillStaminaRecoveryChance + node.effectValue); skillStaminaRecoveryLevel++; break;
            case SkillEffect.StaminaRecoveryAmountBonus: skillStaminaRecoveryAmount += node.effectValue; break;
        }
        RecalculateAllStats();
    }
    void OfferPerkChoice()
    {
        currentPhase = Phase.PerkChoice;
        isChoosingPerk = false;
        if (machete != null) machete.SetActive(false);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(true);

        SetCursorVisible(true);

        currentPerkChoices = PickRandomPerks(3);
        if (currentPerkChoices[0] == null)
        {
            pendingPerkOffers = 0;
            if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);
            ShowRecaudacionAfterPerk();
            return;
        }
        SetPerkLabel(perkOption1Name, perkOption1Desc, perkOption1Icon, currentPerkChoices[0]);
        SetPerkLabel(perkOption2Name, perkOption2Desc, perkOption2Icon, currentPerkChoices[1]);
        SetPerkLabel(perkOption3Name, perkOption3Desc, perkOption3Icon, currentPerkChoices[2]);
        if (perkCard1 != null) perkCard1.gameObject.SetActive(currentPerkChoices[0] != null);
        if (perkCard2 != null) perkCard2.gameObject.SetActive(currentPerkChoices[1] != null);
        if (perkCard3 != null) perkCard3.gameObject.SetActive(currentPerkChoices[2] != null);
        if (perkCard1 != null) perkCard1.PlayAppear(0f);
        if (perkCard2 != null) perkCard2.PlayAppear(perkCardStagger);
        if (perkCard3 != null) perkCard3.PlayAppear(perkCardStagger * 2f);
    }

    void ShowRecaudacionAfterPerk()
    {
        skipNextDayDecrement = true;
        currentPhase = Phase.Shop;
        if (machete != null) machete.SetActive(false);
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);
        if (betweenRunsPanel != null) betweenRunsPanel.SetActive(false);
        if (upgradeTreePanel != null) upgradeTreePanel.SetActive(false);
        if (tiendaPanel != null) tiendaPanel.SetActive(false);
        if (rewardMachinePanel != null) rewardMachinePanel.SetActive(false);
        if (deudaPanel != null) deudaPanel.SetActive(false);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(true);

        SetCursorVisible(true);
        RefreshAllUI();
        OnRecaudacionShown?.Invoke();
        AnnounceNewUnlocks();
        SaveGame();
    }

    void SetPerkLabel(TMP_Text nameLabel, TMP_Text descLabel, Image iconImage, PerkOption perk)
    {
        if (perk == null) return;
        if (nameLabel != null) nameLabel.text = perk.perkName;
        if (descLabel != null) descLabel.text = perk.description;
        if (iconImage != null)
        {
            Sprite s = GetPerkIcon(perk);
            iconImage.sprite = s;
            iconImage.enabled = s != null;
            iconImage.preserveAspect = true;
        }
    }

    PerkOption[] PickRandomPerks(int count)
    {
        List<PerkOption> pool = perkPool.FindAll(p =>
        {
            int min = PerkMinCycle.TryGetValue(p.perkName, out int mn) ? mn : 1;
            int max = PerkMaxCycle.TryGetValue(p.perkName, out int mx) ? mx : int.MaxValue;
            bool cycleOk = billCycle >= min && billCycle < max;
            bool copiesOk = !perkCopies.ContainsKey(p.perkName) || perkCopies[p.perkName] < maxCopiesPerPerk;
            if (p.effect == PerkEffect.SwingIntervalReduction && GetRawSwingInterval(false) - p.value < SwingFloor) return false;
            if (!StreakUnlocked && (p.effect == PerkEffect.StreakMaxStacksBonus || p.effect == PerkEffect.StreakPowerBonus)) return false;
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

    public void ChoosePerk(int index)
    {
        if (isChoosingPerk) return;
        if (index < 0 || index >= currentPerkChoices.Length || currentPerkChoices[index] == null) return;
        StartCoroutine(ChoosePerkRoutine(index));
    }

    IEnumerator ChoosePerkRoutine(int index)
    {
        isChoosingPerk = true;

        PerkCardFX[] cards = { perkCard1, perkCard2, perkCard3 };
        float wait = 0f;
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            if (i == index) wait = Mathf.Max(wait, cards[i].PlaySelected());
            else cards[i].PlayDismiss();
        }
        if (wait > 0f) yield return new WaitForSecondsRealtime(wait);
        string pn = currentPerkChoices[index].perkName;
        perkCopies[pn] = perkCopies.ContainsKey(pn) ? perkCopies[pn] + 1 : 1;
        ApplyPerk(currentPerkChoices[index]);
        BetaLog("perk_" + pn);
        Log(Loc("log_perk_chosen", currentPerkChoices[index].perkName));

        if (pendingPerkOffers > 0) pendingPerkOffers--;

        if (pendingPerkOffers > 0)
        {
            OfferPerkChoice();
            yield break;
        }

        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);

        isChoosingPerk = false;
        ShowRecaudacionAfterPerk();
    }

    void ApplyPerk(PerkOption perk)
    {
        switch (perk.effect)
        {
            case PerkEffect.DamageBonus: perkDamageBonus += perk.value; break;
            case PerkEffect.StaminaMaxBonus: perkStaminaMaxBonus += perk.value; break;
            case PerkEffect.MoneyMultiplierBonus: perkWaterMultiplierBonus += perk.value; break;
            case PerkEffect.SwingIntervalReduction: perkSwingIntervalReduction += perk.value; break;
            case PerkEffect.ExtraCoconutNextCycle: if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += Mathf.RoundToInt(perk.value); break;
            case PerkEffect.CritChanceBonus: perkCritChanceBonus += perk.value; break;
            case PerkEffect.WaterMultiplierBonus: perkWaterMultiplierBonus += perk.value; break;
            case PerkEffect.WaterForSwingPenalty: perkWaterMultiplicative *= 1f + perk.value; perkSwingIntervalReduction -= perk.value * 0.48f; break;
            case PerkEffect.HitRadiusBonus: skillHitRadiusBonus += perk.value; break;
            case PerkEffect.JackpotChanceBonus: skillJackpotChance += perk.value; break;
            case PerkEffect.StreakMaxStacksBonus: skillStreakStackBonus += Mathf.RoundToInt(perk.value); break;
            case PerkEffect.StreakPowerBonus: skillStreakPowerBonus += perk.value; break;
            case PerkEffect.OrderDiscount: waterTargetML *= 1f - perk.value; break;
            case PerkEffect.CoinChanceBonus: coinDropChance += perk.value; break;
            case PerkEffect.ExecuteDamageBonus: skillExecuteBonus += perk.value; break;
            case PerkEffect.ChainSplash: skillChainSplashPercent += perk.value; break;
            case PerkEffect.LastBreathBonus: skillLastBreathBonus += perk.value; break;
            case PerkEffect.EarlyDayWaterBonus: skillEarlyDayBonus += perk.value; break;
            case PerkEffect.MultiHitDamageBonus: skillMultiHitBonus += perk.value; break;
            case PerkEffect.DamageForStaminaPenalty: perkDamageBonus += perk.value; perkStaminaMaxBonus -= perk.value * 0.5f; break;
        }
        RecalculateAllStats();
    }

    Sprite GetPerkIcon(PerkOption perk)
    {
        if (perk == null) return null;
        foreach (var entry in perkIcons)
        {
            if (entry != null && entry.perkName == perk.perkName) return entry.icon;
        }
        return perk.icon;
    }

    [ContextMenu("Rellenar nombres de perks")]
    void FillPerkIconNames()
    {
        string[] names = { "Manos Firmes", "Segundo Aire", "Buen Trato", "Reflejos",
                     "Golpe Certero", "Cosecha Extra", "Coco Generoso", "Golpe de Suerte",
                      "Cosecha Abundante", "Manos de Hierro", "Pulmones de Acero",
                      "Pacto del Cobrador", "Ojo de Halcón", "Coco Dorado", "Manos de Titán" };
        perkIcons.Clear();
        foreach (var n in names) perkIcons.Add(new PerkIconEntry { perkName = n });
    }
    public LegacyItem GetLegacyItem(string itemName) => legacyShop.Find(i => i.itemName == itemName);

    public bool IsLegacyItemPurchased(string itemName) => purchasedLegacyItems.Contains(itemName);

    string PreviousLegacyTier(string n)
    {
        string basePart = null;
        if (n.EndsWith(" IV")) basePart = n.Substring(0, n.Length - 3) + " III";
        else if (n.EndsWith(" III")) basePart = n.Substring(0, n.Length - 4) + " II";
        else if (n.EndsWith(" II")) basePart = n.Substring(0, n.Length - 3);
        if (basePart == null) return null;
        if (GetLegacyItem(basePart) == null && GetLegacyItem(basePart + " I") != null) return basePart + " I";
        return basePart;
    }

    public void BuyLegacyItem(string itemName)
    {
        if (currentPhase != Phase.BetweenRuns) return;

        LegacyItem item = GetLegacyItem(itemName);
        if (item == null) return;
        if (purchasedLegacyItems.Contains(itemName)) return;
        if (legacyPoints < item.cost) return;
        string prev = PreviousLegacyTier(itemName);
        if (prev != null && !purchasedLegacyItems.Contains(prev)) { Log("Primero necesitas: " + prev); return; }
        legacyPoints -= item.cost;
        purchasedLegacyItems.Add(itemName);
        ApplyLegacyEffect(item);
        Log(Loc("log_legacy_bought", item.itemName));
        RefreshAllUI();
        SaveGame();
    }

    void ApplyLegacyEffect(LegacyItem item)
    {
        switch (item.effect)
        {
            case LegacyEffect.PermanentDamageBonus: legacyDamageBonus += item.value; break;
            case LegacyEffect.PermanentStaminaMaxBonus: legacyStaminaBonus += item.value; break;
            case LegacyEffect.PermanentMoneyMultiplierBonus: legacyWaterMultiplierBonus += item.value; break;
            case LegacyEffect.PermanentCritChanceBonus: legacyCritChanceBonus += item.value; break;
            case LegacyEffect.PermanentStartingCoconutBonus: legacyStartingCoconuts += Mathf.RoundToInt(item.value); if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += Mathf.RoundToInt(item.value); break;
            case LegacyEffect.PermanentLegacyMultiplierBonus: legacyMultiplierBonus += item.value; RecalcLegacyMultiplier(); break;
            case LegacyEffect.PermanentCoinChanceBonus: legacyCoinChanceBonus += item.value; coinDropChance += item.value; break;
            case LegacyEffect.PermanentExtraDay: legacyExtraDays += Mathf.RoundToInt(item.value); break;
            case LegacyEffect.PermanentStartingWater: legacyStartingWater += item.value; break;
            case LegacyEffect.PermanentJackpotChance: legacyJackpotChance += item.value; break;
            case LegacyEffect.PermanentStreakStacks: legacyStreakStacks += Mathf.RoundToInt(item.value); break;
            case LegacyEffect.PermanentHitRadius: legacyHitRadiusBonus += item.value; break;
            case LegacyEffect.PermanentOrderDiscount: legacyOrderDiscount += item.value; break;
            case LegacyEffect.PermanentStartingMachete: legacyStartingMachete += Mathf.RoundToInt(item.value); equippedMacheteIndex = Mathf.Max(equippedMacheteIndex, legacyStartingMachete); break;
        }
        RecalculateAllStats();
    }
}