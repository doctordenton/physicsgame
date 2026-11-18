using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Tools > Boomer > Build Game Explosion Mist
/// Rebuilds the mist/smoke part of Left 4 Dead's "boomer_explode" effect (boomer_fx.pcf)
/// as a Unity prefab, using the game's own textures and numbers:
///   B        160 blood-mist droplets          (blood_mist, 32 sprites in a row)
///   B_extra  120 faster droplets
///   E        8 dark grey smoke clouds that swell (vistasmokev1, 6x6 sheet)
///   C        8 big faint red clouds, 0.1 s later
/// Source units are converted to metres with 0.0254 (1 unit = 1 inch).
/// Scale the prefab's root object to make the whole effect bigger or smaller.
public static class BoomerMistBuilder
{
    const string Dir = "Assets/Boomer/";
    const string TexDir = Dir + "Textures/Particles/";
    const string PrefabPath = Dir + "BoomerExplosionMist.prefab";
    const float U = 0.0254f;          // Source unit -> metre
    const float G = 9.81f;

    [MenuItem("Tools/Boomer/Build Game Explosion Mist")]
    public static void Build()
    {
        var mistMat = Mat("Boomer_BloodMist", TexDir + "blood_mist.png");
        var smokeMat = Mat("Boomer_Smoke", TexDir + "vistasmokev1.png");

        var root = new GameObject("BoomerExplosionMist");

        // B: 160 droplets, 200-400 u/s outward, live 1-3 s, dark red, fall with gravity -500 u/s^2
        Layer(root, "B Blood Mist", mistMat, 32, 1, 0, 31, count: 160, delay: 0f,
              life: (1f, 3f), speed: (200, 400), radius: (1, 2), spawnRadius: 32,
              c1: C(5, 4, 4), c2: C(50, 9, 9), alpha: (100, 200), gravity: 500, drag: 0f,
              sizeEnd: 0.8f, fadeStart: 0.5f, spinDeg: 5f);

        // B_extra: 120 faster droplets, 550-600 u/s, live 1-1.3 s, a bit redder
        Layer(root, "B_extra Blood Mist Fast", mistMat, 32, 1, 0, 31, count: 120, delay: 0f,
              life: (1f, 1.29f), speed: (550, 600), radius: (1, 4), spawnRadius: 32,
              c1: C(20, 2, 2), c2: C(81, 11, 11), alpha: (170, 220), gravity: 500, drag: 0f,
              sizeEnd: 1f, fadeStart: 0.5f, spinDeg: 0f);

        // E: 8 dark smoke clouds, slow (25-150 u/s), swell from 0.25x to 3x, live 1.5-2 s
        Layer(root, "E Smoke", smokeMat, 6, 6, 0, 15, count: 8, delay: 0f,
              life: (1.5f, 2f), speed: (25, 150), radius: (40, 70), spawnRadius: 32,
              c1: C(1, 1, 1), c2: C(74, 74, 74), alpha: (120, 150), gravity: 40, drag: 2f,
              sizeStart: 0.25f, sizeEnd: 3f, fadeStart: 0.45f, spinDeg: 0f);

        // C: 8 huge faint red clouds, 0.1 s later, 180-300 u/s, live 1-3 s
        Layer(root, "C Red Haze", smokeMat, 6, 6, 0, 15, count: 8, delay: 0.1f,
              life: (1f, 3f), speed: (180, 300), radius: (90, 200), spawnRadius: 32,
              c1: C(51, 0, 0), c2: C(117, 0, 0), alpha: (50, 80), gravity: 40, drag: 2f,
              sizeStart: 0.25f, sizeEnd: 3f, fadeStart: 0.45f, spinDeg: 0f);

        // --- Splashes (spray1 sheet, the same texture as the vomit; 8x3 frames, animated over life) ---
        var splashMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/StartScreen/Vomit/Vomit_Splash_A.mat");
        if (splashMat)
        {
            // boomer_explode (root): 9 huge, nearly black-red splashes, 180-200 u/s, live 0.8-1.2 s
            Layer(root, "Splash Big", splashMat, 8, 3, 0, 0, count: 9, delay: 0f,
                  life: (0.8f, 1.2f), speed: (180, 200), radius: (130, 200), spawnRadius: 32,
                  c1: C(1, 0, 0), c2: C(28, 0, 0), alpha: (120, 150), gravity: 400, drag: 0f,
                  sizeEnd: 1f, fadeStart: 0f, spinDeg: 0f, animate: true);

            // A_alt: 20 mid-size dark red splashes, 380-500 u/s, grow to 2x, live 1-1.3 s
            Layer(root, "Splash A_alt", splashMat, 8, 3, 0, 0, count: 20, delay: 0f,
                  life: (1f, 1.3f), speed: (380, 500), radius: (15, 18), spawnRadius: 32,
                  c1: C(44, 0, 0), c2: C(68, 0, 0), alpha: (128, 220), gravity: 800, drag: 0f,
                  sizeEnd: 2f, fadeStart: 0.5f, spinDeg: 5f, animate: true);

            // D: 4 big splashes thrown upward (200-400 u/s up), 0.5x -> 2x, live 0.7-0.8 s
            Layer(root, "Splash D Up", splashMat, 8, 3, 0, 0, count: 4, delay: 0f,
                  life: (0.7f, 0.8f), speed: (300, 600), radius: (60, 70), spawnRadius: 0,
                  c1: C(23, 1, 1), c2: C(149, 0, 0), alpha: (255, 255), gravity: 900, drag: 2f,
                  sizeStart: 0.5f, sizeEnd: 2f, fadeStart: 0.6f, spinDeg: 0f, animate: true, upCone: true);
        }
        else Debug.LogWarning("[Boomer] Vomit_Splash_A.mat not found, splashes skipped.");

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        // Hook it up to any Boomer Explode already in the open scene(s).
        int hooked = 0;
        foreach (var be in Object.FindObjectsByType<BoomerExplode>(FindObjectsSortMode.None))
        {
            if (be.explosionEffect) continue;
            be.explosionEffect = prefab;
            EditorUtility.SetDirty(be);
            EditorSceneManager.MarkSceneDirty(be.gameObject.scene);
            hooked++;
        }
        Selection.activeObject = prefab;
        Debug.Log($"[Boomer] Built {PrefabPath}. Hooked into {hooked} Boomer Explode component(s). Save the scene.");
    }

