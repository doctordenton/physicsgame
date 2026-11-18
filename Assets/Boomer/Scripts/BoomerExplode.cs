using UnityEngine;

/// Pops the Boomer: hides his body, spawns physics gibs flung outward,
/// and plays a procedural bile/gore particle burst.
public class BoomerExplode : MonoBehaviour
{
    [Header("Gibs")]
    [Tooltip("The BoomerGibs.fbx model (Gib_Body, Gib_Head, Gib_RArm, Gib_Steak1-3).")]
    public GameObject gibsPrefab;
    [Tooltip("Optional material override for the gibs (Boomer_exploded texture).")]
    public Material gibMaterial;
    public float explosionForce = 9f;
    public float explosionRadius = 3f;
    public float upwardsModifier = 1.2f;
    [Tooltip("Seconds before gibs are cleaned up (0 = never).")]
    public float gibLifetime = 20f;

    [Header("Effects")]
    [Tooltip("The game's explosion mist (BoomerExplosionMist prefab from Tools > Boomer > Build Game Explosion Mist). " +
             "Replaces the made-up mist when set.")]
    public GameObject explosionEffect;
    [Tooltip("Seconds before the spawned mist is cleaned up.")]
    public float effectLifetime = 6f;
    [Tooltip("Optional particle material. Left empty, a URP particle material is made at runtime.")]
    public Material particleMaterial;
    public Color bileColor = new Color(0.55f, 0.65f, 0.15f, 1f);
    public Color goreColor = new Color(0.45f, 0.05f, 0.04f, 1f);
    public AudioClip explodeSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    public bool HasExploded { get; private set; }
    readonly System.Collections.Generic.List<Component> hidden = new System.Collections.Generic.List<Component>();
    GameObject spawnedGibs;

    /// Puts the Boomer back together after Explode(): shows his body, turns his colliders,
    /// animator and physics back on, and clears away the gibs. Safe to call when he hasn't exploded.
    public void Revive()
    {
        if (!HasExploded) return;
        HasExploded = false;
        foreach (var c in hidden)
        {
            if (c is Renderer r) r.enabled = true;
            else if (c is Collider col) col.enabled = true;
            else if (c is Behaviour b) b.enabled = true;
        }
        hidden.Clear();
        var anim = GetComponentInChildren<Animator>();
        if (anim) { anim.Rebind(); anim.Update(0f); }   // back to the idle pose
        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false; rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
        }
        if (spawnedGibs) Destroy(spawnedGibs);
    }

    public void Explode() => Explode(Vector3.zero);

    public void Explode(Vector3 impactVelocity)
    {
        if (HasExploded) return;
        HasExploded = true;

        Vector3 center = transform.position + Vector3.up * 0.6f;

        // 1. Hide the living Boomer (remembering what was on, so Revive() can put it back).
        hidden.Clear();
        foreach (var r in GetComponentsInChildren<Renderer>()) if (r.enabled) { hidden.Add(r); r.enabled = false; }
        foreach (var col in GetComponentsInChildren<Collider>()) if (col.enabled) { hidden.Add(col); col.enabled = false; }
        var anim = GetComponentInChildren<Animator>();
        if (anim && anim.enabled) { hidden.Add(anim); anim.enabled = false; }
        var rb = GetComponent<Rigidbody>();
        if (rb) { rb.isKinematic = true; rb.detectCollisions = false; }

        // 2. Gibs.
        if (gibsPrefab)
        {
            var gibs = Instantiate(gibsPrefab, transform.position, transform.rotation);
            gibs.name = "BoomerGibs (spawned)";
            spawnedGibs = gibs;
            foreach (var mf in gibs.GetComponentsInChildren<MeshFilter>())
            {
                var go = mf.gameObject;
                if (gibMaterial) go.GetComponent<MeshRenderer>().sharedMaterial = gibMaterial;
                var mc = go.AddComponent<MeshCollider>();
                mc.convex = true;
                var grb = go.AddComponent<Rigidbody>();
                grb.mass = go.name.Contains("Body") ? 20f : 3f;
                grb.interpolation = RigidbodyInterpolation.Interpolate;
                grb.linearVelocity = impactVelocity * -0.15f; // a bit of bounce from the landing
                grb.AddExplosionForce(explosionForce * grb.mass, center, explosionRadius, upwardsModifier, ForceMode.Impulse);
                grb.AddTorque(Random.insideUnitSphere * 5f * grb.mass, ForceMode.Impulse);
            }
            if (gibLifetime > 0f) Destroy(gibs, gibLifetime);
        }

        // 3. Particles + sound.
        if (!explosionEffect)   // made-up bursts, only when the game's effect isn't set
        {
            SpawnBurst(center, bileColor, 160, 2.5f, 7f, 0.25f, 0.6f);   // bile spray
            SpawnBurst(center, goreColor, 60, 1.5f, 5f, 0.08f, 0.2f);     // gore chunks
        }
        if (explosionEffect)
        {
            var fx = Instantiate(explosionEffect, center, Quaternion.identity);
            fx.name = "BoomerExplosionMist (spawned)";
            Destroy(fx, effectLifetime);
        }
        else SpawnMist(center);
        if (explodeSound) AudioSource.PlayClipAtPoint(explodeSound, center, soundVolume);
        var audio = GetComponent<BoomerAudio>();
        if (audio) audio.PlayDeath(center);
    }

    void SpawnBurst(Vector3 pos, Color color, int count, float minSpeed, float maxSpeed, float minSize, float maxSize)
    {
        var go = new GameObject("BoomerBurst");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = color;
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var col = ps.collision;
        col.enabled = true;
        col.type = ParticleSystemCollisionType.World;
        col.dampen = 0.6f;
        col.bounce = 0.1f;
        col.lifetimeLoss = 0.2f;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.3f));

        ApplyMaterial(go);
        ps.Play();
    }

    void SpawnMist(Vector3 pos)
    {
        var go = new GameObject("BoomerMist");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        var c = bileColor; c.a = 0.35f;
        main.startColor = c;
        main.gravityModifier = -0.02f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)25) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
        colorOverLife.color = g;

        ApplyMaterial(go);
        ps.Play();
    }

    void ApplyMaterial(GameObject go)
    {
        var r = go.GetComponent<ParticleSystemRenderer>();
        if (!particleMaterial)
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Particles/Standard Unlit")
                  ?? Shader.Find("Sprites/Default");
            particleMaterial = new Material(sh);
        }
        r.sharedMaterial = particleMaterial;
    }
}
