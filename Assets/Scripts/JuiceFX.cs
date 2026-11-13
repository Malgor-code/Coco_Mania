using UnityEngine;

public static class JuiceFX
{
    static Material mat;
    static Texture2D circleTex;

    public static int DefaultLayer = 0;

    static Texture2D GetCircle()
    {
        if (circleTex != null) return circleTex;
        const int size = 64;
        circleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        circleTex.wrapMode = TextureWrapMode.Clamp;
        circleTex.filterMode = FilterMode.Bilinear;
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.75f, 1f, d));
                circleTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        circleTex.Apply();
        return circleTex;
    }

    static Material GetMat()
    {
        if (mat != null) return mat;
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) { Debug.LogError("JuiceFX: no se encontró ningún shader para partículas."); return null; }
        mat = new Material(sh);
        Texture2D tex = GetCircle();
        mat.mainTexture = tex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        mat.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
        if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        mat.renderQueue = 3500;
        return mat;
    }

    public static void Burst(Vector3 pos, Color color, int count, Vector3 dir,
                             float scale = 1f, float speed = 4f, float size = 0.14f,
                             float lifetime = 0.65f, float gravity = 1.5f,
                             float cone = 55f, float speedMul = 1f, int layer = -1)
    {
        if (count <= 0) return;

        var go = new GameObject("JuiceBurst");
        go.layer = layer >= 0 ? layer : DefaultLayer;
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.up);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * scale * 0.5f * speedMul, speed * scale * speedMul);
        main.startSize = new ParticleSystem.MinMaxCurve(size * scale * 0.6f, size * scale);
        main.startColor = color;
        main.gravityModifier = gravity * scale;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = cone;
        shape.radius = 0.05f * scale;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;

        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = GetMat();
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        psr.receiveShadows = false;
        psr.sortingOrder = 500;

        ps.Play();
    }
}