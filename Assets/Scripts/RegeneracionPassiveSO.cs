using UnityEngine;

[CreateAssetMenu(fileName = "Pasiva_Regeneracion", menuName = "CocoMania/Pasivas/Regeneracion")]
public class RegeneracionPassiveSO : CoconutPassiveSO
{
    public float delayAfterHit = 2f;
    [Range(0f, 0.2f)] public float percentPerSecond = 0.03f;

    public override ICoconutPassive Create(ICoconutHost host) => new Runtime(host, this);

    sealed class Runtime : CoconutPassiveBase
    {
        readonly RegeneracionPassiveSO d;
        readonly CoconutRegen regen;
        public Runtime(ICoconutHost h, RegeneracionPassiveSO d) : base(h) { this.d = d; regen = new CoconutRegen(h, d.delayAfterHit); }

        public override void OnHit() => regen.NotifyHit();
        public override void Tick(float dt) => regen.Tick(dt, d.percentPerSecond);
    }
}