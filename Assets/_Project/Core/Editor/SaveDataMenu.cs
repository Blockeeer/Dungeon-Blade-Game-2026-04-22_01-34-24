#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DungeonBlade.Core.EditorTools
{
    public static class SaveDataMenu
    {
        static string Root => Path.Combine(Application.persistentDataPath, "DungeonBlade");

        [MenuItem("DungeonBlade/Save Data/Open Save Folder")]
        public static void OpenSaveFolder()
        {
            Directory.CreateDirectory(Root);
            Process.Start(new ProcessStartInfo
            {
                FileName = Root,
                UseShellExecute = true,
                Verb = "open",
            });
        }

        [MenuItem("DungeonBlade/Save Data/Reset Profile (delete profile.json)")]
        public static void ResetProfile()
        {
            string path = Path.Combine(Root, "profile.json");
            if (File.Exists(path))
            {
                File.Delete(path);
                UnityEngine.Debug.Log($"[SaveData] Deleted {path}");
            }
            else
            {
                UnityEngine.Debug.Log($"[SaveData] No profile.json to delete at {path}");
            }
        }

        [MenuItem("DungeonBlade/Save Data/Reset Bank (delete bank.json)")]
        public static void ResetBank()
        {
            string path = Path.Combine(Root, "bank.json");
            if (File.Exists(path))
            {
                File.Delete(path);
                UnityEngine.Debug.Log($"[SaveData] Deleted {path}");
            }
            else
            {
                UnityEngine.Debug.Log($"[SaveData] No bank.json to delete at {path}");
            }
        }

        [MenuItem("DungeonBlade/Save Data/Reset ALL (delete everything)")]
        public static void ResetAll()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, true);
                UnityEngine.Debug.Log($"[SaveData] Deleted {Root}");
            }
            else
            {
                UnityEngine.Debug.Log($"[SaveData] No save folder to delete at {Root}");
            }
        }
    }
}
#endif
