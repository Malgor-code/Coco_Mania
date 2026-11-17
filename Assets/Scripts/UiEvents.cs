
public enum UIScreen
{
    None,     
    Hitting,   
    HittingActive, 
    Recaudacion,
    ForcedOrder,
    PerkChoice, 
    BetweenRuns, 
    Event   
}

public readonly struct ScreenRequested : IEvent
{
    public readonly UIScreen Screen;
    public ScreenRequested(UIScreen screen) { Screen = screen; }
}

public readonly struct BankruptcyDeclared : IEvent
{
    public readonly int LegacyGained;
    public BankruptcyDeclared(int legacyGained) { LegacyGained = legacyGained; }
}