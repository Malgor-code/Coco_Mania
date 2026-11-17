using UnityEngine;

/// <summary>
/// Pasa al shader del entorno el nivel y las olas del agua para dibujar la marca de humedad.
/// Ponlo en el objeto del agua (o en cualquier objeto y asigna el Renderer).
/// </summary>
[ExecuteAlways]
public class WaterWetnessDriver : MonoBehaviour
{
    [Tooltip("Renderer del plano de agua (usa el material Stylized/Water URP).")]
    public Renderer waterRenderer;

    [Tooltip("Ajuste fino de la altura del agua en mundo (metros).")]
    public float levelOffset = 0f;

    static readonly int ID_Enabled = Shader.PropertyToID("_EnvWaterEnabled");
    static readonly int ID_Level = Shader.PropertyToID("_EnvWaterLevel");
    static readonly int ID_Wave = Shader.PropertyToID("_EnvWaterWave");
    static readonly int ID_Dir = Shader.PropertyToID("_EnvWaterDir");

    static readonly int ID_Amp = Shader.PropertyToID("_WaveAmplitude");
    static readonly int ID_Freq = Shader.PropertyToID("_WaveFrequency");
    static readonly int ID_Speed = Shader.PropertyToID("_WaveSpeed");
    static readonly int ID_WDir = Shader.PropertyToID("_WaveDirection");

    void Reset()
    {
        waterRenderer = GetComponent<Renderer>();
    }

    void OnEnable() { Apply(); }
    void OnDisable() { Shader.SetGlobalFloat(ID_Enabled, 0f); }

    void LateUpdate() { Apply(); }

    void Apply()
    {
        if (waterRenderer == null) waterRenderer = GetComponent<Renderer>();
        if (waterRenderer == null) { Shader.SetGlobalFloat(ID_Enabled, 0f); return; }

        Material m = waterRenderer.sharedMaterial;
        bool wavesOn = m != null && !m.IsKeywordEnabled("_WAVES_OFF");

        float amp = (wavesOn && m.HasProperty(ID_Amp)) ? m.GetFloat(ID_Amp) : 0f;
        float freq = (m != null && m.HasProperty(ID_Freq)) ? m.GetFloat(ID_Freq) : 1f;
        float speed = (m != null && m.HasProperty(ID_Speed)) ? m.GetFloat(ID_Speed) : 0f;
        Vector4 dir = (m != null && m.HasProperty(ID_WDir)) ? m.GetVector(ID_WDir) : new Vector4(1, 0, 0, 1);

        Shader.SetGlobalFloat(ID_Enabled, 1f);
        // bounds.center.y = altura real de la superficie (no depende de dónde esté el pivote)
        Shader.SetGlobalFloat(ID_Level, waterRenderer.bounds.center.y + levelOffset);
        Shader.SetGlobalVector(ID_Wave, new Vector4(amp, freq, speed, 0f));
        Shader.SetGlobalVector(ID_Dir, dir);
    }
}
