using UnityEngine;
using TMPro;
public class MacheteSelectButton : MonoBehaviour
{
    public int macheteIndex;
    public TMP_Text label;

    public void OnClick()
    {
        if (GameManager.Instance != null) GameManager.Instance.TrySelectMachete(macheteIndex);
    }

    void Update()
    {
        if (GameManager.Instance == null || label == null) return;

        MacheteData data = GameManager.Instance.GetMacheteData(macheteIndex);
        if (data == null) return;

        if (GameManager.Instance.equippedMacheteIndex == macheteIndex)
        {
            label.text = data.macheteName + "\n(equipado)";
        }
        else if (GameManager.Instance.IsMacheteUnlocked(macheteIndex))
        {
            label.text = data.macheteName + "\nEquipar";
        }
        else
        {
            label.text = data.macheteName + "\nDesbloquear ($" + data.unlockCost + ")";
        }
    }
}