#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DungeonBlade.Core.EditorTools
{
    [InitializeOnLoad]
    static class BuildSceneRegistrar
    {
        static readonly string[] OrderedScenes =
        {
            "Assets/Scenes/0_LandingScene.unity",
            "Assets/Scenes/1_MainMenu.unity",
            "Assets/Scenes/2_Lobby.unity",
            "Assets/Scenes/3_Dungeon1.unity",
        };

        static BuildSceneRegistrar()
        {
            EditorApplication.delayCall += SyncBuildSettings;
        }

        [MenuItem("DungeonBlade/Build Settings/Sync Scenes")]
        static void SyncBuildSettings()
        {
            var existing = EditorBuildSettings.scenes.ToDictionary(s => s.path, s => s);
            var result = new List<EditorBuildSettingsScene>();
            bool changed = false;

            foreach (var path in OrderedScenes)
            {
                if (!System.IO.File.Exists(path)) continue;

                if (existing.TryGetValue(path, out var existingScene))
                {
                    if (!existingScene.enabled)
                    {
                        existingScene.enabled = true;
                        changed = true;
                    }
                    result.Add(existingScene);
                }
                else
                {
                    result.Add(new EditorBuildSettingsScene(path, true));
                    changed = true;
                }
            }

            foreach (var s in EditorBuildSettings.scenes)
            {
                if (!OrderedScenes.Contains(s.path)) result.Add(s);
            }

            if (changed || result.Count != EditorBuildSettings.scenes.Length)
            {
                EditorBuildSettings.scenes = result.ToArray();
                Debug.Log("[DungeonBlade] Build Settings synced with project scenes.");
            }
        }
    }
}
#endif
