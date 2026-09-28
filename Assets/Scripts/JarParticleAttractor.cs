using UnityEngine;
using System.Collections.Generic;

public class JarParticleAttractor : MonoBehaviour
{
    private static ParticleSystem.Particle[] sBuffer = new ParticleSystem.Particle[256];

    private WaterJar3D jar;
    private ParticleSystem[] systems;
    private readonly HashSet<uint> arrived = new HashSet<uint>();
    private float totalML;
    private float pendingML;
    private int maxSeen = 1;
    private float startTime;
    private bool finished;

    public void Init(WaterJar3D jarRef, ParticleSystem[] systemsRef, float waterML)
    {
        jar = jarRef;
        systems = systemsRef;
        totalML = waterML;
        pendingML = waterML;
        startTime = Time.time;
        jar.RegisterFlight();
    }

    void LateUpdate()
    {
        if (finished || jar == null) return;

        float dt = Time.deltaTime;

        int seenNow = 0;
        for (int si = 0; si < systems.Length; si++)
            if (systems[si] != null) seenNow += systems[si].particleCount;
        if (seenNow > maxSeen) maxSeen = seenNow;

        for (int si = 0; si < systems.Length; si++)
        {
            ParticleSystem ps = systems[si];
            if (ps == null) continue;

            int need = ps.particleCount;
            if (need == 0) continue;
            if (sBuffer.Length < need) sBuffer = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(need)];

            int n = ps.GetParticles(sBuffer);
            bool local = ps.main.simulationSpace == ParticleSystemSimulationSpace.Local;
            Transform psT = ps.transform;
            bool changed = false;

            for (int i = 0; i < n; i++)
            {
                ParticleSystem.Particle p = sBuffer[i];

                float age = p.startLifetime - p.remainingLifetime;
                if (age < jar.particleAttractDelay) continue;

                Vector3 targetWorld = jar.GetLandingPoint(p.randomSeed);
                Vector3 target = local ? psT.InverseTransformPoint(targetWorld) : targetWorld;
                Vector3 worldPos = local ? psT.TransformPoint(p.position) : p.position;

                float worldDist = Vector3.Distance(worldPos, targetWorld);
                float attractAge = age - jar.particleAttractDelay;
                float speed = Mathf.Lerp(jar.particleMinSpeed, jar.particleMaxSpeed,
                                         Mathf.Clamp01(attractAge / Mathf.Max(0.01f, jar.particleAccelTime)));

                if (worldDist <= jar.particleArriveDistance || worldDist <= speed * dt)
                {
                    if (arrived.Add(p.randomSeed))
                    {
                        float share = Mathf.Min(pendingML, totalML / Mathf.Max(1, maxSeen));
                        pendingML -= share;
                        jar.NotifyArrival(targetWorld, share);
                    }
                    p.remainingLifetime = 0f;
                    sBuffer[i] = p;
                    changed = true;
                    continue;
                }

                Vector3 dir = (target - p.position).normalized;
                float k = 1f - Mathf.Exp(-jar.particleSteering * dt);
                p.velocity = Vector3.Lerp(p.velocity, dir * speed, k);
                const float minRemain = 0.5f;
                if (p.remainingLifetime < minRemain)
                {
                    float add = minRemain - p.remainingLifetime;
                    p.remainingLifetime += add;
                    p.startLifetime += add;
                }

                sBuffer[i] = p;
                changed = true;
            }

            if (changed) ps.SetParticles(sBuffer, n);
        }

        float elapsed = Time.time - startTime;
        bool anyAlive = false;
        for (int si = 0; si < systems.Length; si++)
        {
            if (systems[si] != null && systems[si].IsAlive(false)) { anyAlive = true; break; }
        }

        if ((!anyAlive && elapsed > 0.2f) || elapsed > jar.particleMaxLifetime)
        {
            Finish();
        }
    }

    void Finish()
    {
        if (finished) return;
        finished = true;
        if (pendingML > 0.01f && jar != null) jar.NotifyArrival(jar.TargetPosition(), pendingML);
        if (jar != null) jar.UnregisterFlight();

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (!finished && jar != null) jar.UnregisterFlight();
    }
}