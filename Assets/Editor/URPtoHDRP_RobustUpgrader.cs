using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class URPtoHDRP_RobustUpgrader : EditorWindow
{
    [MenuItem("Tools/RenderPipeline/Upgrade URP → HDRP (Robust)")]
    public static void UpgradeAll()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        Shader hdrpLit = Shader.Find("HDRP/Lit");
        Shader hdrpUnlit = Shader.Find("HDRP/Unlit");

        if (hdrpLit == null || hdrpUnlit == null)
            Debug.LogWarning("HDRP shaders not found via Shader.Find(). Make sure HDRP package is installed and a HDRP Render Pipeline Asset is assigned.");

        int convertedMaterials = 0;
        int convertedGraphs = 0;
        int skipped = 0;

        // ---------- Materials ----------
        string[] matGuids = AssetDatabase.FindAssets("t:Material");
        foreach (string g in matGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(g);
            if (string.IsNullOrEmpty(assetPath)) continue;

            string fullPath = Path.Combine(projectRoot, assetPath).Replace('\\', '/');
            if (!File.Exists(fullPath))
            {
                // If file isn't on disk (unlikely), skip
                Debug.LogWarning($"Material file missing on disk: {assetPath}");
                skipped++;
                continue;
            }

            string fileText = File.ReadAllText(fullPath);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                Debug.LogWarning($"Could not load Material asset (skipping): {assetPath}");
                skipped++;
                continue;
            }

            string currentShaderName = mat.shader != null ? mat.shader.name : null;
            bool isErrorShader = currentShaderName != null && currentShaderName.ToLower().Contains("internalerror");
            bool isMissing = mat.shader == null || isErrorShader;

            // Try to detect original shader from text (m_ShaderName or GUID or explicit string)
            string detectedOriginal = null;
            if (fileText.Contains("Universal Render Pipeline/Lit")) detectedOriginal = "Universal Render Pipeline/Lit";
            else if (fileText.Contains("Universal Render Pipeline/Unlit")) detectedOriginal = "Universal Render Pipeline/Unlit";
            else if (fileText.Contains("Procedural Pixels/Bake AO - URP/Lit")) detectedOriginal = "Procedural Pixels/Bake AO - URP/Lit";
            else
            {
                // try to extract the GUID from m_Shader: {fileID: ..., guid: xxxxx, type: 3}
                var rg = new Regex(@"m_Shader:\s*\{[^}]*guid:\s*([0-9a-fA-F]+)");
                var m = rg.Match(fileText);
                if (m.Success)
                {
                    string guid = m.Groups[1].Value.ToLowerInvariant();
                    // Known URP Lit GUID (the one you posted) -> treat as URP Lit
                    if (guid == "933532a4fcc9baf4fa0491de14d08ed7") detectedOriginal = "Universal Render Pipeline/Lit";
                    // Add other GUID checks here if you discover them (unlit, terrain, custom)
                }
            }

            // if no detection and shader is a URP shader by runtime name, detect that way
            if (detectedOriginal == null && !string.IsNullOrEmpty(currentShaderName) && currentShaderName.Contains("Universal Render Pipeline"))
                detectedOriginal = currentShaderName;

            if (detectedOriginal == null && !isMissing)
            {
                // no reason to convert
                continue;
            }

            // --- Read original property values (try to read from Material object first; if unavailable, parse YAML)
            // We'll collect common URP property names into locals before swapping shader.
            Texture baseMap = TryGetTexture(mat, fileText, "_BaseMap") ?? TryGetTexture(mat, fileText, "_MainTex");

            Texture normalMap = TryGetTexture(mat, fileText, "_BumpMap") ?? TryGetTexture(mat, fileText, "_NormalMap");
            Texture metallicMap = TryGetTexture(mat, fileText, "_MetallicGlossMap") ?? TryGetTexture(mat, fileText, "_MetallicMap");
            float metallic = TryGetFloat(mat, fileText, "_Metallic");
            float smoothness = TryGetFloat(mat, fileText, "_Smoothness");
            if (smoothness == 0f) smoothness = TryGetFloat(mat, fileText, "_Glossiness");
            Texture emissionMap = TryGetTexture(mat, fileText, "_EmissionMap") ?? TryGetTexture(mat, fileText, "_EmissiveColorMap");
            Color baseColor = Color.white;
            {
                Color c1 = TryGetColor(mat, fileText, "_BaseColor");
                Color c2 = TryGetColor(mat, fileText, "_Color");
                if (c1 != default) baseColor = c1;
                else if (c2 != default) baseColor = c2;
            }

            Color emissionColor = Color.black;
            {
                Color c1 = TryGetColor(mat, fileText, "_EmissionColor");
                if (c1 != default) emissionColor = c1;
            }


            // Decide target shader
            string targetShaderName = null;
            if (detectedOriginal.Contains("Unlit")) targetShaderName = "HDRP/Unlit";
            else targetShaderName = "HDRP/Lit"; // fallback for Lit or custom bake AO

            Shader targetShader = Shader.Find(targetShaderName);
            if (targetShader == null)
            {
                Debug.LogWarning($"Target shader '{targetShaderName}' not found. You may not have HDRP installed. Skipping: {assetPath}");
                skipped++;
                continue;
            }

            // Assign shader and reassign properties
            mat.shader = targetShader;

            // Map properties to HDRP equivalents (best-effort)
            if (baseMap != null && mat.HasProperty("_BaseColorMap"))
            {
                mat.SetTexture("_BaseColorMap", baseMap);
            }
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", baseColor);
            }

            if (normalMap != null && mat.HasProperty("_NormalMap"))
            {
                mat.SetTexture("_NormalMap", normalMap);
                // keep bump scale if it exists
                float bumpScale = TryGetFloat(mat, fileText, "_BumpScale");
                if (bumpScale != 0f && mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", bumpScale);
                // enable keyword if applicable
                // mat.EnableKeyword("_NORMALMAP"); // HDRP handles keywords differently, just set texture
            }

            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", metallic);
            }
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }

            if (emissionMap != null && mat.HasProperty("_EmissiveColorMap"))
            {
                mat.SetTexture("_EmissiveColorMap", emissionMap);
                mat.SetColor("_EmissiveColor", emissionColor);
            }
            else if (mat.HasProperty("_EmissiveColor"))
            {
                mat.SetColor("_EmissiveColor", emissionColor);
            }

            EditorUtility.SetDirty(mat);
            convertedMaterials++;
            Debug.Log($"[Converted] {assetPath} -> {targetShaderName}");
        }

        // ---------- ShaderGraphs ----------
        // Look for .shadergraph files under Assets (do safe backup, then text-replace)
        string[] sgFiles = Directory.GetFiles(Application.dataPath, "*.shadergraph", SearchOption.AllDirectories);
        foreach (string file in sgFiles)
        {
            string text = File.ReadAllText(file);
            if (text.Contains("Universal Render Pipeline") || text.Contains("\"URP\""))
            {
                string backup = file + ".backup";
                try
                {
                    File.Copy(file, backup, true);
                }
                catch { /* ignore backup failure */ }

                string newText = text.Replace("Universal Render Pipeline", "High Definition Render Pipeline");
                // Don't blindly replace all "URP" tokens that may be unrelated, but many graphs use "URP" shortcodes.
                newText = newText.Replace("\"URP\"", "\"HDRP\"");

                File.WriteAllText(file, newText);
                // import asset using relative path
                string relative = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                AssetDatabase.ImportAsset(relative);
                convertedGraphs++;
                Debug.Log($"[ShaderGraph patched (backup created)] {relative}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("URP → HDRP Upgrade",
            $"Done. Materials converted: {convertedMaterials}\nShaderGraphs updated: {convertedGraphs}\nSkipped: {skipped}\n\nCheck Console for details.",
            "OK");
    }

    // --- Diagnostics helper: list materials that are missing/hidden
    [MenuItem("Tools/RenderPipeline/Diagnostics/List Missing Shader Materials")]
    public static void ListMissingMaterials()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string[] matGuids = AssetDatabase.FindAssets("t:Material");
        int missingCount = 0;

        foreach (string g in matGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(g);
            string fullPath = Path.Combine(projectRoot, assetPath).Replace('\\', '/');
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null) continue;

            bool isMissing = (mat.shader == null) || mat.shader.name.ToLower().Contains("internalerror");
            if (isMissing)
            {
                missingCount++;
                Debug.LogWarning($"[Missing Shader] {assetPath}");
                if (File.Exists(fullPath))
                {
                    string text = File.ReadAllText(fullPath);
                    int idx = text.IndexOf("m_Shader:");
                    if (idx != -1)
                    {
                        int end = text.IndexOf('\n', idx);
                        string line = text.Substring(idx, (end > idx ? end - idx : 0));
                        Debug.Log($"  → YAML shader line: {line}");
                    }
                    // also try to show m_ShaderName if present
                    var mName = Regex.Match(text, @"m_ShaderName:\s*(.+)");
                    if (mName.Success)
                        Debug.Log($"  → m_ShaderName: {mName.Groups[1].Value.Trim()}");
                }
            }
        }

        if (missingCount == 0) Debug.Log("No missing shader materials found.");
        else Debug.Log($"Found {missingCount} materials with missing shaders. See Console for details.");
    }

    // ---------- Helpers: read props from Material object or YAML fallback ----------
    static Texture TryGetTexture(Material mat, string yamlText, string prop)
    {
        if (mat != null && mat.HasProperty(prop))
        {
            var t = mat.GetTexture(prop);
            if (t != null) return t;
        }

        // parse YAML: look for "- _PropName:\n        m_Texture: {fileID: 2800000, guid: abc..., type: 3}"
        string pattern = @"- " + Regex.Escape(prop) + @":\s*\n\s*m_Texture:\s*\{[^}]*guid:\s*([0-9a-fA-F]+)";
        var m = Regex.Match(yamlText, pattern, RegexOptions.Multiline);
        if (m.Success)
        {
            string guid = m.Groups[1].Value;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
                return AssetDatabase.LoadAssetAtPath<Texture>(path);
        }
        return null;
    }

    static float TryGetFloat(Material mat, string yamlText, string prop)
    {
        if (mat != null && mat.HasProperty(prop))
        {
            try { return mat.GetFloat(prop); } catch { }
        }
        // parse YAML: "- _Prop: 0.5"
        string pattern = @"- " + Regex.Escape(prop) + @":\s*([0-9eE\+\-\.]+)";
        var m = Regex.Match(yamlText, pattern, RegexOptions.Multiline);
        if (m.Success && float.TryParse(m.Groups[1].Value, out float val)) return val;
        return 0f;
    }

    static Color TryGetColor(Material mat, string yamlText, string prop)
    {
        if (mat != null && mat.HasProperty(prop))
        {
            try { return mat.GetColor(prop); } catch { }
        }

        // parse YAML color: "- _BaseColor: {r: 0.9, g: 0.9, b: 0.9, a: 1}"
        string pattern = @"- " + Regex.Escape(prop) + @":\s*\{r:\s*([0-9eE\+\-\.]+),\s*g:\s*([0-9eE\+\-\.]+),\s*b:\s*([0-9eE\+\-\.]+),\s*a:\s*([0-9eE\+\-\.]+)\}";
        var m = Regex.Match(yamlText, pattern, RegexOptions.Multiline);
        if (m.Success)
        {
            if (float.TryParse(m.Groups[1].Value, out float r) &&
                float.TryParse(m.Groups[2].Value, out float g) &&
                float.TryParse(m.Groups[3].Value, out float b) &&
                float.TryParse(m.Groups[4].Value, out float a))
            {
                return new Color(r, g, b, a);
            }
        }
        return Color.black;
    }
}
