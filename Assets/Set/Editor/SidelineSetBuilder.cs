using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// Tools > Set > Build Sideline Set
/// Builds a football-sideline bench area out of Unity primitives and saves it as
/// Assets/Set/SidelineSet.prefab. Drag the prefab into a scene and place it on the ground.
/// Real-world sizes in metres; the root is scaled 0.8 to suit the Boomer (about 1.33 m tall).
/// Big things (benches, tables, coolers, tent) keep colliders; small food and props don't,
/// so they never snag the player.
public static class SidelineSetBuilder
{
    const string Dir = "Assets/Set/";
    const string MatDir = Dir + "Materials/";
    const string PrefabPath = Dir + "SidelineSet.prefab";
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

    [MenuItem("Tools/Set/Build Sideline Set")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(MatDir.TrimEnd('/'))) AssetDatabase.CreateFolder("Assets/Set", "Materials");
        mats.Clear();

        var root = new GameObject("SidelineSet");

        // ---------- ground ----------
        var ground = Group(root, "Ground");
        Box(ground, "Turf", new Vector3(0, -0.01f, 1.5f), new Vector3(16, 0.02f, 7), M("Turf", 0.16f, 0.42f, 0.17f, 0.05f), true);
        Box(ground, "Sideline Stripe", new Vector3(0, 0.002f, 3.2f), new Vector3(16, 0.004f, 0.12f), M("Paint White", 0.95f, 0.95f, 0.93f, 0.1f), false);
        Box(ground, "Coaches Box Line", new Vector3(0, 0.002f, 2.6f), new Vector3(10, 0.004f, 0.06f), M("Paint Yellow", 0.95f, 0.82f, 0.15f, 0.1f), false);

        // ---------- team benches ----------
        var benches = Group(root, "Benches");
        for (int i = 0; i < 2; i++)
        {
            var b = Bench(benches, "Bench " + (i + 1), new Vector3(-4.6f + i * 4.4f, 0, 0));
            // a helmet and a towel on each bench
            Helmet(b, new Vector3(-1.2f + i * 0.9f, 0.48f, 0f));
            Box(b, "Towel", new Vector3(0.6f - i * 0.5f, 0.475f, 0.02f), new Vector3(0.5f, 0.02f, 0.3f), M("Towel White", 0.92f, 0.92f, 0.9f, 0f), false);
        }
        Box(benches, "Towel Draped", new Vector3(-2.9f, 0.4f, 0.21f), new Vector3(0.35f, 0.25f, 0.02f), M("Towel Navy", 0.1f, 0.14f, 0.3f, 0f), false);

        // ---------- drinks table (left, by the benches) ----------
        var drinks = Table(root.transform, "Drinks Table", new Vector3(-6.2f, 0, 1.6f), 1.2f);
        Cooler(drinks, new Vector3(-0.25f, 0.76f, 0f));
        Cooler(drinks, new Vector3(0.3f, 0.76f, 0.05f));
        for (int s = 0; s < 3; s++)   // stacks of cups
            Cyl(drinks, "Cup Stack", new Vector3(-0.45f + s * 0.12f, 0.76f + 0.12f, 0.22f), 0.07f, 0.24f, M("Cup Green", 0.2f, 0.62f, 0.25f, 0.3f), false);
        var carrier = Group(root, "Bottle Carrier");
        carrier.transform.localPosition = new Vector3(-5.3f, 0, 0.6f);
        Box(carrier, "Carrier", new Vector3(0, 0.12f, 0), new Vector3(0.36f, 0.24f, 0.24f), M("Plastic Black", 0.08f, 0.08f, 0.08f, 0.3f), true);
        for (int x = 0; x < 3; x++)
            for (int z = 0; z < 2; z++)
            {
                Cyl(carrier, "Squeeze Bottle", new Vector3(-0.11f + x * 0.11f, 0.32f, -0.055f + z * 0.11f), 0.07f, 0.24f, M("Bottle Clear", 0.85f, 0.9f, 0.95f, 0.6f), false);
                Cyl(carrier, "Bottle Top", new Vector3(-0.11f + x * 0.11f, 0.46f, -0.055f + z * 0.11f), 0.035f, 0.05f, M("Bottle Top", 0.15f, 0.55f, 0.2f, 0.3f), false);
            }

