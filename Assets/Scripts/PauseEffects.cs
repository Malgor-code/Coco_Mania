using System.Collections.Generic;
using UnityEngine;

public class PauseEffects : MonoBehaviour
{
    readonly List<(Animator anim, float speed)> pausedAnimators = new List<(Animator, float)>();
    readonly List<ParticleSystem> pausedParticles = new List<ParticleSystem>();
    SimulationMode originalSimulationMode;
    bool applied;

    void OnEnable() => EventBus.Subscribe<GameStateChanged>(OnStateChanged);

    void OnDisable()
    {
        EventBus.Unsubscribe<GameStateChanged>(OnStateChanged);
        if (applied) Release();
    }

    void OnStateChanged(GameStateChanged e)
    {
        if (e.Current == GameState.OnPause && !applied) Freeze();
        else if (e.Current != GameState.OnPause && applied) Release();
    }

    void Freeze()
    {
        applied = true;

        originalSimulationMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script; 

        foreach (Animator a in FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            if (a.updateMode == AnimatorUpdateMode.UnscaledTime) continue;
            pausedAnimators.Add((a, a.speed));
            a.speed = 0f;
        }

        foreach (ParticleSystem ps in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            if (!ps.isPlaying || ps.main.useUnscaledTime) continue;
            ps.Pause(false);
            pausedParticles.Add(ps);
        }
    }

    void Release()
    {
        applied = false;

        Physics.simulationMode = originalSimulationMode;

        foreach (var p in pausedAnimators)
            if (p.anim != null) p.anim.speed = p.speed;
        pausedAnimators.Clear();

        foreach (ParticleSystem ps in pausedParticles)
            if (ps != null) ps.Play(false);
        pausedParticles.Clear();
    }
}