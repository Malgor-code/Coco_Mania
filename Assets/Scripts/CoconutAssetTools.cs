#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class CoconutAssetTools
{
    const string Root = "Assets/Data/Cocos";
    const string PassiveFolder = Root + "/Pasivas";

    static readonly string[] Names =
    {
        "Coco Verde", "Coco Maduro", "Coco Correoso", "Coco Fibroso", "Coco Petreo", "Coco Curtido", "Coco Blindado",
        "Coco de Hierro", "Coco de Acero", "Coco de Titanio", "Coco de Diamante", "Coco Legendario", "Coco Mitico",
        "Coco Ancestral", "Coco Supremo"
    };

    static readonly float[] BaseHp = { 1f, 2.2f, 6f, 12f, 20f, 26f, 34f, 46f, 58f, 70f, 80f, 98f, 118f, 145f, 165f };
    static readonly int[] UnlockCycle = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 16 };

    static readonly string[] AbilityNames =
    {
        "Sin habilidad", "Generoso", "Resistente", "Fibra Dura", "Pesado",
        "Tenaz", "Armadura", "Rebote", "Fortificado", "Implacable",
        "Fragil y Valioso", "Regeneracion", "Camuflaje", "Maldicion", "Supremo"
    };

    static readonly string[] AbilityDescs =
    {
        "Un coco normal. Sin trucos.",
        "Suelta mas agua que los cocos comunes.",
        "Recibe 30% menos de daño de los golpes normales. Los criticos lo ignoran.",
        "Cada golpe seguido lo endurece (hasta -25% de daño). Si lo dejas respirar 1.5 s, se reinicia.",
        "Grande y pesado: casi no se mueve al ser golpeado.",
        "Con menos de 30% de vida se aferra y recibe 25% menos de daño.",
        "Recibe 50% menos de daño de los golpes normales. Los criticos lo ignoran.",
        "Cada 5 golpes que recibe, te quita 2 de resistencia.",
        "Cada 4 segundos se blinda durante 3 s y recibe 40% menos de daño.",
        "Si lo golpeas 4 veces en 2 s, se protege 2 s y recibe 30% menos de daño.",
        "Recibe 75% menos de daño normal, pero los criticos le pegan x4.",
        "Si pasa 2 s sin recibir golpes, recupera 3% de su vida por segundo.",
        "Se vuelve casi invisible durante 2 s de cada 5.",
        "Al romperlo lanza una maldicion temporal que te debilita unos segundos.",
        "Mini-jefe de 3 fases: empieza con 20% menos de daño, a 70% de vida se regenera y a 30% se endurece (-35% de daño)."
    };

    static readonly string[] Phrases =
    {
        "Joven, terco y siempre listo para su primer golpe.",
        "Ya probo el sol. Dicen que su agua sabe a gloria.",
        "Mas duro de roer que una suela vieja.",
        "Cada fibra cuenta una historia de machetazos.",
        "No es un coco, es una piedra con aires de grandeza.",
        "Ha visto mil dias de sol y ningun machete lo asusta.",
        "Su cascara ha desafiado mas filos que un herrero.",
        "Suena a campana cuando lo golpeas. No es buena señal.",
        "Se endurece justo cuando crees que ya ganaste.",
        "Nadie lo golpea dos veces sin que se ponga serio.",
        "Brilla de mas. Cuidado con presumirlo... o con pegarle mal.",
        "Los viejos jurabamos que ya lo habiamos cortado.",
        "Ahora lo ves, ahora no.",
        "Guarda rencores de generaciones de cocoteros.",
        "El rey del cocotal. No se inclina ante nadie."
    };

    static readonly CoconutWander.MoveStyle[] Styles =
    {
        CoconutWander.MoveStyle.Despreocupado, CoconutWander.MoveStyle.Perezoso, CoconutWander.MoveStyle.Terco,
        CoconutWander.MoveStyle.Nervioso, CoconutWander.MoveStyle.Rodante, CoconutWander.MoveStyle.Persistente,
        CoconutWander.MoveStyle.Vigilante, CoconutWander.MoveStyle.Saltarin, CoconutWander.MoveStyle.Robotico,
        CoconutWander.MoveStyle.Implacable, CoconutWander.MoveStyle.Asustadizo, CoconutWander.MoveStyle.Huidizo,
        CoconutWander.MoveStyle.Sigiloso, CoconutWander.MoveStyle.Embrujado, CoconutWander.MoveStyle.Jefe
    };

    static readonly string[] PassiveKey =
    {
        "", "", "Resistente", "FibraDura", "", "Tenaz", "Armadura", "Rebote", "Fortificado", "Implacable",
        "FragilValioso", "Regeneracion", "Camuflaje", "Maldicion", "Supremo"
    };

    [MenuItem("CocoMania/Cocos/1. Crear assets desde los datos actuales")]
    static void CreateAssets()
    {
        EnsureFolder("Assets/Data");
        EnsureFolder(Root);
        EnsureFolder(PassiveFolder);

        var passives = CreatePassives();
        int created = 0, filled = 0;

        for (int i = 0; i < Names.Length; i++)
        {
            string path = Root + "/" + i.ToString("00") + "_" + Names[i].Replace(" ", "") + ".asset";
            CoconutTypeSO so = AssetDatabase.LoadAssetAtPath<CoconutTypeSO>(path);
            bool isNew = so == null;
            if (isNew)
            {
                so = ScriptableObject.CreateInstance<CoconutTypeSO>();
                so.typeName = Names[i];
                so.baseHpMultiplier = BaseHp[i];
                so.unlockCycle = UnlockCycle[i];
                so.phrase = Phrases[i];
                so.abilityName = AbilityNames[i];
                so.abilityDescription = AbilityDescs[i];
                so.moveStyle = Styles[i];
                so.passive = PassiveKey[i] != "" && passives.TryGetValue(PassiveKey[i], out CoconutPassiveSO p) ? p : null;
                AssetDatabase.CreateAsset(so, path);
                created++;
            }

            if (so.prefab == null) { so.prefab = OldPrefab(i); if (so.prefab != null) filled++; }
            if (so.icon == null) { so.icon = OldIcon(i); if (so.icon != null) filled++; }
            EditorUtility.SetDirty(so);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        string msg = $"Assets de cocos creados: {created}\nPrefabs/iconos copiados de tu escena: {filled}\nCarpeta: {Root}";
        Debug.Log("[CoconutAssetTools] " + msg);
        EditorUtility.DisplayDialog("Cocos", msg, "OK");
    }

    [MenuItem("CocoMania/Cocos/2. Asignar los tipos al CoconutSpawner")]
    static void AssignToSpawner()
    {
        var spawner = Object.FindFirstObjectByType<CoconutSpawner>(FindObjectsInactive.Include);
        if (spawner == null)
        {
            EditorUtility.DisplayDialog("Cocos", "No hay un CoconutSpawner en la escena abierta.", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:CoconutTypeSO", new[] { Root });
        var paths = new List<string>();
        foreach (string g in guids) paths.Add(AssetDatabase.GUIDToAssetPath(g));
        paths.Sort(System.StringComparer.Ordinal);

        var so = new SerializedObject(spawner);
        SerializedProperty list = so.FindProperty("coconutTypes");
        if (list == null || !list.isArray)
        {
            EditorUtility.DisplayDialog("Cocos", "El CoconutSpawner aun es la version vieja (no tiene la lista de CoconutTypeSO). Reemplaza el script primero.", "OK");
            return;
        }

        list.arraySize = paths.Count;
        for (int i = 0; i < paths.Count; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<CoconutTypeSO>(paths[i]);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);

        string msg = $"Tipos asignados al CoconutSpawner: {paths.Count}\nGuarda la escena (Ctrl+S).";
        Debug.Log("[CoconutAssetTools] " + msg);
        EditorUtility.DisplayDialog("Cocos", msg, "OK");
    }

    static Dictionary<string, CoconutPassiveSO> CreatePassives()
    {
        var map = new Dictionary<string, CoconutPassiveSO>();

        var resistente = Passive<FlatReductionPassiveSO>("Resistente", map); resistente.reduction = 0.30f; EditorUtility.SetDirty(resistente);
        var armadura = Passive<FlatReductionPassiveSO>("Armadura", map); armadura.reduction = 0.50f; EditorUtility.SetDirty(armadura);
        Passive<FibraDuraPassiveSO>("FibraDura", map);
        Passive<TenazPassiveSO>("Tenaz", map);
        Passive<RebotePassiveSO>("Rebote", map);
        Passive<FortificadoPassiveSO>("Fortificado", map);
        Passive<ImplacablePassiveSO>("Implacable", map);
        Passive<FragilValiosoPassiveSO>("FragilValioso", map);
        Passive<RegeneracionPassiveSO>("Regeneracion", map);
        Passive<CamuflajePassiveSO>("Camuflaje", map);
        Passive<MaldicionPassiveSO>("Maldicion", map);
        Passive<SupremoPassiveSO>("Supremo", map);
        return map;
    }

    static T Passive<T>(string key, Dictionary<string, CoconutPassiveSO> map) where T : CoconutPassiveSO
    {
        string path = PassiveFolder + "/Pasiva_" + key + ".asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        map[key] = asset;
        return asset;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        string name = path.Substring(slash + 1);
        AssetDatabase.CreateFolder(parent, name);
    }
    static GameObject OldPrefab(int i)
    {
        var spawner = Object.FindFirstObjectByType<CoconutSpawner>(FindObjectsInactive.Include);
        if (spawner != null)
        {
            var list = new SerializedObject(spawner).FindProperty("coconutTypes");
            if (list != null && list.isArray && i < list.arraySize)
            {
                var prefab = list.GetArrayElementAtIndex(i).FindPropertyRelative("prefab");
                if (prefab != null && prefab.objectReferenceValue is GameObject go) return go;
            }
        }
        var ui = Object.FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
        if (ui != null)
        {
            var list = new SerializedObject(ui).FindProperty("coconutUnlocks");
            if (list != null && list.isArray && i < list.arraySize)
            {
                var prefab = list.GetArrayElementAtIndex(i).FindPropertyRelative("prefab");
                if (prefab != null && prefab.objectReferenceValue is GameObject go) return go;
            }
        }
        return null;
    }

    static Sprite OldIcon(int i)
    {
        var ui = Object.FindFirstObjectByType<GameUI>(FindObjectsInactive.Include);
        if (ui == null) return null;
        var list = new SerializedObject(ui).FindProperty("coconutUnlocks");
        if (list == null || !list.isArray || i >= list.arraySize) return null;
        var icon = list.GetArrayElementAtIndex(i).FindPropertyRelative("icon");
        return icon != null ? icon.objectReferenceValue as Sprite : null;
    }
}
#endif