        // ---------- food table under a tent (right) ----------
        var foodArea = Group(root, "Food Area");
        foodArea.transform.localPosition = new Vector3(5.6f, 0, 1.4f);
        var food = Table(foodArea, "Food Table", Vector3.zero, 1.8f);
        PizzaBoxes(food, new Vector3(-0.55f, 0.76f, 0f));
        HotDogTray(food, new Vector3(0.05f, 0.76f, 0.05f));
        OrangeTray(food, new Vector3(0.6f, 0.76f, -0.05f));
        var chips = new[] { M("Chips Red", 0.8f, 0.12f, 0.1f, 0.4f), M("Chips Blue", 0.12f, 0.3f, 0.75f, 0.4f), M("Chips Yellow", 0.95f, 0.75f, 0.12f, 0.4f) };
        for (int c = 0; c < 4; c++)
        {
            var bag = Box(food, "Chip Bag", new Vector3(0.35f + c * 0.13f, 0.76f + 0.1f, 0.26f), new Vector3(0.11f, 0.18f, 0.04f), chips[c % 3], false);
            bag.transform.localRotation = Quaternion.Euler(-12f, c * 7f - 10f, 0f);
        }
        for (int w = 0; w < 5; w++)
            WaterBottle(food, new Vector3(-0.8f + w * 0.08f, 0.76f, 0.27f));
        Tent(foodArea, Vector3.zero);
        IceChest(foodArea, new Vector3(1.3f, 0, 0.7f));

        // ---------- loose props ----------
        var ball = Sph(root, "Football", new Vector3(1.2f, 0.09f, 1.6f), new Vector3(0.28f, 0.17f, 0.17f), M("Leather", 0.42f, 0.2f, 0.09f, 0.25f), false);
        ball.transform.localRotation = Quaternion.Euler(0, 35, 0);
        Box(ball, "Lace", new Vector3(0, 0.5f, 0), new Vector3(0.35f, 0.08f, 0.15f), M("Paint White", 0.95f, 0.95f, 0.93f, 0.1f), false);
        Cyl(root, "Water Bucket", new Vector3(-1.5f, 0.18f, 1.0f), 0.32f, 0.36f, M("Bucket Orange", 0.95f, 0.45f, 0.08f, 0.3f), true);

