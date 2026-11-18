using UnityEngine;

[CreateAssetMenu(fileName = "Pasiva_Maldicion", menuName = "CocoMania/Pasivas/Maldicion")]
public class MaldicionPassiveSO : CoconutPassiveSO
{
    public float duration = 6f;

    public override ICoconutPassive Create(ICoconutHost host) => new Runtime(host, this);

    sealed class Runtime : CoconutPassiveBase
    {
        readonly MaldicionPassiveSO d;
        public Runtime(ICoconutHost h, MaldicionPassiveSO d) : base(h) { this.d = d; }

        public override void OnDeath()
        {
            StatsManager.Instance.ApplyTemporaryCurse(d.duration);
            host.FloatText("¡MALDICION!", 1f);
        }
    }
}