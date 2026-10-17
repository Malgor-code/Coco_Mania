using System.Collections.Generic;
using UnityEngine;

public abstract class MinigameBase
{
    public abstract string Name { get; }
    public abstract string[] Intros { get; }
    public abstract string Controls { get; }
    public virtual float Duration => 0f;
    public virtual string Hud => "";
    public abstract float Score01 { get; }
    public virtual bool Is3D => false;

    public bool Finished { get; protected set; }
    public float TimeLeft => timeLeft;

    protected RectTransform root;
    protected float diff = 1f;
    protected float timeLeft;

    protected MinigameStage Stage => MinigameStage.Instance;
    readonly List<GameObject> spawned = new List<GameObject>();

    public void Begin(RectTransform area, float difficulty)
    {
        root = area;
        diff = Mathf.Max(1f, difficulty);
        timeLeft = Duration;
        Finished = false;
        Setup();
    }

    public void Update(float dt)
    {
        if (Finished) return;
        Tick(dt);
        if (Duration > 0f)
        {
            timeLeft -= dt;
            if (timeLeft <= 0f) { timeLeft = 0f; Finished = true; }
        }
    }

    protected abstract void Setup();
    protected abstract void Tick(float dt);

    protected GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        var go = Object.Instantiate(prefab, pos, rot);
        spawned.Add(go);
        return go;
    }

    public virtual void Cleanup()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i] != null) Object.Destroy(spawned[i]);
        spawned.Clear();

        if (MinigameStage.Instance != null) MinigameStage.Instance.ReleaseCamera();

        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            Object.Destroy(root.GetChild(i).gameObject);
    }
    protected float Scale(float perPoint) => 1f + perPoint * (diff - 1f);
}