        root.transform.localScale = Vector3.one * 0.8f;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("[Set] Built " + PrefabPath + ". Drag it into the scene and set its Y so the turf sits on your ground.");
    }

    // ---------- pieces ----------

    static Transform Bench(Transform parent, string name, Vector3 pos)
    {
        var g = Group(parent, name); g.localPosition = pos;
        var metal = M("Aluminium", 0.75f, 0.77f, 0.8f, 0.55f, 0.6f);
        var frame = M("Team Navy", 0.08f, 0.12f, 0.3f, 0.35f);
        Box(g, "Seat", new Vector3(0, 0.45f, 0), new Vector3(4f, 0.05f, 0.38f), metal, true);
        Box(g, "Footrest", new Vector3(0, 0.12f, -0.1f), new Vector3(4f, 0.03f, 0.18f), metal, false);
        for (int i = 0; i < 4; i++)
        {
            float x = -1.8f + i * 1.2f;
            Box(g, "Leg", new Vector3(x, 0.22f, 0.12f), new Vector3(0.05f, 0.44f, 0.05f), frame, false);
            Box(g, "Leg", new Vector3(x, 0.22f, -0.12f), new Vector3(0.05f, 0.44f, 0.05f), frame, false);
            Box(g, "Brace", new Vector3(x, 0.06f, 0), new Vector3(0.05f, 0.04f, 0.3f), frame, false);
        }
        return g;
    }

    static Transform Table(Transform parent, string name, Vector3 pos, float length)
    {
        var g = Group(parent, name); g.localPosition = pos;
        Box(g, "Top", new Vector3(0, 0.74f, 0), new Vector3(length, 0.04f, 0.76f), M("Table White", 0.93f, 0.93f, 0.9f, 0.2f), true);
        Box(g, "Skirt", new Vector3(0, 0.68f, -0.37f), new Vector3(length, 0.1f, 0.01f), M("Table Cloth Navy", 0.1f, 0.14f, 0.32f, 0f), false);
        var leg = M("Steel Dark", 0.25f, 0.25f, 0.27f, 0.4f, 0.7f);
        float hx = length / 2 - 0.08f;
        foreach (var x in new[] { -hx, hx })
            foreach (var z in new[] { -0.3f, 0.3f })
                Cyl(g, "Leg", new Vector3(x, 0.36f, z), 0.035f, 0.72f, leg, false);
        return g;
    }

    static void Cooler(Transform parent, Vector3 pos)
    {
        // Use the real cooler model when it's in the project (Assets/Set/Models/GatoradeCooler.fbx).
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Set/Models/GatoradeCooler.fbx");
        if (model)
        {
            var c = (GameObject)PrefabUtility.InstantiatePrefab(model);
            c.name = "Drink Cooler";
            c.transform.SetParent(parent, false);
            c.transform.localPosition = pos;
            c.transform.localRotation = Quaternion.Euler(0f, 180f + Random.Range(-25f, 25f), 0f);
            var box = c.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.275f, 0f);
            box.size = new Vector3(0.42f, 0.55f, 0.42f);
            return;
        }
        var g = Group(parent, "Drink Cooler"); g.localPosition = pos;
        Cyl(g, "Body", new Vector3(0, 0.22f, 0), 0.34f, 0.44f, M("Cooler Orange", 0.97f, 0.45f, 0.05f, 0.45f), true);
        Cyl(g, "Lid", new Vector3(0, 0.47f, 0), 0.35f, 0.06f, M("Lid White", 0.95f, 0.95f, 0.93f, 0.4f), false);
        Box(g, "Handle", new Vector3(0, 0.52f, 0), new Vector3(0.2f, 0.03f, 0.03f), M("Lid White", 0.95f, 0.95f, 0.93f, 0.4f), false);
        Box(g, "Spout", new Vector3(0, 0.06f, -0.18f), new Vector3(0.05f, 0.05f, 0.06f), M("Lid White", 0.95f, 0.95f, 0.93f, 0.4f), false);
    }

    static void Helmet(Transform parent, Vector3 pos)
    {
        var g = Group(parent, "Helmet"); g.localPosition = pos;
        Sph(g, "Shell", new Vector3(0, 0.11f, 0), new Vector3(0.24f, 0.22f, 0.28f), M("Helmet Navy", 0.08f, 0.12f, 0.32f, 0.85f), false);
        Box(g, "Stripe", new Vector3(0, 0.215f, 0), new Vector3(0.04f, 0.02f, 0.26f), M("Paint White", 0.95f, 0.95f, 0.93f, 0.1f), false);
        var mask = M("Facemask Grey", 0.55f, 0.56f, 0.6f, 0.5f, 0.5f);
        for (int i = 0; i < 3; i++)
            Box(g, "Facemask Bar", new Vector3(0, 0.03f + i * 0.045f, -0.15f), new Vector3(0.18f, 0.012f, 0.012f), mask, false);
    }

    static void PizzaBoxes(Transform parent, Vector3 pos)
    {
        var g = Group(parent, "Pizza"); g.localPosition = pos;
        var card = M("Cardboard", 0.72f, 0.56f, 0.36f, 0.05f);
        for (int i = 0; i < 3; i++)   // closed stack
            Box(g, "Pizza Box", new Vector3(-0.08f, 0.025f + i * 0.05f, -0.05f), new Vector3(0.42f, 0.045f, 0.42f), card, false);
        // open box in front, with the pizza
        var open = Group(g, "Open Box"); open.localPosition = new Vector3(0.12f, 0.17f, 0.1f);
        open.localRotation = Quaternion.Euler(0, -8f, 0);
        Box(open, "Base", new Vector3(0, 0, 0), new Vector3(0.42f, 0.03f, 0.42f), card, false);
        var lid = Box(open, "Lid", new Vector3(0, 0.2f, 0.21f), new Vector3(0.42f, 0.42f, 0.01f), card, false);
        lid.transform.localRotation = Quaternion.Euler(-12f, 0, 0);
        Cyl(open, "Crust", new Vector3(0, 0.02f, 0), 0.38f, 0.015f, M("Crust", 0.86f, 0.62f, 0.3f, 0.1f), false);
        Cyl(open, "Cheese", new Vector3(0, 0.03f, 0), 0.33f, 0.01f, M("Cheese", 0.98f, 0.8f, 0.35f, 0.3f), false);
        var pep = M("Pepperoni", 0.65f, 0.12f, 0.08f, 0.35f);
        for (int i = 0; i < 9; i++)
        {
            float a = i * 40f * Mathf.Deg2Rad, r = (i % 3 == 0) ? 0.05f : 0.11f;
            Cyl(open, "Pepperoni", new Vector3(Mathf.Cos(a) * r, 0.037f, Mathf.Sin(a) * r), 0.05f, 0.006f, pep, false);
        }
    }

    static void HotDogTray(Transform parent, Vector3 pos)
    {
        var g = Group(parent, "Hot Dogs"); g.localPosition = pos;
        Box(g, "Foil Tray", new Vector3(0, 0.02f, 0), new Vector3(0.4f, 0.04f, 0.28f), M("Foil", 0.8f, 0.8f, 0.82f, 0.7f, 0.8f), false);
        var bun = M("Bun", 0.86f, 0.62f, 0.32f, 0.1f);
        var dog = M("Sausage", 0.62f, 0.25f, 0.14f, 0.45f);
        var mustard = M("Mustard", 0.95f, 0.8f, 0.1f, 0.4f);
        for (int i = 0; i < 4; i++)
        {
            float z = -0.09f + i * 0.06f;
            Cap(g, "Bun", new Vector3(0, 0.07f, z), new Vector3(0.055f, 0.09f, 0.05f), bun);
            Cap(g, "Sausage", new Vector3(0, 0.095f, z), new Vector3(0.03f, 0.1f, 0.03f), dog);
            Box(g, "Mustard", new Vector3(0, 0.112f, z), new Vector3(0.14f, 0.008f, 0.008f), mustard, false);
        }
    }

    static void OrangeTray(Transform parent, Vector3 pos)
    {
        var g = Group(parent, "Orange Slices"); g.localPosition = pos;
        Box(g, "Tray", new Vector3(0, 0.015f, 0), new Vector3(0.36f, 0.03f, 0.26f), M("Tray White", 0.95f, 0.95f, 0.93f, 0.3f), false);
        var peel = M("Orange", 0.98f, 0.5f, 0.05f, 0.4f);
        for (int i = 0; i < 12; i++)
        {
            var s = Sph(g, "Orange Slice", new Vector3(-0.13f + (i % 4) * 0.085f, 0.045f, -0.08f + (i / 4) * 0.08f),
                        new Vector3(0.08f, 0.035f, 0.035f), peel, false);
            s.transform.localRotation = Quaternion.Euler(0, (i * 37) % 60 - 30, 0);
        }
    }

    static void WaterBottle(Transform parent, Vector3 pos)
    {
        var g = Group(parent, "Water Bottle"); g.localPosition = pos;
        Cyl(g, "Bottle", new Vector3(0, 0.1f, 0), 0.065f, 0.2f, M("Bottle Clear", 0.85f, 0.9f, 0.95f, 0.6f), false);
        Cyl(g, "Label", new Vector3(0, 0.1f, 0), 0.067f, 0.06f, M("Label Blue", 0.15f, 0.35f, 0.8f, 0.3f), false);
        Cyl(g, "Cap", new Vector3(0, 0.215f, 0), 0.03f, 0.02f, M("Label Blue", 0.15f, 0.35f, 0.8f, 0.3f), false);
    }

    static void IceChest(Transform parent, Vector3 pos)
    {
        var g = Group(parent, "Ice Chest"); g.localPosition = pos;
        Box(g, "Body", new Vector3(0, 0.2f, 0), new Vector3(0.75f, 0.4f, 0.42f), M("Chest Red", 0.75f, 0.1f, 0.1f, 0.4f), true);
        Box(g, "Lid", new Vector3(0, 0.42f, 0), new Vector3(0.77f, 0.05f, 0.44f), M("Lid White", 0.95f, 0.95f, 0.93f, 0.4f), false);
    }

    static void Tent(Transform parent, Vector3 pos)
    {
        var g = Group(parent, "Tent"); g.localPosition = pos;
        var pole = M("Steel Dark", 0.25f, 0.25f, 0.27f, 0.4f, 0.7f);
        foreach (var x in new[] { -1.45f, 1.45f })
            foreach (var z in new[] { -1.25f, 1.25f })
                Cyl(g, "Pole", new Vector3(x, 1.3f, z), 0.05f, 2.6f, pole, true);
        var canvas = M("Canopy Navy", 0.08f, 0.13f, 0.34f, 0.15f);
        Box(g, "Canopy", new Vector3(0, 2.62f, 0), new Vector3(3.1f, 0.06f, 2.7f), canvas, true);
        Box(g, "Valance Front", new Vector3(0, 2.5f, -1.35f), new Vector3(3.1f, 0.22f, 0.02f), canvas, false);
        Box(g, "Valance Back", new Vector3(0, 2.5f, 1.35f), new Vector3(3.1f, 0.22f, 0.02f), canvas, false);
        Box(g, "Stripe", new Vector3(0, 2.5f, -1.365f), new Vector3(3.1f, 0.05f, 0.01f), M("Paint White", 0.95f, 0.95f, 0.93f, 0.1f), false);
    }

    // ---------- helpers ----------

    static Transform Group(GameObject parent, string name) => Group(parent.transform, name);
    static Transform Group(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material m, bool collider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static GameObject Box(Transform p, string n, Vector3 pos, Vector3 size, Material m, bool col) => Prim(PrimitiveType.Cube, p, n, pos, size, m, col);
    static GameObject Box(GameObject p, string n, Vector3 pos, Vector3 size, Material m, bool col) => Box(p.transform, n, pos, size, m, col);
    static GameObject Sph(Transform p, string n, Vector3 pos, Vector3 size, Material m, bool col) => Prim(PrimitiveType.Sphere, p, n, pos, size, m, col);
    static GameObject Sph(GameObject p, string n, Vector3 pos, Vector3 size, Material m, bool col) => Sph(p.transform, n, pos, size, m, col);
    // Unity's cylinder is 2 units tall, so height/2 on Y.
    static GameObject Cyl(Transform p, string n, Vector3 pos, float diameter, float height, Material m, bool col) =>
        Prim(PrimitiveType.Cylinder, p, n, pos, new Vector3(diameter, height / 2f, diameter), m, col);
    static GameObject Cyl(GameObject p, string n, Vector3 pos, float diameter, float height, Material m, bool col) => Cyl(p.transform, n, pos, diameter, height, m, col);
    // Capsule lying along X (hot dogs, buns).
    static GameObject Cap(Transform p, string n, Vector3 pos, Vector3 size, Material m)
    {
        var go = Prim(PrimitiveType.Capsule, p, n, pos, size, m, false);
        go.transform.localRotation = Quaternion.Euler(0, 0, 90);
        return go;
    }

    static Material M(string name, float r, float g, float b, float smooth, float metal = 0f)
    {
        if (mats.TryGetValue(name, out var cached)) return cached;
        string path = MatDir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", new Color(r, g, b));
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        EditorUtility.SetDirty(m);
        mats[name] = m;
        return m;
    }
}
