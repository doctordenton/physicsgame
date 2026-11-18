using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// Tools > Boomer > Fix Materials (match game look)
/// Replaces Unity's default glossy FBX materials with ones that match the game's:
///  - body: mostly matte; shine only where the game's specular mask allows it
///  - normal map imported as a real normal map (green flipped: Source uses DirectX normals)
///  - hair: alpha-cutout, double-sided (the game uses $alphatest + $nocull)
public static class BoomerMaterialFixer
{
    const string Dir = "Assets/Boomer/";
    const string Tex = Dir + "Textures/";
    const string MatDir = Dir + "Materials/";

    [MenuItem("Tools/Boomer/Fix Materials (match game look)")]
    public static void Fix()
    {
        SetupTextures();
        if (!AssetDatabase.IsValidFolder(MatDir.TrimEnd('/'))) AssetDatabase.CreateFolder("Assets/Boomer", "Materials");

        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var body = GetOrCreate(MatDir + "Boomer_Body.mat", lit);
        body.SetTexture("_BaseMap", Load(Tex + "Boomer.png"));
        body.SetColor("_BaseColor", Color.white);
        body.SetTexture("_BumpMap", Load(Tex + "Boomer_normal.png"));
        body.SetFloat("_BumpScale", 1f);
        body.EnableKeyword("_NORMALMAP");
        body.SetTexture("_MetallicGlossMap", Load(Tex + "Boomer_smoothness.png"));
        body.EnableKeyword("_METALLICSPECGLOSSMAP");
        body.SetFloat("_Smoothness", 1f);            // multiplier on the mask
        body.SetFloat("_Metallic", 0f);
        body.SetFloat("_SmoothnessTextureChannel", 0); // alpha of metallic map
        body.SetFloat("_EnvironmentReflections", 0f);  // no shiny skybox reflections
        body.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        EditorUtility.SetDirty(body);

        var hair = GetOrCreate(MatDir + "Boomer_Hair.mat", lit);
        hair.SetTexture("_BaseMap", Load(Tex + "Boomer_Hair.png"));
        hair.SetFloat("_AlphaClip", 1f);
        hair.EnableKeyword("_ALPHATEST_ON");
        hair.SetFloat("_Cutoff", 0.5f);
        hair.SetFloat("_Cull", (float)CullMode.Off);
        hair.SetFloat("_Smoothness", 0f);
        hair.SetFloat("_Metallic", 0f);
        hair.SetFloat("_SpecularHighlights", 0f);
        hair.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        hair.renderQueue = (int)RenderQueue.AlphaTest;
        EditorUtility.SetDirty(hair);

        // Gibs: the game's exploded material has no phong at all -> fully matte.
        var gib = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Boomer_exploded.mat");
        if (gib) { gib.SetFloat("_Smoothness", 0.05f); gib.SetFloat("_Metallic", 0f); EditorUtility.SetDirty(gib); }

        AssetDatabase.SaveAssets();
        Remap(Dir + "Boomer.fbx", body, hair);
        Remap("Assets/StartScreen/Boomer_Face.fbx", body, hair);
        Debug.Log("[Boomer] Materials fixed: matte body with masked shine, real normal map, cut-out hair.");
    }

    static void SetupTextures()
    {
        var n = (TextureImporter)AssetImporter.GetAtPath(Tex + "Boomer_normal.png");
        if (n && (n.textureType != TextureImporterType.NormalMap || !n.flipGreenChannel))
        {
            n.textureType = TextureImporterType.NormalMap;
            n.flipGreenChannel = true;
            n.SaveAndReimport();
        }
        var s = (TextureImporter)AssetImporter.GetAtPath(Tex + "Boomer_smoothness.png");
        if (s && s.sRGBTexture) { s.sRGBTexture = false; s.SaveAndReimport(); }
        var h = (TextureImporter)AssetImporter.GetAtPath(Tex + "Boomer_Hair.png");
        if (h && !h.alphaIsTransparency) { h.alphaIsTransparency = true; h.SaveAndReimport(); }
    }

    static void Remap(string fbxPath, Material body, Material hair)
    {
        var imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (!imp) return;
        var mats = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>().Select(m => m.name).ToList();
        // Also cover materials that are already remapped (no embedded copy any more).
        foreach (var kv in imp.GetExternalObjectMap())
            if (kv.Key.type == typeof(Material) && !mats.Contains(kv.Key.name)) mats.Add(kv.Key.name);
        foreach (var name in mats)
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name),
                         name.ToLower().Contains("hair") ? hair : body);
        imp.SaveAndReimport();
    }

    static Texture2D Load(string p) => AssetDatabase.LoadAssetAtPath<Texture2D>(p);

    static Material GetOrCreate(string path, Shader sh)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m) { m.shader = sh; return m; }
        m = new Material(sh);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
