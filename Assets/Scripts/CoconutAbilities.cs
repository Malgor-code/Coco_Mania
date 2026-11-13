using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CoconutAbilitySettings
{
    [Header("Resistente / Armadura / Diamante")]
    [Range(0f, 1f)] public float resistenteReduction = 0.30f;
    [Range(0f, 1f)] public float armaduraReduction = 0.50f;
    [Range(0f, 1f)] public float diamanteReduction = 0.75f;
    public float diamanteCritMultiplier = 4f;

    [Header("Fibra Dura")]
    public float fibraStackPerHit = 0.05f;
    public float fibraMaxStack = 0.25f;
    public float fibraResetTime = 1.5f;

    [Header("Tenaz")]
    [Range(0f, 1f)] public float tenazHpThreshold = 0.3f;
    [Range(0f, 1f)] public float tenazReduction = 0.25f;

    [Header("Rebote")]
    public int reboteHitsPerTrigger = 5;
    public float reboteStaminaPenalty = 2f;

    [Header("Fortificado")]
    public float fortificadoNormalDuration = 4f;
    public float fortificadoBuffDuration = 3f;
    [Range(0f, 1f)] public float fortificadoReduction = 0.4f;
    [Range(0f, 1f)] public float fortificadoDarken = 0.45f;

    [Header("Implacable")]
    public int implacableHitsToTrigger = 4;
    public float implacableWindow = 2f;
    public float implacableBuffDuration = 2f;
    [Range(0f, 1f)] public float implacableReduction = 0.3f;

    [Header("Regeneracion")]
    public float regenDelayAfterHit = 2f;
    [Range(0f, 0.2f)] public float regenPercentPerSecond = 0.03f;
    [Range(0f, 0.2f)] public float supremoPhase2RegenPercent = 0.015f;

    [Header("Camuflaje")]
    public float camuflajeVisibleDuration = 3f;
    public float camuflajeHiddenDuration = 2f;
    [Range(0f, 1f)] public float camuflajeHiddenAlpha = 0.2f;

    [Header("Maldicion")]
    public float maldicionDuration = 6f;

    [Header("Supremo (mini-boss)")]
    [Range(0f, 1f)] public float supremoPhase2Hp = 0.7f;
    [Range(0f, 1f)] public float supremoPhase3Hp = 0.3f;
    [Range(0f, 1f)] public float supremoPhase1Reduction = 0.2f;
    [Range(0f, 1f)] public float supremoPhase3Reduction = 0.35f;
}

public class AbilityBehaviour
{
    protected readonly ICoconutHost host;
    protected readonly CoconutAbilitySettings s;

    public AbilityBehaviour(ICoconutHost host, CoconutAbilitySettings settings)
    {
        this.host = host;
        s = settings;
    }

    protected float HpPercent => host.HpMax > 0 ? (float)host.Hp / host.HpMax : 1f;

    public virtual void Tick(float dt) { }
    public virtual float ModifyDamage(float amount, bool isCritical) => amount;
    public virtual void OnHit() { }
    public virtual void OnDeath() { }
}

public sealed class NoAbility : AbilityBehaviour
{
    public NoAbility(ICoconutHost h, CoconutAbilitySettings s) : base(h, s) { }
}
public sealed class FlatReduction : AbilityBehaviour
{
    readonly float reduction;
    public FlatReduction(ICoconutHost h, CoconutAbilitySettings s, float reduction) : base(h, s) { this.reduction = reduction; }
    public override float ModifyDamage(float a, bool crit) => crit ? a : a * (1f - reduction);
}

public sealed class FragilValiosoAbility : AbilityBehaviour
{
    public FragilValiosoAbility(ICoconutHost h, CoconutAbilitySettings s) : base(h, s) { }
    public override float ModifyDamage(float a, bool crit) =>
        crit ? a * s.diamanteCritMultiplier : a * (1f - s.diamanteReduction);
}

