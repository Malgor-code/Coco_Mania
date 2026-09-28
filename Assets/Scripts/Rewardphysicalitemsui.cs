using UnityEngine;
using UnityEngine.UI;

public class RewardPhysicalItemsUI : MonoBehaviour, IRewardSubPanel
{
    public GameObject panelRoot;
    public RewardMachineManager machine;
    public RewardDatabase database;
    public Transform listParent;
    public RewardPhysicalItemEntryUI entryPrefab;
    public Button closeButton;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (machine != null) machine.OnStateChanged += Populate;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (machine != null) machine.OnStateChanged -= Populate;
        CancelInvoke(nameof(Populate));
    }

    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        Populate();
        CancelInvoke(nameof(Populate));
        InvokeRepeating(nameof(Populate), 1f, 1f); 
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        CancelInvoke(nameof(Populate));
    }

    void Populate()
    {
        if (listParent == null || entryPrefab == null || machine == null || database == null) return;

        foreach (Transform child in listParent) Destroy(child.gameObject);

        foreach (var inst in machine.currentRun.physicalItems)
        {
            var data = database.GetById(inst.rewardId);
            if (data == null) continue;

            var entry = Instantiate(entryPrefab, listParent);
            entry.Setup(data, inst);
        }
    }
}