using UnityEngine;

[CreateAssetMenu(fileName = "Pasiva_Camuflaje", menuName = "CocoMania/Pasivas/Camuflaje")]
public class CamuflajePassiveSO : CoconutPassiveSO
{
    public float visibleDuration = 3f;
    public float hiddenDuration = 2f;
    [Range(0f, 1f)] public float hiddenAlpha = 0.2f;

    public override ICoconutPassive Create(ICoconutHost host) => new Runtime(host, this);

    sealed class Runtime : CoconutPassiveBase
    {
        readonly CamuflajePassiveSO d;
        float timer;
        bool hidden;
        public Runtime(ICoconutHost h, CamuflajePassiveSO d) : base(h) { this.d = d; }

        public override void Tick(float dt)
        {
            timer += dt;
            float limit = hidden ? d.hiddenDuration : d.visibleDuration;
            if (timer < limit) return;
            timer = 0f;
            hidden = !hidden;
            host.SetAlpha(hidden ? d.hiddenAlpha : 1f);
        }
    }
}