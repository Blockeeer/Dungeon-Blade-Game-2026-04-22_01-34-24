#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace DungeonBlade.EditorTools
{
    /// <summary>
    /// Top-level editor menu. The one-click entry point is
    /// `DungeonBlade > Build Everything`, which creates:
    ///   • sample item assets
    ///   • animator controllers
    ///   • player / enemy / NPC prefabs
    ///   • HUD + Inventory + Bank canvases
    ///   • Lobby scene + Dungeon scene
    /// After running, open Dungeon_ForsakenKeep.unity and press Play.
    /// </summary>
    public static class DBEditorMenu
    {
        public const string SamplePath = "Assets/DungeonBladeSample";
        public const string ItemsPath = SamplePath + "/Items";
        public const string LootPath = SamplePath + "/Loot";
        public const string AnimatorsPath = SamplePath + "/Animators";
        public const string PrefabsPath = SamplePath + "/Prefabs";
        public const string ScenesPath = SamplePath + "/Scenes";
        public const string UIPath = SamplePath + "/UI";

        [MenuItem("DungeonBlade/Build Everything", priority = 1)]
        public static void BuildEverything()
        {
            EnsureFolder(SamplePath);
            EnsureFolder(ItemsPath);
            EnsureFolder(LootPath);
            EnsureFolder(AnimatorsPath);
            EnsureFolder(PrefabsPath);
            EnsureFolder(ScenesPath);
            EnsureFolder(UIPath);

            EditorUtility.DisplayProgressBar("DungeonBlade", "Building sample items...", 0.1f);
            DBContentBuilder.BuildSampleItems();
            DBContentBuilder.BuildSampleLootTables();

            EditorUtility.DisplayProgressBar("DungeonBlade", "Building animator controllers...", 0.25f);
            DBAnimatorBuilder.BuildAll();

            EditorUtility.DisplayProgressBar("DungeonBlade", "Building prefabs...", 0.45f);
            DBPrefabBuilder.BuildAll();

            EditorUtility.DisplayProgressBar("DungeonBlade", "Building Lobby scene...", 0.7f);
            DBSceneBuilder.BuildLobbyScene();

            EditorUtility.DisplayProgressBar("DungeonBlade", "Building Dungeon scene...", 0.9f);
            DBSceneBuilder.BuildDungeonScene();

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("DungeonBlade",
                "Build complete!\n\n" +
                "Open " + ScenesPath + "/Dungeon_ForsakenKeep.unity and press Play.\n\n" +
                "Don't forget: bake the NavMesh (Window → AI → Navigation → Bake).",
                "OK");
        }

        [MenuItem("DungeonBlade/Sub-Builders/Sample Items Only")]
        public static void BuildSampleItemsOnly()
        {
            EnsureFolder(SamplePath);
            EnsureFolder(ItemsPath);
            EnsureFolder(LootPath);
            DBContentBuilder.BuildSampleItems();
            DBContentBuilder.BuildSampleLootTables();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("DungeonBlade/Sub-Builders/Animator Controllers Only")]
        public static void BuildAnimatorsOnly()
        {
            EnsureFolder(SamplePath);
            EnsureFolder(AnimatorsPath);
            DBAnimatorBuilder.BuildAll();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("DungeonBlade/Sub-Builders/Prefabs Only")]
        public static void BuildPrefabsOnly()
        {
            EnsureFolder(SamplePath);
            EnsureFolder(PrefabsPath);
            DBPrefabBuilder.BuildAll();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("DungeonBlade/Sub-Builders/Lobby Scene Only")]
        public static void BuildLobbyOnly()
        {
            EnsureFolder(SamplePath);
            EnsureFolder(ScenesPath);
            DBSceneBuilder.BuildLobbyScene();
        }

        [MenuItem("DungeonBlade/Sub-Builders/Dungeon Scene Only")]
        public static void BuildDungeonOnly()
        {
            EnsureFolder(SamplePath);
            EnsureFolder(ScenesPath);
            DBSceneBuilder.BuildDungeonScene();
        }

        [MenuItem("DungeonBlade/Open Save Folder")]
        public static void OpenSaveFolder()
        {
            var path = Application.persistentDataPath;
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("DungeonBlade/Delete Sample Content")]
        public static void DeleteSampleContent()
        {
            if (EditorUtility.DisplayDialog("DungeonBlade",
                "Delete all generated sample content under " + SamplePath + "? This cannot be undone.",
                "Delete", "Cancel"))
            {
                AssetDatabase.DeleteAsset(SamplePath);
                AssetDatabase.Refresh();
            }
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