public sealed class FibraDuraAbility : AbilityBehaviour
{
    float stack, lastHit = -999f;
    public FibraDuraAbility(ICoconutHost h, CoconutAbilitySettings s) : base(h, s) { }

    public override float ModifyDamage(float a, bool crit)
    {
        if (Time.time - lastHit > s.fibraResetTime) stack = 0f;
        float mult = 1f - Mathf.Min(stack, s.fibraMaxStack);
        stack += s.fibraStackPerHit;
        lastHit = Time.time;
        return a * mult;
    }
}

public sealed class TenazAbility : AbilityBehaviour
{
    public TenazAbility(ICoconutHost h, CoconutAbilitySettings s) : base(h, s) { }
    public override float ModifyDamage(float a, bool crit) =>
        HpPercent <= s.tenazHpThreshold ? a * (1f - s.tenazReduction) : a;
}

public sealed class ReboteAbility : AbilityBehaviour
{
    int hits;
    public ReboteAbility(ICoconutHost h, CoconutAbilitySettings s) : base(h, s) { }

    public override void OnHit()
    {
        if (++hits < s.reboteHitsPerTrigger) return;
        hits = 0;
        if (GameManager.Instance != null) GameManager.Instance.ApplyStaminaPenalty(s.reboteStaminaPenalty);
        host.FloatText("¡REBOTE!", 0.8f);
        host.Flash(Color.yellow, 0.2f);
    }
}

public sealed class FortificadoAbility : AbilityBehaviour
{
    float timer;
    bool active;
    public FortificadoAbility(ICoconutHost h, CoconutAbilitySettings s) : base(h, s) { }

    public override void Tick(float dt)
    {
        timer += dt;
        float cycle = s.fortificadoNormalDuration + s.fortificadoBuffDuration;
        bool shouldBeActive = timer % cycle >= s.fortificadoNormalDuration;
        if (shouldBeActive == active) return;
        active = shouldBeActive;
        host.SetDarken(active ? s.fortificadoDarken : 0f);
    }

    public override float ModifyDamage(float a, bool crit) => active ? a * (1f - s.fortificadoReduction) : a;
}

public sealed class ImplacableAbility : AbilityBehaviour
{
    readonly Queue<float> recentHits = new Queue<float>();
    float buffUntil = -999f;
    public ImplacableAbility(ICoconutHost h, CoconutAbilitySettings s) : base(h, s) { }

    public override void OnHit() => recentHits.Enqueue(Time.time);

    public override void Tick(float dt)
    {
        float cutoff = Time.time - s.implacableWindow;
        while (recentHits.Count > 0 && recentHits.Peek() < cutoff) recentHits.Dequeue();

        if (recentHits.Count >= s.implacableHitsToTrigger && Time.time > buffUntil)
        {
            buffUntil = Time.time + s.implacableBuffDuration;
            host.Flash(Color.cyan, 0.15f);
        }
    }

    public override float ModifyDamage(float a, bool crit) =>
        Time.time <= buffUntil ? a * (1f - s.implacableReduction) : a;
}

public sealed class RegenState
{
    readonly ICoconutHost host;
    readonly CoconutAbilitySettings s;
    float lastHit;
    float accumulator;

    public RegenState(ICoconutHost host, CoconutAbilitySettings s)
    {
        this.host = host;
        this.s = s;
        lastHit = Time.time;
    }

    public void NotifyHit() => lastHit = Time.time;

    public void Tick(float dt, float percentPerSecond)
    {
        if (Time.time - lastHit < s.regenDelayAfterHit) { accumulator = 0f; return; }
        if (host.Hp >= host.HpMax) return;

        accumulator += host.HpMax * percentPerSecond * dt;
        if (accumulator < 1f) return;

        int healed = Mathf.FloorToInt(accumulator);
        accumulator -= healed;
        host.Heal(healed);
        host.Flash(Color.Lerp(Color.white, Color.green, 0.5f), 0.12f);
    }
}

