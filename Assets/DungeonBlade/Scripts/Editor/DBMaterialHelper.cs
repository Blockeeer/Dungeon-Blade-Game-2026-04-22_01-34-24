#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Collections.Generic;

namespace DungeonBlade.EditorTools
{
    /// <summary>
    /// Creates materials saved as .mat assets in the project. Required because
    /// in-memory materials (`new Material(shader)`) don't serialize properly
    /// when assigned to a prefab's Mesh Renderer — the reference becomes null
    /// after save, resulting in pink/missing material.
    ///
    /// Materials are cached in a folder and reused across objects with the
    /// same color — so the 4 SkeletonSoldiers share one gray material.
    /// </summary>
    public static class DBMaterialHelper
    {
        const string MaterialsFolder = "Assets/DungeonBladeSample/Materials";
        static Shader cachedShader;
        static Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        public static Shader GetShader()
        {
            if (cachedShader != null) return cachedShader;

            cachedShader = Shader.Find("Universal Render Pipeline/Lit");
            if (cachedShader != null) return cachedShader;

            cachedShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (cachedShader != null) return cachedShader;

            cachedShader = Shader.Find("Standard");
            if (cachedShader != null) return cachedShader;

            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.defaultShader != null)
            {
                cachedShader = pipeline.defaultShader;
                return cachedShader;
            }

            cachedShader = Shader.Find("Sprites/Default");
            return cachedShader;
        }

        public static Material Create(Color baseColor, Color? emissive = null)
        {
            EnsureFolder();

            string key = ColorKey(baseColor, emissive);
            if (materialCache.TryGetValue(key, out var cached) && cached != null) return cached;

            string path = MaterialsFolder + "/" + key + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                materialCache[key] = existing;
                return existing;
            }

            var shader = GetShader();
            var mat = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            mat.name = key;

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.color = baseColor;

            if (emissive.HasValue)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", emissive.Value);
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                if (mat.HasProperty("_EmissiveColor"))
                    mat.SetColor("_EmissiveColor", emissive.Value);
            }

            AssetDatabase.CreateAsset(mat, path);
            materialCache[key] = mat;
            return mat;
        }

        public static void ClearCache() { cachedShader = null; materialCache.Clear(); }

        static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(MaterialsFolder)) return;
            if (!AssetDatabase.IsValidFolder("Assets/DungeonBladeSample"))
                AssetDatabase.CreateFolder("Assets", "DungeonBladeSample");
            AssetDatabase.CreateFolder("Assets/DungeonBladeSample", "Materials");
        }

        static string ColorKey(Color c, Color? emissive)
        {
            int r = Mathf.RoundToInt(c.r * 255);
            int g = Mathf.RoundToInt(c.g * 255);
            int b = Mathf.RoundToInt(c.b * 255);
            string key = $"Mat_{r:D3}_{g:D3}_{b:D3}";
            if (emissive.HasValue)
            {
                int er = Mathf.RoundToInt(emissive.Value.r * 255);
                int eg = Mathf.RoundToInt(emissive.Value.g * 255);
                int eb = Mathf.RoundToInt(emissive.Value.b * 255);
                key += $"_E{er:D3}_{eg:D3}_{eb:D3}";
            }
            return key;
        }
    }
}
#endif
