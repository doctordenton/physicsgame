using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Tools > Set > Set Up Skybox and Cooler
///  - Skybox: makes a Panoramic (360) skybox material from Assets/Set/Skybox/stadium_skybox.jpg
///    and makes it the sky of the open scene.
///  - Cooler: gives GatoradeCooler.fbx a URP material using its baked colour texture.
/// Run Tools > Set > Build Sideline Set afterwards to swap the real cooler into the set.
public static class SetAssetsSetup
{
    const string SkyTex = "Assets/Set/Skybox/stadium_skybox.jpg";
    const string SkyMat = "Assets/Set/Skybox/StadiumSkybox.mat";
    const string CoolerFbx = "Assets/Set/Models/GatoradeCooler.fbx";
    const string CoolerTex = "Assets/Set/Models/GatoradeCooler_albedo.png";
    const string CoolerMat = "Assets/Set/Models/GatoradeCooler.mat";

    [MenuItem("Tools/Set/Set Up Skybox and Cooler")]
    public static void Setup()
    {
        SetupSkybox();
        SetupCooler();
        AssetDatabase.SaveAssets();
    }

    static void SetupSkybox()
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(SkyTex);
        if (!ti) { Debug.LogError("[Set] Missing " + SkyTex); return; }
        ti.textureShape = TextureImporterShape.Texture2D;
        ti.npotScale = TextureImporterNPOTScale.None;   // keep the full 2816 x 1222
        ti.maxTextureSize = 4096;
        ti.mipmapEnabled = false;                        // no seam line where the panorama wraps
        ti.wrapModeU = TextureWrapMode.Repeat;           // wraps around 360 degrees
        ti.wrapModeV = TextureWrapMode.Clamp;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.SaveAndReimport();

        var mat = AssetDatabase.LoadAssetAtPath<Material>(SkyMat);
        if (!mat) { mat = new Material(Shader.Find("Skybox/Panoramic")); AssetDatabase.CreateAsset(mat, SkyMat); }
        mat.shader = Shader.Find("Skybox/Panoramic");
        mat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(SkyTex));
        mat.SetFloat("_Mapping", 1f);     // Latitude Longitude Layout
        mat.SetFloat("_ImageType", 0f);   // 360 degrees
        mat.SetFloat("_Exposure", 1f);
        mat.SetFloat("_Rotation", 0f);
        EditorUtility.SetDirty(mat);

        RenderSettings.skybox = mat;
        DynamicGI.UpdateEnvironment();     // ambient light picks up the stadium colours
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Set] Skybox set on the open scene. Turn it with the material's Rotation. Save the scene.");
    }

    static void SetupCooler()
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(CoolerTex);
        if (ti && ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport(); }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(CoolerMat);
        if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, CoolerMat); }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(CoolerTex));
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0.35f);   // glossy plastic, not mirror-like
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);

        var imp = AssetImporter.GetAtPath(CoolerFbx) as ModelImporter;
        if (!imp) { Debug.LogError("[Set] Missing " + CoolerFbx); return; }
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(CoolerFbx))
            if (o is Material m)
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), mat);
        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "GatoradeCooler"), mat);
        imp.SaveAndReimport();
        Debug.Log("[Set] Cooler material set up.");
    }
}
