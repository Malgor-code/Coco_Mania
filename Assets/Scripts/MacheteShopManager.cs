using System.Collections.Generic;
using UnityEngine;
using static MacheteData;

public class MacheteShopManager : MonoBehaviour
{
    static MacheteShopManager instance;
    public static MacheteShopManager Instance
    {
        get
        {
            if (instance == null) instance = ManagerBootstrap.Ensure<MacheteShopManager>();
            return instance;
        }
    }

    public List<MacheteData> Options => GameData.Machetes;
    public int EquippedIndex { get; set; }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public MacheteData Get(int index)
    {
        if (index < 0 || index >= Options.Count) return null;
        return Options[index];
    }

    public MacheteData Current => Get(EquippedIndex);
    public MacheteData Next => Get(EquippedIndex + 1);

    public bool IsOwned(int index) => index == 0 || index == EquippedIndex;

    public bool CanAffordAny()
    {
        List<MacheteData> list = Options;
        float water = ResourcesManager.Instance.Water;
        for (int i = 0; i < list.Count; i++)
        {
            if (IsOwned(i)) continue;
            if (list[i].unlockCost <= water) return true;
        }
        return false;
    }

    public void RecalculateStats() => StatsManager.Instance.Recalculate(Current);

    public void BuyNextMachete() => TryBuyNext(out _);

    public bool TryBuyNext(out MacheteData bought)
    {
        bought = Next;
        if (bought == null) return false;
        if (!ResourcesManager.Instance.TrySpendWater(bought.unlockCost)) return false;

        EquippedIndex++;
        RecalculateStats();
        EventBus.Publish(new MacheteBought(EquippedIndex));
        return true;
    }
}