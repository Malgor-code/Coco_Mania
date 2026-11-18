using UnityEngine;

[CreateAssetMenu(fileName = "Pasiva_Rebote", menuName = "CocoMania/Pasivas/Rebote")]
public class RebotePassiveSO : CoconutPassiveSO
{
    public int hitsPerTrigger = 5;
    public float staminaPenalty = 2f;

    public override ICoconutPassive Create(ICoconutHost host) => new Runtime(host, this);

    sealed class Runtime : CoconutPassiveBase
    {
        readonly RebotePassiveSO d;
        int hits;
        public Runtime(ICoconutHost h, RebotePassiveSO d) : base(h) { this.d = d; }

        public override void OnHit()
        {
            if (++hits < d.hitsPerTrigger) return;
            hits = 0;
            StaminaManager.Instance.ApplyPenalty(d.staminaPenalty);
            host.FloatText("¡REBOTE!", 0.8f);
            host.Flash(Color.yellow, 0.2f);
        }
    }
}