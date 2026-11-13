using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum Language { Spanish, English, Portuguese, French, German, Japanese }

[DefaultExecutionOrder(-1000)]
public class LocalizationManager : MonoBehaviour
{
    const string LangKey = "game_language";
    const int LangCount = 6;
    const string CsvPath = "Localization/localization";

    public static readonly string[] NativeNames =
    {
        "Español", "English", "Português", "Français", "Deutsch", "日本語"
    };

    private static LocalizationManager _instance;
    public static LocalizationManager Instance
    {
        get
        {
            if (_instance == null) _instance = FindObjectOfType<LocalizationManager>();
            return _instance;
        }
    }

    public static Language Current =>
        Instance != null ? Instance.currentLanguage : Language.Spanish;

    public static bool IsEnglish => Current == Language.English;

    public Language currentLanguage = Language.Spanish;
    public TMP_FontAsset japaneseFont;

    public System.Action OnLanguageChanged;

    private Dictionary<string, string[]> table = new Dictionary<string, string[]>();

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (PlayerPrefs.HasKey(LangKey))
        {
            int saved = PlayerPrefs.GetInt(LangKey, (int)currentLanguage);
            if (saved >= 0 && saved < LangCount) currentLanguage = (Language)saved;
        }

        BuildTable();
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public string Get(string key)
    {
        if (table.TryGetValue(key, out var entry))
        {
            string s = entry[(int)currentLanguage];
            if (string.IsNullOrEmpty(s)) s = entry[(int)Language.English];
            if (string.IsNullOrEmpty(s)) s = entry[(int)Language.Spanish];
            return s;
        }
        return key;
    }

    public string Get(string key, params object[] args)
    {
        string raw = Get(key);
        try { return string.Format(raw, args); }
        catch (System.FormatException) { return raw; }
    }

    public void SetLanguage(Language lang)
    {
        if (currentLanguage == lang) return;
        currentLanguage = lang;
        PlayerPrefs.SetInt(LangKey, (int)lang);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    public void SetLanguageIndex(int index)
    {
        if (index >= 0 && index < LangCount) SetLanguage((Language)index);
    }
    public void SetSpanish() { SetLanguage(Language.Spanish); }
    public void SetEnglish() { SetLanguage(Language.English); }
    public void SetPortuguese() { SetLanguage(Language.Portuguese); }
    public void SetFrench() { SetLanguage(Language.French); }
    public void SetGerman() { SetLanguage(Language.German); }
    public void SetJapanese() { SetLanguage(Language.Japanese); }
    public void NextLanguage() { SetLanguage((Language)(((int)currentLanguage + 1) % LangCount)); }

    void BuildTable()
    {
        table.Clear();
        var asset = Resources.Load<TextAsset>(CsvPath);
        if (asset == null)
        {
            Debug.LogError("No se encontró Resources/" + CsvPath + ".csv");
            return;
        }

        var rows = ParseCsv(asset.text);
        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0])) continue;

            var values = new string[LangCount];
            for (int c = 0; c < LangCount; c++)
                values[c] = (c + 1 < row.Count) ? row[c + 1] : "";

            table[row[0].Trim()] = values;
        }
        if (table.TryGetValue("menu_play", out var t))
            Debug.Log("JA = " + t[(int)Language.Japanese]);
    }

    static List<List<string>> ParseCsv(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;

        if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == ',') { row.Add(sb.ToString()); sb.Length = 0; }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(sb.ToString()); sb.Length = 0;
                rows.Add(row); row = new List<string>();
            }
            else sb.Append(c);
        }
        if (sb.Length > 0 || row.Count > 0) { row.Add(sb.ToString()); rows.Add(row); }
        return rows;
    }
}