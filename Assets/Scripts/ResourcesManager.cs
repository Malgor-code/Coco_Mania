using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    static ResourcesManager instance;
    public static ResourcesManager Instance
    {
        get
        {
            if (instance == null) instance = ManagerBootstrap.Ensure<ResourcesManager>();
            return instance;
        }
    }

    public float Water { get; private set; }
    public int Coins { get; private set; }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void SetWater(float value)
    {
        if (Mathf.Approximately(value, Water)) return;
        float delta = value - Water;
        Water = value;
        EventBus.Publish(new WaterChanged(Water, delta));
    }

    public void AddWater(float ml) => SetWater(Water + ml);
    public bool CanAffordWater(float cost) => Water >= cost;

    public bool TrySpendWater(float cost)
    {
        if (Water < cost) return false;
        SetWater(Water - cost);
        return true;
    }

    public void SetCoins(int value)
    {
        if (value == Coins) return;
        int delta = value - Coins;
        Coins = value;
        EventBus.Publish(new CoinsChanged(Coins, delta));
    }

    public void AddCoins(int amount) => SetCoins(Coins + amount);
    public bool CanAffordCoins(int cost) => Coins >= cost;

    public bool TrySpendCoins(int cost)
    {
        if (Coins < cost) return false;
        SetCoins(Coins - cost);
        return true;
    }

    public static string FormatWater(float ml)
    {
        return Mathf.RoundToInt(ml).ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " mL";
    }
}