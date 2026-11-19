using UnityEngine;
using ST = StatType;

public class DebtManager : MonoBehaviour
{
    static DebtManager instance;
    public static DebtManager Instance
    {
        get
        {
            if (instance == null) instance = ManagerBootstrap.Ensure<DebtManager>();
            return instance;
        }
    }

    [Header("Tabla de deudas (hecha a mano)")]
    [Tooltip("Asset DebtTableSO con los dias y el monto de cada deuda. Si lo dejas vacio se usa una tabla generada con la formula anterior.")]
    public DebtTableSO table;

    [Header("Meta de la partida")]
    public int victoryCycle = 16;
    public int victoryLegacyBonus = 10;

    [Header("Descuentos al pedido (arbol, perks, legado)")]
    [Range(0f, 0.95f)]
    [Tooltip("Tope del descuento total acumulado.")]
    public float maxOrderDiscount = 0.75f;

    public int BillCycle { get; set; } = 1;
    public int DaysLeft { get; set; } = 6;
    public int BestCycleReached { get; set; } = 1;

    bool victoryAwarded;
    public bool VictoryAwarded { get => victoryAwarded; set => victoryAwarded = value; }
    DebtTableSO fallbackTable;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    DebtTableSO Table
    {
        get
        {
            if (table != null) return table;
            if (fallbackTable == null)
            {
                fallbackTable = DebtTableSO.CreateDefault();
                Debug.LogWarning("[DebtManager] No hay DebtTableSO asignada; se usa una tabla generada con la formula anterior. " +
                                 "Crea un asset (Create > CocoMania > Debt Table) y asignalo para editar las deudas a mano.");
            }
            return fallbackTable;
        }
    }

    public float DiscountFactor =>
        1f - Mathf.Clamp(StatsManager.Instance.Get(ST.OrderDiscount), 0f, maxOrderDiscount);
    public float BaseTarget(int cycle) => Table.Get(cycle).waterTarget;
    public float TargetWater => BaseTarget(BillCycle) * DiscountFactor;
    public int DaysForCycle(int cycle) =>
        Mathf.Max(1, Table.Get(cycle).days + Mathf.RoundToInt(StatsManager.Instance.Get(ST.ExtraDays)));
    public bool CanPay => ResourcesManager.Instance.CanAffordWater(TargetWater);

    public void ResetRun()
    {
        BillCycle = 1;
        DaysLeft = DaysForCycle(1);
        victoryAwarded = false;
    }
    public bool TryPay(out float delivered, out float refund, out int deliveredCycle)
    {
        deliveredCycle = BillCycle;
        delivered = TargetWater;
        refund = 0f;

        ResourcesManager res = ResourcesManager.Instance;
        if (!res.CanAffordWater(delivered)) return false;

        res.AddWater(-delivered);
        refund = delivered * StatsManager.Instance.Get(ST.OrderRefund);
        if (refund > 0f) res.AddWater(refund);

        BillCycle++;
        BestCycleReached = Mathf.Max(BestCycleReached, BillCycle);
        DaysLeft = DaysForCycle(BillCycle);

        EventBus.Publish(new DebtPaid(deliveredCycle, delivered, refund));
        return true;
    }

    public bool TryAwardVictory(int deliveredCycle)
    {
        if (victoryAwarded || deliveredCycle < victoryCycle) return false;
        victoryAwarded = true;
        return true;
    }
}