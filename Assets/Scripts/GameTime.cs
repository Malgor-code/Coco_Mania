using UnityEngine;

public static class GameTime
{
    public static bool IsPaused => GameManager.IsPaused;
    public static float DeltaTime => IsPaused ? 0f : Time.deltaTime;
}

public sealed class WaitForGameSeconds : CustomYieldInstruction
{
    readonly float duration;
    float elapsed;

    public WaitForGameSeconds(float seconds) { duration = seconds; }

    public override bool keepWaiting
    {
        get
        {
            if (!GameTime.IsPaused) elapsed += Time.deltaTime;
            return elapsed < duration;
        }
    }
}