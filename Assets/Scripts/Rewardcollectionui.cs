using UnityEngine;
using UnityEngine.UI;

public class RewardCollectionUI : MonoBehaviour, IRewardSubPanel
{
    public GameObject panelRoot;
    public RewardMachineManager machine;
    public RewardDatabase database;
    [Tooltip("El Transform con un Layout Group (Grid/Vertical) donde se instancian las entradas.")]
    public Transform gridParent;
    public RewardItemUI itemPrefab;
    public Button closeButton;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;


    void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        Populate();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Populate()
    {
        if (gridParent == null || itemPrefab == null || database == null) return;

        foreach (Transform child in gridParent) Destroy(child.gameObject);

        foreach (var data in database.allRewards)
        {
            if (data == null) continue;

            bool unlocked = machine != null && machine.persistent.unlockedRewardIds.Contains(data.id);

            RewardItemUI item = Instantiate(itemPrefab, gridParent);
            item.Setup(data, unlocked, false, null);
        }
    }
}