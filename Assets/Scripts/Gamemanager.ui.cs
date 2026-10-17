using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static MacheteData;

public partial class GameManager
{
    void HandleUpgradeTreePanZoom()
    {
        if (upgradeTreeContent == null) return;

        bool panelOpen = upgradeTreePanel != null && upgradeTreePanel.activeInHierarchy;
        if (!panelOpen)
        {
            isPanningTree = false;
            return;
        }
        if (Input.GetMouseButtonDown(panMouseButton))
        {
            isPanningTree = true;
            lastPanMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(panMouseButton))
        {
            isPanningTree = false;
        }

        if (isPanningTree)
        {
            Vector2 currentMousePos = Input.mousePosition;
            Vector2 delta = (currentMousePos - lastPanMousePos) * panSpeed;
            Vector2 newPos = upgradeTreeContent.anchoredPosition + delta;
            ClampContentPosition(ref newPos);
            upgradeTreeContent.anchoredPosition = newPos;
            lastPanMousePos = currentMousePos;
        }
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newScale = Mathf.Clamp(upgradeTreeContent.localScale.x + scroll * zoomSpeed, minZoom, maxZoom);
            upgradeTreeContent.localScale = new Vector3(newScale, newScale, upgradeTreeContent.localScale.z);
            Vector2 clampedPos = upgradeTreeContent.anchoredPosition;
            ClampContentPosition(ref clampedPos);
            upgradeTreeContent.anchoredPosition = clampedPos;
        }
    }

    void ClampContentPosition(ref Vector2 pos)
    {
        if (upgradeTreeViewport != null)
        {
            Vector2 contentSize = Vector2.Scale(upgradeTreeContent.rect.size, upgradeTreeContent.localScale);
            Vector2 viewportSize = upgradeTreeViewport.rect.size;

            float maxX = Mathf.Max(0f, (contentSize.x - viewportSize.x) / 2f);
            float maxY = Mathf.Max(0f, (contentSize.y - viewportSize.y) / 2f);

            pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
            pos.y = Mathf.Clamp(pos.y, -maxY, maxY);
        }
        else
        {
            pos.x = Mathf.Clamp(pos.x, -panLimit.x, panLimit.x);
            pos.y = Mathf.Clamp(pos.y, -panLimit.y, panLimit.y);
        }
    }

    public void RecenterUpgradeTree()
    {
        if (upgradeTreeContent == null) return;
        upgradeTreeContent.anchoredPosition = Vector2.zero;
        upgradeTreeContent.localScale = Vector3.one;
    }
    string PassiveLine(MacheteData m)
    {
        return (m != null && m.passive != MachetePassive.Ninguna && !string.IsNullOrEmpty(m.passiveDescription))
            ? "\n" + m.passiveDescription : "";
    }

    string StatsGainLine(MacheteData cur, MacheteData next)
    {
        if (next == null) return "";
        float dDmg = next.baseDamage - (cur != null ? cur.baseDamage : 0f);
        float dSwing = next.baseSwingInterval - (cur != null ? cur.baseSwingInterval : next.baseSwingInterval);
        float dRadius = next.baseHitRadius - (cur != null ? cur.baseHitRadius : 0f);

        return "\nDaño " + Signed(dDmg)
             + "  |  Golpe " + Signed(dSwing) + " s"
             + "  |  Radio " + Signed(dRadius);
    }

    static string Signed(float v) => (v >= 0f ? "+" : "") + v.ToString("0.##");

    void UpdateMoneyUI()
    {
        if (!Mathf.Approximately(displayedMoney, waterCurrentML))
        {
            float diff = waterCurrentML - displayedMoney;
            float step = diff * moneyCounterSpeed * Time.unscaledDeltaTime;
            if (Mathf.Abs(step) < 1f) step = Mathf.Sign(diff) * Mathf.Min(Mathf.Abs(diff), 1f);
            displayedMoney += step;

            bool overshot = (diff > 0f && displayedMoney > waterCurrentML) || (diff < 0f && displayedMoney < waterCurrentML);
            if (overshot) displayedMoney = waterCurrentML;
        }

        if (moneyText != null) moneyText.text = Loc("collection_current_money", FormatWater(displayedMoney));
        if (moneyText2 != null) moneyText2.text = Loc("collection_current_money", FormatWater(displayedMoney));
        if (moneyText3 != null) moneyText3.text = Loc("collection_current_money", FormatWater(displayedMoney));
        if (coinCountText != null) coinCountText.text = Loc("collection_coins", coconutCoins);
    }
    void RefreshAllUI()
    {
        if (indicacionesPanel != null)
        {
            bool showIndicaciones = (upgradeTreePanel != null && upgradeTreePanel.activeSelf)
                      || (tiendaPanel != null && tiendaPanel.activeSelf)
                      || (rewardMachinePanel != null && rewardMachinePanel.activeSelf);
            indicacionesPanel.SetActive(showIndicaciones);
        }
        UpdateMoneyUI();

        if (runKillsText != null) runKillsText.text = Loc("collection_coconuts_killed", dayCoconutsKilled);
        if (runMoneyText != null) runMoneyText.text = Loc("collection_money_earned", FormatWater(dayMoneyEarned));

        if (accuracyText != null)
        {
            float accuracyPercent = daySwings > 0 ? (dayHits / (float)daySwings) * 100f : 0f;
            accuracyText.text = Mathf.RoundToInt(accuracyPercent) + "% de punteria";
        }

        if (dayCoinsText != null)
        {
            bool gotCoinsToday = dayCoinsFound > 0;
            dayCoinsText.gameObject.SetActive(gotCoinsToday);
            if (gotCoinsToday)
            {
                dayCoinsText.text = "+" + dayCoinsFound + (dayCoinsFound == 1 ? " moneda encontrada" : " monedas encontradas");
            }
        }

        CoconutUnlockThreshold nextUnlock = GetNextLockedCoconut();
        float unlockProgress = GetNextUnlockProgress01();

        if (nextUnlockImage != null)
        {
            bool showImage = nextUnlock != null;
            nextUnlockImage.gameObject.SetActive(showImage);
            if (showImage)
            {
                if (nextUnlock.icon != null) nextUnlockImage.sprite = nextUnlock.icon;
                Color c = nextUnlockImage.color;
                c.a = Mathf.Lerp(lockedImageMinAlpha, 1f, unlockProgress);
                nextUnlockImage.color = c;
            }
        }
        if (nextUnlockPercentText != null)
        {
            bool showText = nextUnlock != null;
            nextUnlockPercentText.gameObject.SetActive(showText);
            if (showText) nextUnlockPercentText.text = "Pedido " + nextUnlock.cycleRequired;
        }

        if (deudaMoneyText != null) deudaMoneyText.text = Loc("collection_current_money", FormatWater(waterCurrentML));

        bool readyToDeliver = waterCurrentML >= waterTargetML;

        if (deudaBillText != null)
        {
            if (readyToDeliver) deudaBillText.text = "¡Pedido listo! Entregalo";
            else
            {
                float remaining = Mathf.Max(0f, waterTargetML - waterCurrentML);
                deudaBillText.text = "Faltan " + FormatWater(remaining);
            }
        }

        if (deudaDaysLeftText != null)
        {
            int daysToPlay = DaysToPlay;
            if (forcedOrderDecision)
            {
                deudaDaysLeftText.text = readyToDeliver ? "¡ÚLTIMO DÍA! Paga o bancarrota" : "¡ÚLTIMO DÍA! No alcanza: bancarrota";
                deudaDaysLeftText.color = urgentColor;
            }
            else if (!readyToDeliver && daysToPlay <= 1)
            {
                deudaDaysLeftText.text = "¡ÚLTIMO DÍA PARA JUNTAR EL AGUA!";
                deudaDaysLeftText.color = urgentColor;
            }
            else
            {
                deudaDaysLeftText.text = daysToPlay + (daysToPlay == 1 ? " día restante" : " días restantes");
                deudaDaysLeftText.color = normalDaysColor;
            }
        }

        if (bankruptcyButton != null) bankruptcyButton.gameObject.SetActive(forcedOrderDecision);

        if (payDebtButton != null) payDebtButton.interactable = readyToDeliver;

        MacheteData current = GetCurrentMachete();
        MacheteData next = GetNextMachete();
        if (macheteStatusText != null)
            macheteStatusText.text = Loc("shop_current_machete", current != null ? current.macheteName : "-") + PassiveLine(current);
        if (next != null)
        {
            if (macheteUpgradeCostText != null)
                macheteUpgradeCostText.text = Loc("shop_next_machete", next.macheteName, FormatWater(next.unlockCost))
                            + StatsGainLine(current, next) + PassiveLine(next);
            if (macheteUpgradeButton != null) macheteUpgradeButton.interactable = waterCurrentML >= next.unlockCost;
        }
        else
        {
            if (macheteUpgradeCostText != null) macheteUpgradeCostText.text = Loc("shop_max_machete");
            if (macheteUpgradeButton != null) macheteUpgradeButton.interactable = false;
        }
        if (coinCountText != null) coinCountText.text = Loc("collection_coins", coconutCoins);

        if (legacyPointsText != null) legacyPointsText.text = Loc("betweenruns_legacy_points", legacyPoints);
    }
    public CoconutAbility GetAbilityForCoconutName(string name)
    {
        if (coconutUnlocks == null || string.IsNullOrEmpty(name)) return CoconutAbility.Ninguna;
        foreach (var u in coconutUnlocks)
        {
            if (u != null && u.coconutName == name) return u.ability;
        }
        return CoconutAbility.Ninguna;
    }

    public CoconutUnlockThreshold GetNextLockedCoconut()
    {
        CoconutUnlockThreshold next = null;
        if (coconutUnlocks == null) return null;
        foreach (var u in coconutUnlocks)
        {
            if (u == null) continue;
            if (billCycle < u.cycleRequired)
            {
                if (next == null || u.cycleRequired < next.cycleRequired) next = u;
            }
        }
        return next;
    }

    void AnnounceNewUnlocks()
    {
        if (unlockPanel == null || coconutUnlocks == null) return;
        for (int i = 1; i < coconutUnlocks.Count; i++)
        {
            CoconutUnlockThreshold u = coconutUnlocks[i];
            if (u == null || billCycle < u.cycleRequired) continue;
            if (!announcedUnlocks.Add(i)) continue;
            if (showUnlockOnlyFirstTime)
            {
                string key = "coco_visto_" + i;
                if (PlayerPrefs.GetInt(key, 0) == 1) continue;
                PlayerPrefs.SetInt(key, 1);
            }
            unlockPanel.Enqueue(i);
        }
        PlayerPrefs.Save();
    }

    public float GetNextUnlockProgress01()
    {
        CoconutUnlockThreshold next = GetNextLockedCoconut();
        if (next == null) return 1f;
        int prev = 1;
        foreach (var u in coconutUnlocks)
            if (u != null && u.cycleRequired < next.cycleRequired && u.cycleRequired > prev) prev = u.cycleRequired;
        int span = next.cycleRequired - prev;
        if (span <= 0) return 1f;
        return Mathf.Clamp01((float)(billCycle - prev) / span);
    }

    [ContextMenu("Borrar cocos vistos (para probar el panel)")]
    void ClearSeenCoconuts()
    {
        for (int i = 0; i < 15; i++) PlayerPrefs.DeleteKey("coco_visto_" + i);
        PlayerPrefs.Save();
    }
}