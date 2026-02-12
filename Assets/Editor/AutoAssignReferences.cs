using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public static class AutoAssignReferences
{
    [MenuItem("Tools/CircuitCraft/Auto-Assign Scene References")]
    public static void AssignAll()
    {
        // --- 1. Load GridSettings SO ---
        var gridSettingsGuids = AssetDatabase.FindAssets("t:GridSettings");
        if (gridSettingsGuids.Length == 0)
        {
            Debug.LogError("[AutoAssign] No GridSettings asset found!");
            return;
        }
        var gridSettingsPath = AssetDatabase.GUIDToAssetPath(gridSettingsGuids[0]);
        var gridSettings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(gridSettingsPath);
        Debug.Log($"[AutoAssign] Found GridSettings: {gridSettingsPath}");

        // --- 2. Load ComponentDefinition SOs ---
        var compDefGuids = AssetDatabase.FindAssets("t:ComponentDefinition");
        var compDefs = compDefGuids
            .Select(g => AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(cd => cd != null)
            .ToArray();
        Debug.Log($"[AutoAssign] Found {compDefs.Length} ComponentDefinition assets");

        int assignedCount = 0;

        // --- 3. Assign GridSettings to all MonoBehaviours that have _gridSettings field ---
        string[] gridTargetTypes = { "PlacementController", "GridCursor", "GridRenderer", "BoardView", "UIController" };
        foreach (var typeName in gridTargetTypes)
        {
            var go = FindGameObjectWithComponent(typeName);
            if (go == null)
            {
                Debug.LogWarning($"[AutoAssign] Could not find GO with component: {typeName}");
                continue;
            }
            var comp = go.GetComponent(typeName);
            if (comp == null) continue;

            var so = new SerializedObject(comp);
            var prop = so.FindProperty("_gridSettings");
            if (prop == null)
            {
                Debug.LogWarning($"[AutoAssign] {typeName} has no _gridSettings property");
                continue;
            }
            prop.objectReferenceValue = gridSettings;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(comp);
            Debug.Log($"[AutoAssign] Assigned GridSettings to {typeName} on '{go.name}'");
            assignedCount++;
        }

        // --- 4. Assign ComponentDefinitions to SimulationManager ---
        {
            var go = FindGameObjectWithComponent("SimulationManager");
            if (go != null)
            {
                var comp = go.GetComponent("SimulationManager");
                if (comp != null)
                {
                    var so = new SerializedObject(comp);
                    var prop = so.FindProperty("_componentDefinitions");
                    if (prop != null && prop.isArray)
                    {
                        prop.arraySize = compDefs.Length;
                        for (int i = 0; i < compDefs.Length; i++)
                        {
                            prop.GetArrayElementAtIndex(i).objectReferenceValue = compDefs[i];
                        }
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(comp);
                        Debug.Log($"[AutoAssign] Assigned {compDefs.Length} ComponentDefinitions to SimulationManager");
                        assignedCount++;
                    }
                }
            }
            else
            {
                Debug.LogWarning("[AutoAssign] Could not find SimulationManager");
            }
        }

        // --- 5. Assign ComponentDefinitions to ComponentPaletteController ---
        {
            var go = FindGameObjectWithComponent("ComponentPaletteController");
            if (go != null)
            {
                var comp = go.GetComponent("ComponentPaletteController");
                if (comp != null)
                {
                    var so = new SerializedObject(comp);
                    var prop = so.FindProperty("_componentDefinitions");
                    if (prop != null && prop.isArray)
                    {
                        prop.arraySize = compDefs.Length;
                        for (int i = 0; i < compDefs.Length; i++)
                        {
                            prop.GetArrayElementAtIndex(i).objectReferenceValue = compDefs[i];
                        }
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(comp);
                        Debug.Log($"[AutoAssign] Assigned {compDefs.Length} ComponentDefinitions to ComponentPaletteController");
                        assignedCount++;
                    }
                }
            }
            else
            {
                Debug.LogWarning("[AutoAssign] Could not find ComponentPaletteController");
            }
        }

        // --- 6. Save scene ---
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[AutoAssign] Complete! {assignedCount} assignments made. Scene saved.");
    }

    private static GameObject FindGameObjectWithComponent(string typeName)
    {
        var allMBs = Object.FindObjectsOfType<MonoBehaviour>();
        foreach (var mb in allMBs)
        {
            if (mb.GetType().Name == typeName)
                return mb.gameObject;
        }
        return null;
    }
}
