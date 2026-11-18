using UnityEngine;

[CreateAssetMenu(fileName = "Pasiva_Fortificado", menuName = "CocoMania/Pasivas/Fortificado")]
public class FortificadoPassiveSO : CoconutPassiveSO
{
    public float normalDuration = 4f;
    public float buffDuration = 3f;
    [Range(0f, 1f)] public float reduction = 0.4f;
    [Range(0f, 1f)] public float darken = 0.45f;

    public override ICoconutPassive Create(ICoconutHost host) => new Runtime(host, this);

    sealed class Runtime : CoconutPassiveBase
    {
        readonly FortificadoPassiveSO d;
        float timer;
        bool active;
        public Runtime(ICoconutHost h, FortificadoPassiveSO d) : base(h) { this.d = d; }

        public override void Tick(float dt)
        {
            timer += dt;
            float cycle = d.normalDuration + d.buffDuration;
            bool shouldBeActive = timer % cycle >= d.normalDuration;
            if (shouldBeActive == active) return;
            active = shouldBeActive;
            host.SetDarken(active ? d.darken : 0f);
        }

        public override float ModifyDamage(float a, bool crit) => active ? a * (1f - d.reduction) : a;
    }
}