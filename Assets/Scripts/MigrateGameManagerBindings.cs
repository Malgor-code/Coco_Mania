#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MigrateGameManagerBindings
{
    static readonly Dictionary<string, System.Type> MethodTargets = new Dictionary<string, System.Type>
    {
        { "ToggleUpgradeTreePanel", typeof(GameUI) },
        { "ToggleTiendaPanel", typeof(GameUI) },
        { "ToggleRewardMachinePanel", typeof(GameUI) },
        { "ToggleDeudaPanel", typeof(GameUI) },
        { "ChoosePerk", typeof(GameUI) },
        { "RecenterUpgradeTree", typeof(GameUI) },
        { "RefreshAllUI", typeof(GameUI) },
        { "ContinueToNextDay", typeof(RunManager) },
        { "PayDebt", typeof(RunManager) },
        { "DeclareBankruptcy", typeof(RunManager) },
        { "ContinueAfterBankruptcy", typeof(RunManager) },
        { "BeginNewGame", typeof(RunManager) },
        { "StartFreshRun", typeof(RunManager) },
        { "ResumeRun", typeof(RunManager) },
        { "PauseToMainMenu", typeof(RunManager) },
        { "RestartRun", typeof(RunManager) },
        { "GoToMainMenu", typeof(RunManager) },
        { "QuitGame", typeof(RunManager) },
        { "PauseGame", typeof(GameManager) },
        { "ResumeGame", typeof(GameManager) },
        { "BuyNextMachete", typeof(MacheteShopManager) },
    };

    [MenuItem("CocoMania/Migrar botones del GameManager viejo")]
    static void Run()
    {
        int migrated = 0, alreadyOk = 0;
        var unmapped = new HashSet<string>();
        var missingDest = new HashSet<string>();
        var cache = new Dictionary<System.Type, Component>();

        Component Find(System.Type t)
        {
            if (!cache.TryGetValue(t, out Component c))
            {
                c = Object.FindFirstObjectByType(t, FindObjectsInactive.Include) as Component;
                cache[t] = c;
            }
            return c;
        }

        foreach (Component comp in Resources.FindObjectsOfTypeAll<Component>())
        {
            if (comp == null || comp is Transform) continue;
            if (EditorUtility.IsPersistent(comp)) continue;
            if (!comp.gameObject.scene.IsValid()) continue;

            SerializedObject so = new SerializedObject(comp);
            bool changed = false;
            SerializedProperty it = so.GetIterator();
            while (it.Next(true))
            {
                if (!it.isArray || !it.propertyPath.EndsWith("m_PersistentCalls.m_Calls")) continue;

                for (int i = 0; i < it.arraySize; i++)
                {
                    SerializedProperty call = it.GetArrayElementAtIndex(i);
                    SerializedProperty typeProp = call.FindPropertyRelative("m_TargetAssemblyTypeName");
                    SerializedProperty methodProp = call.FindPropertyRelative("m_MethodName");
                    SerializedProperty targetProp = call.FindPropertyRelative("m_Target");
                    if (typeProp == null || methodProp == null || targetProp == null) continue;
                    if (!typeProp.stringValue.StartsWith("GameManager,")) continue;

                    string method = methodProp.stringValue;
                    if (!MethodTargets.TryGetValue(method, out System.Type destType)) { unmapped.Add(method); continue; }

                    Component dest = Find(destType);
                    if (dest == null) { missingDest.Add(destType.Name); continue; }

                    if (targetProp.objectReferenceValue == dest) { alreadyOk++; continue; }

                    targetProp.objectReferenceValue = dest;
                    typeProp.stringValue = destType.FullName + ", " + destType.Assembly.GetName().Name;
                    changed = true;
                    migrated++;
                }
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
            }
        }

        string report = $"Botones reconectados: {migrated}\nYa estaban bien: {alreadyOk}";
        if (missingDest.Count > 0) report += "\n\nFALTAN componentes en la escena: " + string.Join(", ", missingDest);
        if (unmapped.Count > 0) report += "\n\nMetodos sin destino (revisalos a mano): " + string.Join(", ", unmapped);
        Debug.Log("[MigrateGameManagerBindings]\n" + report);
        EditorUtility.DisplayDialog("Migracion de botones", report + "\n\nGuarda la escena (Ctrl+S).", "OK");
    }
}
#endif