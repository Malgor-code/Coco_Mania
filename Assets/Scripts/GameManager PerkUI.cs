using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static MacheteData;

public partial class GameManager
{
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
        RaiseRecaudacionShown();
        AnnounceNewUnlocks();
        SaveGame();
        CheckAchievements();
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
        Perks.RegisterChosen(pn);
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
}