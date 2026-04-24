#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using DungeonBlade.Characters;

namespace DungeonBlade.EditorTools
{
    /// <summary>
    /// Scans a source folder for character model FBX files and creates a
    /// CharacterData ScriptableObject for each one. Writes the assets into
    /// DungeonBladeSample/Characters/ so they're picked up by the scene builder.
    ///
    /// Run via: DungeonBlade → Characters → Build Roster From Folder
    /// The dialog asks you to pick a folder — point it at your Player models
    /// folder (e.g. the unzipped "Player Model" folder in your Downloads).
    /// </summary>
    public static class DBCharacterBuilder
    {
        const string CharactersFolder = DBEditorMenu.SamplePath + "/Characters";

        [MenuItem("DungeonBlade/Characters/Build Roster From Folder...")]
        public static void BuildFromFolder()
        {
            DBEditorMenu.EnsureFolder(CharactersFolder);

            var path = EditorUtility.OpenFolderPanel("Pick the folder containing character FBX models", "", "");
            if (string.IsNullOrEmpty(path)) return;

            Debug.Log($"[DBCharacterBuilder] Picked folder: {path}");

            var fbxFiles = Directory.GetFiles(path, "*.fbx", SearchOption.TopDirectoryOnly);
            Debug.Log($"[DBCharacterBuilder] Found {fbxFiles.Length} .fbx files in folder");

            if (fbxFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("DungeonBlade", "No .fbx files found in the chosen folder.\n\nFolder: " + path, "OK");
                return;
            }

            var importedAssetPaths = new List<string>();
            foreach (var f in fbxFiles)
            {
                var fileName = Path.GetFileName(f);
                var safeName = SanitizeForPath(Path.GetFileNameWithoutExtension(f));
                var subfolder = CharactersFolder + "/" + safeName;
                DBEditorMenu.EnsureFolder(subfolder);

                var destAssetPath = subfolder + "/" + fileName;

                // If file is already inside Unity's Assets/ folder (user picked an internal folder), skip copy
                var normalizedSrc = f.Replace('\\', '/');
                var dataPath = Application.dataPath.Replace('\\', '/');
                bool isInsideAssets = normalizedSrc.StartsWith(dataPath);

                if (!isInsideAssets)
                {
                    var destFull = Path.Combine(Application.dataPath, destAssetPath.Substring("Assets/".Length));
                    try
                    {
                        File.Copy(f, destFull, overwrite: true);
                        Debug.Log($"[DBCharacterBuilder] Copied {fileName} to {destAssetPath}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[DBCharacterBuilder] Failed to copy {fileName}: {ex.Message}");
                        continue;
                    }
                }
                importedAssetPaths.Add(destAssetPath);
            }

            // Force synchronous import of the newly copied files
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var assetPath in importedAssetPaths)
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            // Configure each as Humanoid
            foreach (var assetPath in importedAssetPaths)
                ConfigureAsHumanoid(assetPath);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Create CharacterData for each
            var roster = new List<CharacterData>();
            foreach (var assetPath in importedAssetPaths)
            {
                var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (modelPrefab == null)
                {
                    Debug.LogError($"[DBCharacterBuilder] Couldn't load model at: {assetPath}");
                    continue;
                }

                var rawName = Path.GetFileNameWithoutExtension(assetPath);
                var cleanName = CleanDisplayName(rawName);
                var charId = "char_" + SanitizeForId(rawName);

                var data = ScriptableObject.CreateInstance<CharacterData>();
                data.characterId = charId;
                data.displayName = cleanName;
                data.flavorText = GenerateFlavor(cleanName);
                data.modelPrefab = modelPrefab;
                data.modelScale = 1f;
                data.modelRotation = Vector3.zero;
                data.swordAttachBoneName = "mixamorig:RightHand";
                data.gunAttachBoneName = "mixamorig:RightHand";
                data.swordLocalOffset = new Vector3(0, 0.05f, 0.1f);
                data.swordLocalRotation = new Vector3(0, 0, 0);

                var dataAssetPath = Path.GetDirectoryName(assetPath).Replace('\\', '/') + "/Character_" + SanitizeForPath(rawName) + ".asset";
                if (AssetDatabase.LoadAssetAtPath<CharacterData>(dataAssetPath) != null)
                    AssetDatabase.DeleteAsset(dataAssetPath);
                AssetDatabase.CreateAsset(data, dataAssetPath);
                Debug.Log($"[DBCharacterBuilder] Created CharacterData at: {dataAssetPath}");
                roster.Add(data);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("DungeonBlade",
                $"Created {roster.Count} character(s) from folder.\n\n" +
                (roster.Count > 0
                    ? "Now run DungeonBlade → Sub-Builders → Lobby Scene Only to rebuild the lobby."
                    : "No characters were created. Check the Console for errors."),
                "OK");
        }

        [MenuItem("DungeonBlade/Characters/Rescan Roster")]
        public static void RescanRoster()
        {
            var guids = AssetDatabase.FindAssets("t:CharacterData", new[] { CharactersFolder });
            EditorUtility.DisplayDialog("DungeonBlade", $"Found {guids.Length} CharacterData assets under {CharactersFolder}.", "OK");
        }

        /// <summary>Finds every CharacterData in the Characters folder. Used by DBSceneBuilder.</summary>
        public static List<CharacterData> LoadAllCharacters()
        {
            var list = new List<CharacterData>();
            if (!AssetDatabase.IsValidFolder(CharactersFolder)) return list;
            var guids = AssetDatabase.FindAssets("t:CharacterData", new[] { CharactersFolder });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var c = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (c != null) list.Add(c);
            }
            return list;
        }

        static void ConfigureAsHumanoid(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) return;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
        }

        static string SanitizeForPath(string s)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new System.Text.StringBuilder();
            foreach (var ch in s)
            {
                if (System.Array.IndexOf(invalid, ch) < 0 && ch != ' ') clean.Append(ch);
                else if (ch == ' ') clean.Append('_');
            }
            return clean.ToString();
        }

        static string SanitizeForId(string s)
        {
            var clean = new System.Text.StringBuilder();
            foreach (var ch in s.ToLower())
            {
                if (char.IsLetterOrDigit(ch)) clean.Append(ch);
                else if (ch == ' ' || ch == '-' || ch == '_') clean.Append('_');
            }
            return clean.ToString();
        }

        static string CleanDisplayName(string raw)
        {
            // "Female-6-T-Pose" → "Female 6"
            var name = raw.Replace("-T-Pose", "").Replace("_T_Pose", "").Replace("T-Pose", "");
            name = name.Replace('-', ' ').Replace('_', ' ').Trim();
            return name;
        }

        static string GenerateFlavor(string name)
        {
            if (name.ToLower().Contains("female")) return "A swift and cunning blade, graceful in motion.";
            if (name.ToLower().Contains("male"))   return "A seasoned warrior, heavy-handed and resolute.";
            return "A wanderer drawn to the dungeon's depths.";
        }
    }
}
#endif
