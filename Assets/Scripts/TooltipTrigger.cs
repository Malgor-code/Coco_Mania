using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SkillNodeButton))]
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private SkillNodeButton nodeButton;

    void Awake()
    {
        nodeButton = GetComponent<SkillNodeButton>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipUI.Instance == null || nodeButton == null || GameManager.Instance == null) return;

        SkillNode node = GameManager.Instance.GetSkillNode(nodeButton.nodeId);
        if (node == null) return;

        if (!ArePrerequisitesMet(node)) return;

        bool unlocked = GameManager.Instance.IsSkillUnlocked(node.id);

        string title = node.nodeName;
        string description = node.description;
        string stat = BuildStatLine(node);
        string price = unlocked ? "" : ("$" + node.cost);

        TooltipUI.Instance.ShowUpgradeInfo(title, description, stat, price);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipUI.Instance != null) TooltipUI.Instance.Hide();
    }

    bool ArePrerequisitesMet(SkillNode node)
    {
        if (node.prerequisiteIds == null) return true;

        foreach (var pre in node.prerequisiteIds)
        {
            if (!string.IsNullOrEmpty(pre) && !GameManager.Instance.IsSkillUnlocked(pre)) return false;
        }
        return true;
    }

    string BuildStatLine(SkillNode node)
    {
        MacheteController mc = MacheteController.Instance;
        CoconutSpawner cs = CoconutSpawner.Instance;

        switch (node.effect)
        {
            case SkillEffect.DamageFlatBonus:
                if (mc == null) return "";
                int newDamage = mc.damageOverride + Mathf.RoundToInt(node.effectValue);
                return "Dano base: " + mc.damageOverride + " -> " + newDamage;

            case SkillEffect.StaminaMaxFlatBonus:
                float curStamina = GameManager.Instance.GetStaminaMax();
                return "Energia maxima: " + curStamina.ToString("0") + " -> " + (curStamina + node.effectValue).ToString("0");

            case SkillEffect.SwingIntervalReduction:
                if (mc == null) return "";
                float newInterval = Mathf.Max(0.3f, mc.swingInterval - node.effectValue);
                return "Cooldown de golpe: " + mc.swingInterval.ToString("0.00") + "s -> " + newInterval.ToString("0.00") + "s";

            case SkillEffect.HitRadiusBonus:
                if (mc == null) return "";
                float newRadius = mc.hitRadius + node.effectValue;
                return "Radio de golpe: " + mc.hitRadius.ToString("0.00") + " -> " + newRadius.ToString("0.00");

            case SkillEffect.MoneyMultiplierBonus:
                float curPct = GameManager.Instance.GetSkillMoneyMultiplierBonusPercent();
                float newPct = curPct + node.effectValue * 100f;
                return "Dinero por coco: +" + curPct.ToString("0") + "% -> +" + newPct.ToString("0") + "%";

            case SkillEffect.StaminaCostReduction:
                if (mc == null) return "";
                float newCost = Mathf.Max(0.3f, mc.swingStaminaCostOverride - node.effectValue);
                return "Energia por golpe: " + mc.swingStaminaCostOverride.ToString("0.00") + " -> " + newCost.ToString("0.00");

            case SkillEffect.ExtraStartingCoconut:
                if (cs == null) return "";
                int newStarting = cs.startingCoconuts + Mathf.RoundToInt(node.effectValue);
                return "Cocos al iniciar el dia: " + cs.startingCoconuts + " -> " + newStarting;

            case SkillEffect.SpawnIntervalReduction:
                if (cs == null) return "";
                float newSpawn = Mathf.Max(0.5f, cs.spawnInterval - node.effectValue);
                return "Nuevo coco cada: " + cs.spawnInterval.ToString("0.0") + "s -> " + newSpawn.ToString("0.0") + "s";

            default:
                return "";
        }
    }
}