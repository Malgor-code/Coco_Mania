using UnityEngine;
using UnityEngine.UI;

public class RewardCollectionUI : MonoBehaviour, IRewardSubPanel
{
    public GameObject panelRoot;
    public RewardMachineManager machine;
    public RewardDatabase database;

    [Header("Ranuras (arriba)")]
    [Tooltip("Contenedor (con Horizontal Layout Group) donde se instancian las ranuras.")]
    public Transform slotsParent;
    public RewardSlotUI slotPrefab;

    [Header("Cuadricula de objetos")]
    [Tooltip("El Transform con un Layout Group (Grid/Vertical) donde se instancian las entradas.")]
    public Transform gridParent;
    public RewardItemUI itemPrefab;
    public Button closeButton;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (machine != null) machine.OnStateChanged += OnMachineChanged;
    }

    void OnDestroy()
    {
        if (machine != null) machine.OnStateChanged -= OnMachineChanged;
    }

    void OnMachineChanged()
    {
        if (IsOpen) Populate();
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
        PopulateSlots();
        PopulateGrid();
    }

    void PopulateSlots()
    {
        if (slotsParent == null || slotPrefab == null || machine == null) return;

        foreach (Transform child in slotsParent) Destroy(child.gameObject);

        var db = database != null ? database : machine.database;
        var slots = machine.currentRun.slots;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            RewardData data = (!slot.IsEmpty && db != null) ? db.GetById(slot.rewardId) : null;

            RewardSlotUI ui = Instantiate(slotPrefab, slotsParent);
            ui.Setup(i, slot, data, machine.CanUnequip(i), OnSlotClicked);
        }
    }

    void PopulateGrid()
    {
        var db = database != null ? database : (machine != null ? machine.database : null);
        if (gridParent == null || itemPrefab == null || db == null) return;

        foreach (Transform child in gridParent) Destroy(child.gameObject);

        foreach (var data in db.allRewards)
        {
            if (data == null) continue;
            if (data.category != RewardCategory.Temporary && data.category != RewardCategory.PermanentRelic) continue;

            bool unlocked = machine != null && machine.persistent.unlockedRewardIds.Contains(data.id);
            int bagCount = machine != null ? machine.GetBagCount(data.id) : 0;
            bool equipped = machine != null && machine.IsEquipped(data.id);
            bool canPlace = bagCount > 0;
            RewardItemUI item = Instantiate(itemPrefab, gridParent);
            item.Setup(data, unlocked, canPlace, OnItemClicked, bagCount, equipped);
        }
    }

    void OnItemClicked(RewardData data)
    {
        if (machine == null || data == null) return;

        if (!machine.Equip(data.id))
        {
            Debug.Log("[RewardCollectionUI] No hay ranuras libres para colocar '" + data.displayName + "'.");
        }
    }

    void OnSlotClicked(int slotIndex)
    {
        if (machine != null) machine.Unequip(slotIndex);
    }
}