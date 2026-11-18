using UnityEngine;

public interface ICoconutPassive
{
    void Tick(float dt);
    float ModifyDamage(float amount, bool isCritical);
    void OnHit();
    void OnDeath();
}

public abstract class CoconutPassiveBase : ICoconutPassive
{
    protected readonly ICoconutHost host;

    protected CoconutPassiveBase(ICoconutHost host) { this.host = host; }

    protected float HpPercent => host != null && host.HpMax > 0 ? (float)host.Hp / host.HpMax : 1f;

    public virtual void Tick(float dt) { }
    public virtual float ModifyDamage(float amount, bool isCritical) => amount;
    public virtual void OnHit() { }
    public virtual void OnDeath() { }
}
public sealed class NullPassive : CoconutPassiveBase
{
    public static readonly NullPassive Instance = new NullPassive();
    NullPassive() : base(null) { }
}

public sealed class CoconutRegen
{
    readonly ICoconutHost host;
    readonly float delayAfterHit;
    float lastHit;
    float accumulator;

    public CoconutRegen(ICoconutHost host, float delayAfterHit)
    {
        this.host = host;
        this.delayAfterHit = delayAfterHit;
        lastHit = Time.time;
    }

    public void NotifyHit() => lastHit = Time.time;

    public void Tick(float dt, float percentPerSecond)
    {
        if (Time.time - lastHit < delayAfterHit) { accumulator = 0f; return; }
        if (host.Hp >= host.HpMax) return;

        accumulator += host.HpMax * percentPerSecond * dt;
        if (accumulator < 1f) return;

        int healed = Mathf.FloorToInt(accumulator);
        accumulator -= healed;
        host.Heal(healed);
        host.Flash(Color.Lerp(Color.white, Color.green, 0.5f), 0.12f);
    }
}