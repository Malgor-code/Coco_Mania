using UnityEngine;
using TMPro;

public class SkillNodeButton : MonoBehaviour
{
    public string nodeId;
    public TMP_Text label;

    [Tooltip("Opcional: un overlay/candado que se prende si todavia no se puede comprar")]
    public GameObject lockedOverlay;

    public void OnClick()
    {
        if (GameManager.Instance != null) GameManager.Instance.UnlockSkill(nodeId);
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        SkillNode node = GameManager.Instance.GetSkillNode(nodeId);
        if (node == null) return;

        bool unlocked = GameManager.Instance.IsSkillUnlocked(nodeId);
        bool canUnlock = GameManager.Instance.CanUnlockSkillPublic(nodeId);

        if (label != null)
        {
            label.text = unlocked ? node.nodeName + "\n(comprado)" : node.nodeName + "\n$" + node.cost;
        }

        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!unlocked && !canUnlock);
        }
    }
}