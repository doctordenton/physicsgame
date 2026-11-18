using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Tools > Food > 1. Make Food Prefabs
///   For every model in Assets/Food/Models: makes URP materials from Assets/Food/Textures,
///   then a prefab in Assets/Food/Prefabs with a Rigidbody + convex collider, tagged "Food",
///   so the Boomer can bump it around. The two table sets are solid scenery (no Rigidbody).
/// Tools > Food > 2. Scatter Food Around Player
///   Drops a random pile of food prefabs on the floor around the Player, under "Food Pile".
///   Running it again replaces the old pile.
public static class FoodSetup
{
    const string Root = "Assets/Food/";
    const string Models = Root + "Models";
    const string Textures = Root + "Textures/";
    const string Mats = Root + "Materials";
    const string Prefabs = Root + "Prefabs";
    public const string FoodTag = "Food";
    static readonly string[] Scenery = { "SushiTableSet", "BanquetTableSet" };
    // prefabs the scatter tool never places: thin flat plates/boards jitter and glitch in physics
    static readonly string[] NoScatter = { "WoodBoard", "CuttingBoard", "ServingPlate" };

    [MenuItem("Tools/Food/1. Make Food Prefabs")]
    public static void MakePrefabs()
    {
        EnsureFolder(Root.TrimEnd('/'), "Materials");
        EnsureFolder(Root.TrimEnd('/'), "Prefabs");
        EnsureTag(FoodTag);

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        int made = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Models }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);

            // materials: one URP Lit per texture, matched by the material name inside the FBX
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            imp.importAnimation = false;
            imp.animationType = ModelImporterAnimationType.None;
            foreach (var srcMat in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
            {
                var m = GetMaterial(srcMat.name, shader);
                if (m) imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), srcMat.name), m);
            }
            imp.SaveAndReimport();

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            var mf = go.GetComponentInChildren<MeshFilter>();
            bool scenery = Scenery.Contains(name);

            var size = mf.sharedMesh.bounds.size;
            size.Scale(mf.transform.lossyScale);
            if (size.magnitude > (scenery ? 6f : 1.5f))
                Debug.LogWarning($"[Food] {name} imported at an odd size {size} - check its Scale Factor.");

            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            if (!scenery)
            {
                mc.convex = true;
                var rb = go.AddComponent<Rigidbody>();
                // light, so the Boomer sends it flying; bigger things are a bit heavier
                rb.mass = Mathf.Clamp(size.x * size.y * size.z * 300f, 0.2f, 3f);
                rb.linearDamping = 0.1f;
                rb.angularDamping = 0.3f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                go.tag = FoodTag;
                foreach (Transform t in go.GetComponentsInChildren<Transform>()) t.gameObject.tag = FoodTag;
            }
            else go.isStatic = true;

            PrefabUtility.SaveAsPrefabAsset(go, $"{Prefabs}/{name}.prefab");
            Object.DestroyImmediate(go);
            made++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[Food] Made {made} prefabs in {Prefabs}. Food items are tagged \"{FoodTag}\".");
    }

    [MenuItem("Tools/Food/2. Scatter Food Around Player")]
    public static void Scatter()
    {
        var player = GameObject.Find("Player");
        Vector3 centre = player ? player.transform.position : Vector3.zero;
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { Prefabs })
            .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(p => p && p.CompareTag(FoodTag) && !NoScatter.Contains(p.name)).ToList();
        if (prefabs.Count == 0) { Debug.LogError("[Food] No food prefabs yet - run Tools > Food > 1 first."); return; }

        var old = GameObject.Find("Food Pile");
        if (old) Undo.DestroyObjectImmediate(old);
        var pile = new GameObject("Food Pile");
        Undo.RegisterCreatedObjectUndo(pile, "Scatter Food");

        var s = FoodScatterSettings.Get();
        var rng = new System.Random(s.seed);
        int placed = 0, tries = 0;
        var spots = new List<Vector3>();
        while (placed < s.count && tries++ < s.count * 30)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f;
            float r = Mathf.Lerp(s.innerRadius, s.outerRadius, Mathf.Sqrt((float)rng.NextDouble()));
            var p = centre + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            if (spots.Any(q => (q - p).sqrMagnitude < s.spacing * s.spacing)) continue;
            // find the floor under this spot (ignore the player and other food)
            var hits = Physics.RaycastAll(p + Vector3.up * 30f, Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => !h.collider.CompareTag(FoodTag) && !(player && h.collider.transform.IsChildOf(player.transform)))
                .OrderBy(h => h.distance).ToArray();
            if (hits.Length == 0) continue;
            var prefab = prefabs[rng.Next(prefabs.Count)];
            var f = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pile.transform);
            f.transform.localScale = Vector3.one * s.foodScale;
            f.transform.SetPositionAndRotation(hits[0].point + Vector3.up * 0.02f, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f));
            spots.Add(p);
            placed++;
        }
        s.seed++;   // next run gives a different pile
        EditorSceneManager.MarkSceneDirty(pile.scene);
        Selection.activeGameObject = pile;
        Debug.Log($"[Food] Scattered {placed} food items around {(player ? "the Player" : "the origin")}. Save the scene.");
    }

    [MenuItem("Tools/Food/Scatter Settings")]
    static void SelectSettings() => Selection.activeObject = FoodScatterSettings.Get();

    static Material GetMaterial(string matName, Shader shader)
    {
        string tex = Textures + matName + ".jpg";
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(tex);
        if (!t) { Debug.LogWarning("[Food] No texture for material " + matName); return null; }
        string mp = $"{Mats}/{matName}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
        if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, mp); }
        m.SetTexture("_BaseMap", t);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0.25f);
        m.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child);
    }

    static void EnsureTag(string tag)
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tm.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tm.ApplyModifiedProperties();
    }
}
