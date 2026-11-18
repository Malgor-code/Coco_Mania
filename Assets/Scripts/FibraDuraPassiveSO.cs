using UnityEngine;

[CreateAssetMenu(fileName = "Pasiva_FibraDura", menuName = "CocoMania/Pasivas/Fibra Dura")]
public class FibraDuraPassiveSO : CoconutPassiveSO
{
    public float stackPerHit = 0.05f;
    public float maxStack = 0.25f;
    public float resetTime = 1.5f;

    public override ICoconutPassive Create(ICoconutHost host) => new Runtime(host, this);

    sealed class Runtime : CoconutPassiveBase
    {
        readonly FibraDuraPassiveSO d;
        float stack, lastHit = -999f;
        public Runtime(ICoconutHost h, FibraDuraPassiveSO d) : base(h) { this.d = d; }

        public override float ModifyDamage(float a, bool crit)
        {
            if (Time.time - lastHit > d.resetTime) stack = 0f;
            float mult = 1f - Mathf.Min(stack, d.maxStack);
            stack += d.stackPerHit;
            lastHit = Time.time;
            return a * mult;
        }
    }
}