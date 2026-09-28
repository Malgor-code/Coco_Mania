using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardPhysicalItemEntryUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text statusText;

    public void Setup(RewardData data, PhysicalItemInstance inst)
    {
        if (iconImage != null) iconImage.sprite = data.icon;
        if (nameText != null) nameText.text = data.displayName;

        if (statusText != null)
        {
            string usesLabel = inst.usesRemaining < 0 ? "ilimitado" : (inst.usesRemaining + " usos restantes");

            if (data.triggerMode == PhysicalTriggerMode.EveryNewDay)
            {
                statusText.text = $"Se activa 1 vez por dia ({usesLabel})";
            }
            else
            {
                float remaining = Mathf.Max(0f, data.intervalSeconds - inst.secondsSinceLastTrigger);
                statusText.text = $"Proxima activacion en {remaining:0}s ({usesLabel})";
            }
        }
    }
}