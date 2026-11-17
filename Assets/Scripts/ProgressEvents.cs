public readonly struct SkillUnlocked : IEvent
{
    public readonly string Id;
    public SkillUnlocked(string id) { Id = id; }
}

public readonly struct MacheteBought : IEvent
{
    public readonly int Index;
    public MacheteBought(int index) { Index = index; }
}

public readonly struct LegacyPurchased : IEvent
{
    public readonly string ItemName;
    public LegacyPurchased(string itemName) { ItemName = itemName; }
}