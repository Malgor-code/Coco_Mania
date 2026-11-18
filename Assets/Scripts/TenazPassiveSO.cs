using UnityEngine;

[CreateAssetMenu(fileName = "Pasiva_Tenaz", menuName = "CocoMania/Pasivas/Tenaz")]
public class TenazPassiveSO : CoconutPassiveSO
{
    [Range(0f, 1f)] public float hpThreshold = 0.3f;
    [Range(0f, 1f)] public float reduction = 0.25f;

    public override ICoconutPassive Create(ICoconutHost host) => new Runtime(host, this);

    sealed class Runtime : CoconutPassiveBase
    {
        readonly TenazPassiveSO d;
        public Runtime(ICoconutHost h, TenazPassiveSO d) : base(h) { this.d = d; }
        public override float ModifyDamage(float a, bool crit) => HpPercent <= d.hpThreshold ? a * (1f - d.reduction) : a;
    }
}