using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Tools > Boomer > Build Start Screen Scene
/// Creates Assets/Scenes/StartScreen.unity and puts it first in Build Settings.
public static class StartScreenBuilder
{
    const string Dir = "Assets/StartScreen/";
    const string ScenePath = "Assets/Scenes/StartScreen.unity";

    [MenuItem("Tools/Boomer/Build Start Screen Scene")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        BoomerMaterialFixer.Fix();   // matte game-accurate materials on Boomer + Boomer_Face

        // Boomer_Face.fbx = same Boomer, plus the JawOpen blend shape.
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "Boomer_Face.fbx")
                 ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Boomer/Boomer.fbx");
        if (!model) { Debug.LogError("Boomer model not found"); return; }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Camera ---
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 35f;
        cam.nearClipPlane = 0.05f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.07f);
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0, 1.5f, 1.2f);

        // --- Lights: soft key light on the face + a sickly green rim from behind ---
        var key = new GameObject("Key Light").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.6f;
        key.color = new Color(1f, 0.95f, 0.85f);
        key.transform.rotation = Quaternion.Euler(25f, 200f, 0f);

        var rim = new GameObject("Rim Light").AddComponent<Light>();
        rim.type = LightType.Point;
        rim.range = 4f;
        rim.intensity = 6f;
        rim.color = new Color(0.45f, 0.85f, 0.3f);
        rim.transform.position = new Vector3(0.5f, 2.1f, -0.7f);

        // --- Boomer ---
        var boomer = (GameObject)PrefabUtility.InstantiatePrefab(model);
        boomer.name = "Boomer";
        boomer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var anim = boomer.GetComponentInChildren<Animator>() ?? boomer.AddComponent<Animator>();
        anim.runtimeAnimatorController = GetOrMakeController();
        anim.applyRootMotion = false;

        // --- Vomit particles ---
        var vomit = MakeVomit();

        // --- UI ---
        var font = AssetDatabase.LoadAssetAtPath<Font>(Dir + "Futurot.ttf");
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var startText = MakeText(canvasGo.transform, "Start Text", "Start game?", font, 130,
            new Vector2(0.5f, 0.12f), TextAnchor.MiddleCenter);
        var beginText = MakeText(canvasGo.transform, "Begin Text", "Begin!", font, 170,
            new Vector2(0.72f, 0.14f), TextAnchor.MiddleCenter);
        beginText.SetActive(false);

        // --- Controller ---
        var ctrl = new GameObject("Start Screen").AddComponent<StartScreen>();
        ctrl.boomer = anim;
        ctrl.cam = cam;
        ctrl.startText = startText;
        ctrl.beginText = beginText;
        ctrl.vomit = vomit;
        ctrl.face = boomer.GetComponentInChildren<SkinnedMeshRenderer>();
        ctrl.FillSoundsFromProject();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettingsFirst(ScenePath);
        Debug.Log("[StartScreen] Built " + ScenePath + ". Press Play and click.");
    }

    static GameObject MakeText(Transform parent, string name, string text, Font font, int size, Vector2 anchor, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(1200, 260);
        var t = go.GetComponent<Text>();
        t.text = text;
        t.font = font ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = align;
        t.color = new Color(0.92f, 0.95f, 0.88f);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var o = go.GetComponent<Outline>();
        o.effectColor = new Color(0, 0, 0, 0.9f);
        o.effectDistance = new Vector2(4, -4);
        return go;
    }

    // ---------------------------------------------------------------------------------
    // Vomit, rebuilt from the game's boomer_vomit effect (particles/boomer_fx.pcf):
    //   * boomer_vomit_b / _c : animated "spray1" liquid splashes, grow 1x -> 3x
    //   * boomer_vomit / _d   : fast thin streaks (blood_core texture), grow 0.1x -> 3x
    //   game values: 620-700 units/s forward (~16-18 m/s), gravity 300 u/s^2 (~0.78 g),
    //   ~1 s of emission + a second pulse at 0.9 s, olive-green tint.
    // Speed is slowed here because the camera is ~1 m from his mouth, not across a room.
    // ---------------------------------------------------------------------------------
    static readonly Color BileLight = new Color(0.58f, 0.66f, 0.06f);  // game color1 (70,81,0) brightened
    static readonly Color BileDark  = new Color(0.17f, 0.21f, 0.02f);  // game color2 (6,6,0)/(10,9,0) brightened

    static ParticleSystem MakeVomit()
    {
        // Parent = streaks; children = the two splash sequences. Play() on the parent plays all.
        var streaks = MakeLayer("Vomit", Dir + "Vomit/streak.png", tilesY: 0,
            rate: 90f, life: new Vector2(0.25f, 0.35f), speed: new Vector2(5.5f, 7f),
            size: new Vector2(0.03f, 0.06f), grow: AnimationCurve.EaseInOut(0, 0.1f, 0.15f, 3f), stretch: true);

        var a = MakeLayer("Vomit Splash A", Dir + "Vomit/spray_seqA.png", tilesY: 3,
            rate: 45f, life: new Vector2(0.75f, 0.85f), speed: new Vector2(4f, 5.5f),
            size: new Vector2(0.10f, 0.20f), grow: AnimationCurve.Linear(0, 1f, 1, 3f), stretch: false);
        var b = MakeLayer("Vomit Splash B", Dir + "Vomit/spray_seqB.png", tilesY: 3,
            rate: 35f, life: new Vector2(0.75f, 0.85f), speed: new Vector2(4f, 5.5f),
            size: new Vector2(0.10f, 0.20f), grow: AnimationCurve.Linear(0, 1f, 1, 3f), stretch: false);
        a.transform.SetParent(streaks.transform, false);
        b.transform.SetParent(streaks.transform, false);
        return streaks;
    }

    static ParticleSystem MakeLayer(string name, string texPath, int tilesY, float rate, Vector2 life, Vector2 speed,
                                    Vector2 size, AnimationCurve grow, bool stretch)
    {
        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.78f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 500;
        main.startColor = new ParticleSystem.MinMaxGradient(BileDark, BileLight);

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 9f;      // game: +/-128 sideways on 660 forward ~ 11 deg
        shape.radius = 0.02f;

        var limit = ps.limitVelocityOverLifetime;   // the game's slight drag
        limit.enabled = true;
        limit.limit = 100f;
        limit.drag = 0.07f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, grow);

        var col = ps.colorOverLifetime;               // "Alpha Fade Out": fade over the last half
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.5f), new GradientAlphaKey(0, 1) });
        col.color = g;

        if (tilesY > 0)   // play the game's 24-frame splash animation over each particle's life
        {
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Grid;
            tsa.numTilesX = 8;
            tsa.numTilesY = tilesY;
            tsa.animation = ParticleSystemAnimationType.WholeSheet;
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0, 1, 1));
            tsa.cycleCount = 1;
            tsa.flipU = 0.5f;   // game: "Rotation Yaw Flip Random"
        }

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        if (stretch) { r.velocityScale = 0.05f; r.lengthScale = 2f; }
        r.sharedMaterial = ParticleMat(name.Replace(" ", "_"), texPath);
        r.sortingFudge = stretch ? 0f : -1f;
        return ps;
    }

    static Material ParticleMat(string name, string texPath)
    {
        string path = Dir + "Vomit/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(m, path);
        }
        var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
        if (ti && (!ti.alphaIsTransparency || ti.wrapMode != TextureWrapMode.Clamp))
        { ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport(); }

        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        m.SetColor("_BaseColor", Color.white);
        // Transparent, alpha-blended (like the game's $translucent SpriteCard)
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.SetFloat("_ColorMode", 0f);   // multiply: vertex colour tints the white texture
        EditorUtility.SetDirty(m);
        return m;
    }

    static RuntimeAnimatorController GetOrMakeController()
    {
        const string path = Dir + "BoomerStartScreen.controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing) return existing;

        var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Boomer/Boomer.fbx").OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__")).ToArray();
        var idle = clips.FirstOrDefault(c => c.name == "Idle");
        var vomit = clips.FirstOrDefault(c => c.name == "Vomit_Standing");

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = ctrl.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        var sVomit = sm.AddState("Vomit_Standing"); sVomit.motion = vomit;
        sm.defaultState = sIdle;
        return ctrl;
    }

    static void AddToBuildSettingsFirst(string path)
    {
        var list = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
        list.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
