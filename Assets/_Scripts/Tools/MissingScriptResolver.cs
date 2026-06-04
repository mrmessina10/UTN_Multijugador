using UnityEditor;
using UnityEngine;

public class MissingScriptResolver
{
    [MenuItem("Tools/Remove Missing Scripts")]
    private static void FindAndRemoveMissingScripts()
    {
        int totalRemoved = 0;

        // 1. Analizar y limpiar todos los Prefabs del proyecto
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab);
                if (count > 0)
                {
                    int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
                    totalRemoved += removed;
                    Debug.Log($"Removido de Prefab: {path}");
                }
            }
        }

        // 2. Analizar y limpiar todos los objetos de la escena actual
        GameObject[] sceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject go in sceneObjects)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (count > 0)
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                totalRemoved += removed;
                EditorUtility.SetDirty(go);
                Debug.Log($"Removido de objeto en escena: {go.name}", go);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Análisis completo. Se eliminaron {totalRemoved} scripts faltantes.");
    }
}