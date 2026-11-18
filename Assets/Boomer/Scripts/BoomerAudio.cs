using UnityEngine;

/// Boomer sounds: footsteps while the Walk animation plays, random idle groans,
/// and death + explosion sounds (called by BoomerExplode).
[RequireComponent(typeof(AudioSource))]
public class BoomerAudio : MonoBehaviour
{
    [Header("Footsteps")]
    public AudioClip[] footsteps;
    [Tooltip("Seconds between steps. Walk cycle is ~1.27 s with 2 steps.")]
    public float stepInterval = 0.63f;
    [Tooltip("Animator state that counts as walking.")]
    public string walkStateName = "Walk";
    [Range(0f, 1f)] public float footstepVolume = 0.7f;

    [Header("Idle groans")]
    public AudioClip[] groans;
    public Vector2 groanDelay = new Vector2(3f, 7f);
    [Range(0f, 1f)] public float groanVolume = 0.9f;

    [Header("Death")]
    public AudioClip[] deathCries;
    public AudioClip[] explosions;
    [Range(0f, 1f)] public float deathVolume = 1f;

    AudioSource voice;      // groans (one at a time so they don't overlap)
    AudioSource feet;       // footsteps
    Animator anim;
    BoomerExplode exploder;
    float stepTimer, groanTimer;
    int lastStep = -1;

    void Awake()
    {
        voice = GetComponent<AudioSource>();
        voice.spatialBlend = 1f;          // 3D sound: louder when you're close
        voice.playOnAwake = false;
        voice.minDistance = 2f;
        voice.maxDistance = 30f;

        feet = gameObject.AddComponent<AudioSource>();
        feet.spatialBlend = 1f;
        feet.playOnAwake = false;
        feet.minDistance = 1.5f;
        feet.maxDistance = 20f;

        anim = GetComponentInChildren<Animator>();
        exploder = GetComponent<BoomerExplode>();
        groanTimer = Random.Range(groanDelay.x, groanDelay.y);
    }

    void Update()
    {
        if (exploder && exploder.HasExploded) return;

        // Footsteps: only while the Walk state is playing.
        if (anim && anim.enabled && anim.GetCurrentAnimatorStateInfo(0).IsName(walkStateName))
        {
            stepTimer -= Time.deltaTime * anim.speed;
            if (stepTimer <= 0f)
            {
                stepTimer += stepInterval;
                PlayFootstep();
            }
        }

        // Groans: random gaps, skip if one is still playing.
        groanTimer -= Time.deltaTime;
        if (groanTimer <= 0f)
        {
            groanTimer = Random.Range(groanDelay.x, groanDelay.y);
            if (!voice.isPlaying && groans != null && groans.Length > 0)
            {
                voice.pitch = Random.Range(0.95f, 1.05f);
                voice.PlayOneShot(Pick(groans), groanVolume);
            }
        }
    }

    void PlayFootstep()
    {
        if (footsteps == null || footsteps.Length == 0) return;
        // Avoid playing the same step twice in a row.
        int i = Random.Range(0, footsteps.Length);
        if (footsteps.Length > 1 && i == lastStep) i = (i + 1) % footsteps.Length;
        lastStep = i;
        feet.pitch = Random.Range(0.92f, 1.08f);
        feet.PlayOneShot(footsteps[i], footstepVolume);
    }

    /// Called by BoomerExplode at the moment he pops.
    public void PlayDeath(Vector3 at)
    {
        voice.Stop();
        if (deathCries != null && deathCries.Length > 0)
            AudioSource.PlayClipAtPoint(Pick(deathCries), at, deathVolume);
        if (explosions != null && explosions.Length > 0)
            AudioSource.PlayClipAtPoint(Pick(explosions), at, deathVolume);
    }

    static AudioClip Pick(AudioClip[] clips) => clips[Random.Range(0, clips.Length)];
}
