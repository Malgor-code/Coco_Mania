using UnityEngine;

[CreateAssetMenu(fileName = "Pasiva_FragilValioso", menuName = "CocoMania/Pasivas/Fragil y Valioso")]
public class FragilValiosoPassiveSO : CoconutPassiveSO
{
    [Range(0f, 1f)] public float normalReduction = 0.75f;
    public float critMultiplier = 4f;

    public override ICoconutPassive Create(ICoconutHost host) => new Runtime(host, this);

    sealed class Runtime : CoconutPassiveBase
    {
        readonly FragilValiosoPassiveSO d;
        public Runtime(ICoconutHost h, FragilValiosoPassiveSO d) : base(h) { this.d = d; }
        public override float ModifyDamage(float a, bool crit) => crit ? a * d.critMultiplier : a * (1f - d.normalReduction);
    }
}