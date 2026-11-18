using UnityEngine;

[CreateAssetMenu(fileName = "Pasiva_Supremo", menuName = "CocoMania/Pasivas/Supremo (mini-jefe)")]
public class SupremoPassiveSO : CoconutPassiveSO
{
    [Range(0f, 1f)] public float phase2Hp = 0.7f;
    [Range(0f, 1f)] public float phase3Hp = 0.3f;
    [Range(0f, 1f)] public float phase1Reduction = 0.2f;
    [Range(0f, 1f)] public float phase3Reduction = 0.35f;
    [Range(0f, 0.2f)] public float phase2RegenPercent = 0.015f;
    public float regenDelayAfterHit = 2f;

    public override ICoconutPassive Create(ICoconutHost host) => new Runtime(host, this);

    sealed class Runtime : CoconutPassiveBase
    {
        readonly SupremoPassiveSO d;
        readonly CoconutRegen regen;
        int phase = 1;
        public Runtime(ICoconutHost h, SupremoPassiveSO d) : base(h) { this.d = d; regen = new CoconutRegen(h, d.regenDelayAfterHit); }

        public override void OnHit() => regen.NotifyHit();

        public override void Tick(float dt)
        {
            float p = HpPercent;
            int newPhase = p <= d.phase3Hp ? 3 : (p <= d.phase2Hp ? 2 : 1);
            if (newPhase != phase)
            {
                phase = newPhase;
                host.FloatText("FASE " + phase, 1.2f);
                host.Flash(phase == 2 ? Color.green : Color.red, 0.3f);
            }
            if (phase == 2) regen.Tick(dt, d.phase2RegenPercent);
        }

        public override float ModifyDamage(float a, bool crit)
        {
            if (phase == 1) return a * (1f - d.phase1Reduction);
            if (phase == 3) return a * (1f - d.phase3Reduction);
            return a;
        }

        public override void OnDeath() => host.FloatText("¡DERROTADO!", 1.2f);
    }
}