    static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);

    static void Layer(GameObject root, string name, Material mat, int tilesX, int tilesY, int firstFrame, int lastFrame,
                      int count, float delay, (float, float) life, (float, float) speed, (float, float) radius,
                      float spawnRadius, Color c1, Color c2, (int, int) alpha, float gravity, float drag,
                      float sizeEnd, float fadeStart, float spinDeg, float sizeStart = 1f,
                      bool animate = false, bool upCone = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = true;
        main.startDelay = delay;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life.Item1, life.Item2);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.Item1 * U, speed.Item2 * U);
        // Source "radius" is half the sprite's width.
        main.startSize = new ParticleSystem.MinMaxCurve(radius.Item1 * 2f * U, radius.Item2 * 2f * U);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
        var a1 = c1; a1.a = alpha.Item1 / 255f;
        var a2 = c2; a2.a = alpha.Item2 / 255f;
        main.startColor = new ParticleSystem.MinMaxGradient(a1, a2);
        main.gravityModifier = gravity * U / G;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;   // scale the root to resize the effect
        main.maxParticles = count;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        if (upCone)
        {
            shape.shapeType = ParticleSystemShapeType.Cone;         // thrown mostly straight up
            shape.angle = 25f;
            shape.radius = 0.01f;
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Hemisphere;  // the game adds an upward kick (0-300 u/s)
            shape.radius = Mathf.Max(spawnRadius * U, 0.01f);
            shape.randomDirectionAmount = 0.5f;
        }
        shape.rotation = new Vector3(-90f, 0f, 0f);                 // open upward

        if (drag > 0f)
        {
            var lim = ps.limitVelocityOverLifetime;
            lim.enabled = true;
            lim.drag = drag;
            lim.multiplyDragByParticleSize = false;
            lim.multiplyDragByParticleVelocity = false;
        }

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, sizeStart), new Keyframe(1f, sizeEnd)));

        // Fade out over the last part of each particle's life.
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeStart), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        if (spinDeg > 0f)
        {
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = spinDeg * Mathf.Deg2Rad;
        }

        var tsa = ps.textureSheetAnimation;
        tsa.enabled = true;
        tsa.mode = ParticleSystemAnimationMode.Grid;
        tsa.numTilesX = tilesX;
        tsa.numTilesY = tilesY;
        tsa.animation = ParticleSystemAnimationType.WholeSheet;
        if (animate)
        {
            // Splashes play through the whole sheet over their life, like the game's sequence.
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            tsa.startFrame = new ParticleSystem.MinMaxCurve(0f);
            tsa.flipU = 0.5f;
        }
        else
        {
            // Each particle picks a random sprite from the sheet and keeps it.
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            tsa.startFrame = new ParticleSystem.MinMaxCurve(firstFrame, lastFrame + 0.99f);
        }

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortMode = ParticleSystemSortMode.Distance;
    }

    static Material Mat(string name, string texPath)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
        if (ti && (!ti.alphaIsTransparency || ti.wrapMode != TextureWrapMode.Clamp))
        { ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport(); }

        string path = Dir + "Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.SetFloat("_ColorMode", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }
}