public sealed class RegeneracionAbility : AbilityBehaviour
{
    readonly RegenState regen;
    public RegeneracionAbility(ICoconutHost h, CoconutAbilitySettings s) : base(h, s) { regen = new RegenState(h, s); }
    public override void OnHit() => regen.NotifyHit();
    public override void Tick(float dt) => regen.Tick(dt, s.regenPercentPerSecond);
}

public sealed class CamuflajeAbility : AbilityBehaviour
{
    float timer;
    bool hidden;
    public CamuflajeAbility(ICoconutHost h, CoconutAbilitySettings s) : base(h, s) { }

    public override void Tick(float dt)
    {
        timer += dt;
        float limit = hidden ? s.camuflajeHiddenDuration : s.camuflajeVisibleDuration;
        if (timer < limit) return;
        timer = 0f;
        hidden = !hidden;
        host.SetAlpha(hidden ? s.camuflajeHiddenAlpha : 1f);
    }
}

public sealed class MaldicionAbility : AbilityBehaviour
{
    public MaldicionAbility(ICoconutHost h, CoconutAbilitySettings s) : base(h, s) { }

    public override void OnDeath()
    {
        if (GameManager.Instance != null) GameManager.Instance.ApplyTemporaryCurse(s.maldicionDuration);
        host.FloatText("¡MALDICION!", 1f);
    }
}

public sealed class SupremoAbility : AbilityBehaviour
{
    readonly RegenState regen;
    int phase = 1;
    public SupremoAbility(ICoconutHost h, CoconutAbilitySettings s) : base(h, s) { regen = new RegenState(h, s); }

    public override void OnHit() => regen.NotifyHit();

    public override void Tick(float dt)
    {
        float p = HpPercent;
        int newPhase = p <= s.supremoPhase3Hp ? 3 : (p <= s.supremoPhase2Hp ? 2 : 1);
        if (newPhase != phase)
        {
            phase = newPhase;
            host.FloatText("FASE " + phase, 1.2f);
            host.Flash(phase == 2 ? Color.green : Color.red, 0.3f);
        }
        if (phase == 2) regen.Tick(dt, s.supremoPhase2RegenPercent);
    }

    public override float ModifyDamage(float a, bool crit)
    {
        if (phase == 1) return a * (1f - s.supremoPhase1Reduction);
        if (phase == 3) return a * (1f - s.supremoPhase3Reduction);
        return a;
    }

    public override void OnDeath() => host.FloatText("¡DERROTADO!", 1.2f);
}

public static class AbilityFactory
{
    public static AbilityBehaviour Create(GameManager.CoconutAbility type, ICoconutHost h, CoconutAbilitySettings s)
    {
        switch (type)
        {
            case GameManager.CoconutAbility.Resistente: return new FlatReduction(h, s, s.resistenteReduction);
            case GameManager.CoconutAbility.Armadura: return new FlatReduction(h, s, s.armaduraReduction);
            case GameManager.CoconutAbility.FragilValioso: return new FragilValiosoAbility(h, s);
            case GameManager.CoconutAbility.FibraDura: return new FibraDuraAbility(h, s);
            case GameManager.CoconutAbility.Tenaz: return new TenazAbility(h, s);
            case GameManager.CoconutAbility.Rebote: return new ReboteAbility(h, s);
            case GameManager.CoconutAbility.Fortificado: return new FortificadoAbility(h, s);
            case GameManager.CoconutAbility.Implacable: return new ImplacableAbility(h, s);
            case GameManager.CoconutAbility.Regeneracion: return new RegeneracionAbility(h, s);
            case GameManager.CoconutAbility.Camuflaje: return new CamuflajeAbility(h, s);
            case GameManager.CoconutAbility.Maldicion: return new MaldicionAbility(h, s);
            case GameManager.CoconutAbility.Supremo: return new SupremoAbility(h, s);
            default: return new NoAbility(h, s);
        }
    }
}