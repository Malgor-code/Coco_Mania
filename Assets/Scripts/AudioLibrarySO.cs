using System.Collections.Generic;
using UnityEngine;

public static class SfxIds
{
    public const string UiHover = "ui_hover";
    public const string UiClick = "ui_click";
    public const string Purchase = "purchase";
    public const string DebtPaid = "debt_paid";
    public const string Coin = "coin";
    public const string Bankruptcy = "bankruptcy";

    public static readonly string[] All = { UiHover, UiClick, Purchase, DebtPaid, Coin, Bankruptcy };
}

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "CocoMania/Audio Library")]
public class AudioLibrarySO : ScriptableObject
{
    [System.Serializable]
    public class SfxEntry
    {
        public string id;
        [Tooltip("Si hay varios clips, se elige uno al azar.")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 0.3f)] public float pitchVariation = 0.05f;

        public AudioClip RandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }
    }

    public List<SfxEntry> sfx = new List<SfxEntry>();

    Dictionary<string, SfxEntry> map;

    public bool TryGet(string id, out SfxEntry entry)
    {
        if (map == null) Rebuild();
        return map.TryGetValue(id, out entry);
    }

    void Rebuild()
    {
        map = new Dictionary<string, SfxEntry>();
        foreach (var e in sfx)
            if (e != null && !string.IsNullOrEmpty(e.id)) map[e.id] = e;
    }

    void OnValidate() => map = null;

    [ContextMenu("Agregar los ids estandar")]
    void AddStandardIds()
    {
        foreach (string id in SfxIds.All)
        {
            bool exists = sfx.Exists(e => e != null && e.id == id);
            if (!exists) sfx.Add(new SfxEntry { id = id });
        }
        map = null;
    }
}