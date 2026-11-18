using UnityEngine;

/// Tracks how far the Boomer falls. If he lands after dropping more than
/// `deathHeight` metres, he explodes (BoomerExplode). Shorter drops are fine.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoomerExplode))]
public class BoomerFallDeath : MonoBehaviour
{
    [Tooltip("Drop distance in metres that kills him.")]
    public float deathHeight = 8f;

    [Tooltip("Contacts with a normal steeper than this count as ground (0..1).")]
    [Range(0f, 1f)] public float groundNormalY = 0.5f;

    [Tooltip("Log fall distances to the Console.")]
    public bool debugLog = true;

    Rigidbody rb;
    BoomerExplode exploder;
    bool grounded;
    float peakY;
    int groundContacts;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        exploder = GetComponent<BoomerExplode>();
        peakY = transform.position.y;
    }

    void FixedUpdate()
    {
        // While airborne, remember the highest point reached.
        if (!grounded && transform.position.y > peakY)
            peakY = transform.position.y;
    }

    void OnCollisionEnter(Collision c)
    {
        if (!IsGround(c)) return;
        groundContacts++;
        if (grounded) return;

        grounded = true;
        float drop = peakY - transform.position.y;
        if (debugLog) Debug.Log($"[Boomer] landed after a {drop:F2} m drop (limit {deathHeight} m)", this);

        if (drop >= deathHeight)
            exploder.Explode(c.relativeVelocity);
    }

    void OnCollisionExit(Collision c)
    {
        if (groundContacts > 0) groundContacts--;
        if (groundContacts == 0)
        {
            grounded = false;
            peakY = transform.position.y; // start measuring from where he left the ground
        }
    }

    bool IsGround(Collision c)
    {
        for (int i = 0; i < c.contactCount; i++)
            if (c.GetContact(i).normal.y >= groundNormalY) return true;
        return false;
    }
}
