using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Pasiva_Implacable", menuName = "CocoMania/Pasivas/Implacable")]
public class ImplacablePassiveSO : CoconutPassiveSO
{
    public int hitsToTrigger = 4;
    public float window = 2f;
    public float buffDuration = 2f;
    [Range(0f, 1f)] public float reduction = 0.3f;

    public override ICoconutPassive Create(ICoconutHost host) => new Runtime(host, this);

    sealed class Runtime : CoconutPassiveBase
    {
        readonly ImplacablePassiveSO d;
        readonly Queue<float> recentHits = new Queue<float>();
        float buffUntil = -999f;
        public Runtime(ICoconutHost h, ImplacablePassiveSO d) : base(h) { this.d = d; }

        public override void OnHit() => recentHits.Enqueue(Time.time);

        public override void Tick(float dt)
        {
            float cutoff = Time.time - d.window;
            while (recentHits.Count > 0 && recentHits.Peek() < cutoff) recentHits.Dequeue();

            if (recentHits.Count >= d.hitsToTrigger && Time.time > buffUntil)
            {
                buffUntil = Time.time + d.buffDuration;
                host.Flash(Color.cyan, 0.15f);
            }
        }

        public override float ModifyDamage(float a, bool crit) => Time.time <= buffUntil ? a * (1f - d.reduction) : a;
    }
}