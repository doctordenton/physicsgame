using UnityEditor;
using UnityEngine;

/// Settings for the scatter tool (Tools > Food > Scatter Settings), saved in Assets/Food.
public class FoodScatterSettings : ScriptableObject
{
    [Tooltip("How many food items to drop.")] public int count = 60;
    [Tooltip("Food size. 1 = real life (a burger is 13 cm); bigger is easier to see and bump next to the Boomer.")]
    public float foodScale = 2.5f;
    [Tooltip("Keep this clear around the Player's start spot (metres).")] public float innerRadius = 2f;
    [Tooltip("How far out the pile spreads (metres).")] public float outerRadius = 9f;
    [Tooltip("Minimum gap between items (metres).")] public float spacing = 0.6f;
    public int seed = 1;

    const string PathAsset = "Assets/Food/FoodScatterSettings.asset";
    public static FoodScatterSettings Get()
    {
        var s = AssetDatabase.LoadAssetAtPath<FoodScatterSettings>(PathAsset);
        if (!s) { s = CreateInstance<FoodScatterSettings>(); AssetDatabase.CreateAsset(s, PathAsset); }
        return s;
    }
}
