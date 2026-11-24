#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StoryDataTools
{
    const string CsvPath = "Assets/Resources/Localization/story.csv";
    const string DataFolder = "Assets/Data/Historia";
    const int LangCount = 6;
    static readonly string[] Suffix = { "ES", "EN", "PT", "FR", "DE", "JA" };

    class Row
    {
        public string key;
        public string[] values = new string[LangCount];
    }

    static readonly List<Row> rows = new List<Row>();

    [MenuItem("CocoMania/Historia/1. Generar CSV y dialogos desde StoryTexts")]
    static void Generate()
    {
        Type story = FindType("StoryTexts");
        Type gacha = FindType("GachaStoryTexts");
        if (story == null || gacha == null)
        {
            EditorUtility.DisplayDialog("Historia", "No encuentro StoryTexts / GachaStoryTexts. Este paso se hace ANTES de borrarlos.", "OK");
            return;
        }

        rows.Clear();
        EnsureFolder("Assets/Data");
        EnsureFolder(DataFolder);
        DialogueSO intro = MakeDialogue("Intro", AddBlock("story_intro", Langs(story, "Intro")));
        DialogueSO order = MakeDialogue("TutorialOrden", AddBlock("story_order", Langs(story, "Order")));
        DialogueSO deuda = MakeDialogue("TutorialDeuda", AddBlock("story_deuda", Langs(story, "Deuda")));
        DialogueSO upIntro = MakeDialogue("TutorialMejorasIntro", AddBlock("story_upintro", Langs(story, "UpIntro")));
        DialogueSO tree = MakeDialogue("TutorialArbol", AddBlock("story_tree", Langs(story, "Tree")));
        string[][][] chap = new string[LangCount][][];
        int chapterCount = 0;
        for (int l = 0; l < LangCount; l++)
        {
            chap[l] = GetStatic<string[][]>(story, "Chap" + Suffix[l]) ?? new string[0][];
            chapterCount = Mathf.Max(chapterCount, chap[l].Length);
        }

        var chapters = new List<DialogueSO>();
        for (int c = 0; c < chapterCount; c++)
        {
            var perLang = new string[LangCount][];
            bool any = false;
            for (int l = 0; l < LangCount; l++)
            {
                perLang[l] = c < chap[l].Length && chap[l][c] != null ? chap[l][c] : new string[0];
                if (perLang[l].Length > 0) any = true;
            }
            chapters.Add(any ? MakeDialogue("Capitulo_" + c.ToString("00"), AddBlock("story_cap" + c, perLang)) : null);
        }
        DialogueSO gachaIntro = MakeDialogue("Gacha_Intro", AddBlock("gacha_intro", ByLang(gacha, "IntroByLang")));
        DialogueSO gachaExplain = MakeDialogue("Gacha_Explicacion", AddBlock("gacha_explain", ByLang(gacha, "ExplainByLang")));
        WriteCsv();
        string catalogPath = DataFolder + "/StoryCatalog.asset";
        StoryCatalogSO catalog = AssetDatabase.LoadAssetAtPath<StoryCatalogSO>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<StoryCatalogSO>();
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }
        catalog.intro = intro;
        catalog.tutorialOrder = order;
        catalog.tutorialDeuda = deuda;
        catalog.tutorialUpgradeIntro = upIntro;
        catalog.tutorialTree = tree;
        catalog.chapters = chapters;
        catalog.gachaIntro = gachaIntro;
        catalog.gachaExplain = gachaExplain;
        EditorUtility.SetDirty(catalog);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = $"Lineas de texto generadas: {rows.Count} (x6 idiomas)\nCSV: {CsvPath}\nDialogos y catalogo: {DataFolder}\n\nSiguiente: aplica los cambios de Cambios_Fase6.md y ejecuta el paso 2.";
        Debug.Log("[StoryDataTools] " + msg);
        EditorUtility.DisplayDialog("Historia", msg, "OK");
    }

    [MenuItem("CocoMania/Historia/2. Asignar el catalogo al StoryManager")]
    static void AssignCatalog()
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<StoryManager>(FindObjectsInactive.Include);
        var catalog = AssetDatabase.LoadAssetAtPath<StoryCatalogSO>(DataFolder + "/StoryCatalog.asset");
        if (manager == null || catalog == null)
        {
            EditorUtility.DisplayDialog("Historia", "Falta el StoryManager en la escena o el catalogo (ejecuta el paso 1).", "OK");
            return;
        }

        var so = new SerializedObject(manager);
        SerializedProperty prop = so.FindProperty("storyCatalog");
        if (prop == null)
        {
            EditorUtility.DisplayDialog("Historia", "El StoryManager aun no tiene el campo 'storyCatalog'. Aplica primero los cambios de Cambios_Fase6.md.", "OK");
            return;
        }
        prop.objectReferenceValue = catalog;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        EditorUtility.DisplayDialog("Historia", "Catalogo asignado. Guarda la escena (Ctrl+S).", "OK");
    }

    static List<string> AddBlock(string prefix, string[][] perLang)
    {
        int n = 0;
        for (int l = 0; l < LangCount; l++) n = Mathf.Max(n, perLang[l] != null ? perLang[l].Length : 0);

        var keys = new List<string>();
        for (int i = 0; i < n; i++)
        {
            var row = new Row { key = prefix + "_" + (i + 1) };
            for (int l = 0; l < LangCount; l++)
                row.values[l] = perLang[l] != null && i < perLang[l].Length ? perLang[l][i] : "";
            rows.Add(row);
            keys.Add(row.key);
        }
        return keys;
    }

    static DialogueSO MakeDialogue(string assetName, List<string> keys)
    {
        string path = DataFolder + "/" + assetName + ".asset";
        DialogueSO so = AssetDatabase.LoadAssetAtPath<DialogueSO>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<DialogueSO>();
            AssetDatabase.CreateAsset(so, path);
        }
        so.lines = keys;
        EditorUtility.SetDirty(so);
        return so;
    }

    static void WriteCsv()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CsvPath));
        var sb = new StringBuilder();
        sb.Append("key,es,en,pt,fr,de,ja\n");
        foreach (Row r in rows)
        {
            sb.Append(Quote(r.key));
            for (int l = 0; l < LangCount; l++) sb.Append(',').Append(Quote(r.values[l] ?? ""));
            sb.Append('\n');
        }
        File.WriteAllText(CsvPath, sb.ToString(), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(CsvPath);
    }

    static string Quote(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
    static string[][] Langs(Type t, string baseName)
    {
        var result = new string[LangCount][];
        for (int l = 0; l < LangCount; l++) result[l] = GetStatic<string[]>(t, baseName + Suffix[l]) ?? new string[0];
        return result;
    }

    static string[][] ByLang(Type t, string field)
    {
        string[][] table = GetStatic<string[][]>(t, field);
        var result = new string[LangCount][];
        for (int l = 0; l < LangCount; l++) result[l] = table != null && l < table.Length && table[l] != null ? table[l] : new string[0];
        return result;
    }

    static T GetStatic<T>(Type t, string name) where T : class
    {
        FieldInfo f = t.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return f != null ? f.GetValue(null) as T : null;
    }

    static Type FindType(string name)
    {
        foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = a.GetType(name);
            if (t != null) return t;
        }
        return null;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
#endif