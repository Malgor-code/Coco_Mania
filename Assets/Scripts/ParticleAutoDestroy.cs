using UnityEngine;

public class ParticleAutoDestroy : MonoBehaviour
{
    void Start()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        float lifetime = 2f;

        if (ps != null)
        {
            lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
        }

        Destroy(gameObject, lifetime);
    }
}