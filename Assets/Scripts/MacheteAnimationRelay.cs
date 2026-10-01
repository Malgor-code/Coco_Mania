using UnityEngine;

public class MacheteAnimationRelay : MonoBehaviour
{
    public void AnimationImpact()
    {
        if (MacheteController.Instance != null)
            MacheteController.Instance.AnimationImpact();
    }
}