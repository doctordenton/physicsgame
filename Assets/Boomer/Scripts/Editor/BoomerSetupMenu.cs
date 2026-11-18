using UnityEditor;
using UnityEngine;

/// Tools > Boomer > Drop Test Boomer Into Scene
/// Places a fully wired Boomer 15 m above the origin so you can hit Play and watch him fall.
public static class BoomerSetupMenu
{
    const string Dir = "Assets/Boomer/";

    [MenuItem("Tools/Boomer/Drop Test Boomer Into Scene")]
    static void Setup()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "Boomer.fbx");
        var gibs = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "BoomerGibs.fbx");
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Dir + "Boomer.controller");
        if (!model) { Debug.LogError("Boomer.fbx not found in " + Dir); return; }

        var boomer = (GameObject)PrefabUtility.InstantiatePrefab(model);
        boomer.name = "Boomer";
        boomer.transform.position = new Vector3(0f, 15f, 0f);

        var anim = boomer.GetComponentInChildren<Animator>() ?? boomer.AddComponent<Animator>();
        if (controller) anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;

        // Capsule sized from the mesh bounds.
        var b = new Bounds(boomer.transform.position, Vector3.zero);
        foreach (var r in boomer.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        var cap = boomer.AddComponent<CapsuleCollider>();
        cap.height = Mathf.Max(b.size.y, 0.5f);
        cap.radius = Mathf.Max(b.size.x, b.size.z) * 0.35f;
        cap.center = boomer.transform.InverseTransformPoint(b.center);

        var rb = boomer.AddComponent<Rigidbody>();
        rb.mass = 120f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var ex = boomer.AddComponent<BoomerExplode>();
        ex.gibsPrefab = gibs;
        ex.gibMaterial = GetOrMakeGibMaterial();
        boomer.AddComponent<BoomerFallDeath>();

        var au = boomer.AddComponent<BoomerAudio>();
        au.footsteps = Clips("Audio/Footsteps");
        au.groans = Clips("Audio/Idle");
        au.deathCries = Clips("Audio/Death");
        au.explosions = Clips("Audio/Explode");

        Undo.RegisterCreatedObjectUndo(boomer, "Drop Test Boomer");
        Selection.activeGameObject = boomer;
        Debug.Log("[Boomer] Placed 15 m up. Press Play. Death height is on BoomerFallDeath (default 8 m).");
    }

    static AudioClip[] Clips(string sub)
    {
        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { Dir + sub });
        var list = new AudioClip[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            list[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[i]));
        return list;
    }

    static Material GetOrMakeGibMaterial()
    {
        const string path = Dir + "Boomer_exploded.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat) return mat;
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        mat = new Material(sh);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Textures/Boomer_exploded.png");
        if (tex) { mat.SetTexture("_BaseMap", tex); mat.SetTexture("_MainTex", tex); }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
