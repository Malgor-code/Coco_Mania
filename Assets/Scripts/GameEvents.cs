public readonly struct GameStateChanged : IEvent
{
    public readonly GameState Previous;
    public readonly GameState Current;
    public GameStateChanged(GameState previous, GameState current) { Previous = previous; Current = current; }
}

public readonly struct RunStarted : IEvent { }
public readonly struct RunReset : IEvent { }
public readonly struct RecaudacionShown : IEvent { }

public readonly struct DayStarted : IEvent
{
    public readonly int DayIndex;
    public DayStarted(int dayIndex) { DayIndex = dayIndex; }
}

public readonly struct WaterChanged : IEvent
{
    public readonly float Current;
    public readonly float Delta;
    public WaterChanged(float current, float delta) { Current = current; Delta = delta; }
}

public readonly struct CoinsChanged : IEvent
{
    public readonly int Current;
    public readonly int Delta;
    public CoinsChanged(int current, int delta) { Current = current; Delta = delta; }
}

public readonly struct LogMessage : IEvent
{
    public readonly string Text;
    public LogMessage(string text) { Text = text; }
}

public static class GameLog
{
    public static void Say(string text) => EventBus.Publish(new LogMessage(text));
}