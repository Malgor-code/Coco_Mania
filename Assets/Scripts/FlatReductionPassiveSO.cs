using UnityEngine;

[CreateAssetMenu(fileName = "Pasiva_ReduccionPlana", menuName = "CocoMania/Pasivas/Reduccion Plana")]
public class FlatReductionPassiveSO : CoconutPassiveSO
{
    [Range(0f, 1f)] public float reduction = 0.30f;

    public override ICoconutPassive Create(ICoconutHost host) => new Runtime(host, this);

    sealed class Runtime : CoconutPassiveBase
    {
        readonly FlatReductionPassiveSO d;
        public Runtime(ICoconutHost h, FlatReductionPassiveSO d) : base(h) { this.d = d; }
        public override float ModifyDamage(float a, bool crit) => crit ? a : a * (1f - d.reduction);
    }
}