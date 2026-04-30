#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DungeonBlade.EditorTools
{
    /// <summary>
    /// Scrubs "Missing Script" components from prefabs that survived a
    /// nuclear folder reset. Run after deleting script folders and
    /// before rebuilding scenes.
    /// </summary>
    public static class DBPrefabCleanup
    {
        [MenuItem("DungeonBlade/Maintenance/Strip Missing Scripts From All Prefabs")]
        public static void StripAll()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            int totalRemoved = 0;
            int prefabsTouched = 0;

            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var prefab = PrefabUtility.LoadPrefabContents(path);
                if (prefab == null) continue;

                int removedThisPrefab = RemoveMissingRecursive(prefab);
                if (removedThisPrefab > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    Debug.Log($"[DBPrefabCleanup] Removed {removedThisPrefab} missing script(s) from {path}");
                    totalRemoved += removedThisPrefab;
                    prefabsTouched++;
                }
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("DungeonBlade",
                $"Stripped {totalRemoved} missing script component(s) from {prefabsTouched} prefab(s).",
                "OK");
        }

        static int RemoveMissingRecursive(GameObject go)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            foreach (Transform child in go.transform)
                removed += RemoveMissingRecursive(child.gameObject);
            return removed;
        }
    }
}
#endif
