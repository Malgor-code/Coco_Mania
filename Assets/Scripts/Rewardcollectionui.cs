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

    [Header("Panel que se apaga mientras la Coleccion esta abierta")]
    [Tooltip("Opcional. Se desactiva al abrir la Coleccion y se vuelve a activar al cerrarla.")]
    public GameObject panelToHide;

    private bool otherPanelHidden = false;

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
        if (panelToHide != null && panelToHide.activeSelf)
        {
            panelToHide.SetActive(false);
            otherPanelHidden = true;
        }

        if (panelRoot != null) panelRoot.SetActive(true);
        Populate();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        RestoreOtherPanel();
    }

    void RestoreOtherPanel()
    {
        if (!otherPanelHidden) return;
        otherPanelHidden = false;
        if (panelToHide != null) panelToHide.SetActive(true);
    }

    void OnDisable()
    {
        RestoreOtherPanel();
    }

    void Populate()
    {
        PopulateSlots();
        PopulateGrid();
    }

    void PopulateSlots()
    {
        if (machine == null) { Debug.LogWarning("[RewardCollectionUI] Falta asignar 'Machine'."); return; }
        if (slotsParent == null || slotPrefab == null)
        {
            Debug.LogWarning("[RewardCollectionUI] Falta asignar 'Slots Parent' y/o 'Slot Prefab' (las ranuras no se mostraran).");
            return;
        }

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
        if (db == null) { Debug.LogWarning("[RewardCollectionUI] No hay RewardDatabase (asigna 'Database' o la del Machine)."); return; }
        if (gridParent == null || itemPrefab == null)
        {
            Debug.LogWarning("[RewardCollectionUI] Falta asignar 'Grid Parent' y/o 'Item Prefab'.");
            return;
        }
        if (db.allRewards == null || db.allRewards.Count == 0)
            Debug.LogWarning("[RewardCollectionUI] La RewardDatabase esta vacia. Ejecuta Tools > Reward Machine > Generar Recompensas.");

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