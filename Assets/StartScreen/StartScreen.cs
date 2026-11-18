using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// Start screen: the Boomer stares at the camera. Click (or Space/Enter/gamepad A)
/// and he vomits straight at the camera, then the game scene loads.
public class StartScreen : MonoBehaviour
{
    [Header("Scene references")]
    public Animator boomer;
    public Camera cam;
    public GameObject startText;   // "Start game?"
    public GameObject beginText;   // "Begin!"
    public ParticleSystem vomit;

    [Header("Framing")]
    public string headBoneName = "ValveBiped.Bip01_Head1";
    [Tooltip("How far the camera sits in front of his face (metres).")]
    public float cameraDistance = 1.1f;
    [Tooltip("Aim this far below the head bone so the face sits in the upper half of the screen.")]
    public float aimBelowHead = 0.15f;
    [Tooltip("If the camera ends up beside or behind his head, try 90, 180 or -90.")]
    public float facingYawOffset = 0f;
    [Tooltip("Mouth position from the head bone: x = forward, y = up (metres).")]
    public Vector2 mouthOffset = new Vector2(0.11f, 0.0f);

    [Header("Jaw (JawOpen blend shape)")]
    public SkinnedMeshRenderer face;
    public string jawShapeName = "JawOpen";
    [Tooltip("Seconds for the jaw to open fully.")]
    public float jawOpenTime = 0.25f;
    [Range(0f, 100f)] public float jawOpenAmount = 100f;

    [Header("Timing")]
    public string idleState = "Idle";
    public string vomitState = "Vomit_Standing";
    [Tooltip("Seconds after the click before the vomit comes out (match to the animation).")]
    public float vomitStartDelay = 0.6f;
    public float vomitDuration = 1.6f;
    [Tooltip("Pause after the vomit stops, before the game loads.")]
    public float loadDelay = 0.5f;
    public string gameScene = "SampleScene";

    [Header("Sounds")]
    [Tooltip("Groans while he stands there. Filled from Assets/Boomer/Audio/Idle if left empty.")]
    public AudioClip[] idleSounds;
    [Tooltip("Seconds between idle groans (waits longer if a groan is still playing).")]
    public float idleInterval = 4f;
    [Range(0f, 1f)] public float idleVolume = 0.8f;
    [Tooltip("His retching voice when he throws up. Filled from Assets/Boomer/Audio/Vomit.")]
    public AudioClip[] vomitVoices;
    [Tooltip("The liquid gush of the vomit stream. Filled from Assets/Boomer/Audio/VomitSpray.")]
    public AudioClip vomitSpray;
    [Range(0f, 1f)] public float vomitVolume = 1f;

    Transform head;
    bool started;
    int jawIndex = -1;
    AudioSource voice, spray;
    int lastIdle = -1;

    Vector3 Facing => Quaternion.AngleAxis(facingYawOffset, Vector3.up) * boomer.transform.forward;

    IEnumerator Start()
    {
        head = FindDeep(boomer.transform, headBoneName);
        if (!head) Debug.LogWarning($"[StartScreen] head bone '{headBoneName}' not found, using a guess", this);
        if (!face) face = boomer.GetComponentInChildren<SkinnedMeshRenderer>();
        jawIndex = FindBlendShape(face, jawShapeName);
        if (jawIndex < 0) Debug.LogWarning($"[StartScreen] blend shape '{jawShapeName}' not found, jaw won't open", this);
        if (beginText) beginText.SetActive(false);
        if (startText) startText.SetActive(true);
        boomer.Play(idleState);
        voice = MakeSource("Boomer Voice");
        spray = MakeSource("Vomit Spray");
        yield return null;          // let the Animator pose the skeleton once
        FrameCamera();
        StartCoroutine(IdleSounds());
    }

    void FrameCamera()
    {
        Vector3 headPos = head ? head.position : boomer.transform.position + Vector3.up * 1.6f;
        Vector3 aim = headPos - Vector3.up * aimBelowHead;
        cam.transform.position = aim + Facing * cameraDistance;
        cam.transform.LookAt(aim);
    }

    void Update()
    {
        if (!started && Pressed()) StartCoroutine(Begin());
    }

    void LateUpdate()
    {
        // Keep the vomit emitter on his mouth (the head moves with the animation),
        // pointed straight at the camera.
        if (!vomit) return;
        Vector3 headPos = head ? head.position : boomer.transform.position + Vector3.up * 1.6f;
        Vector3 mouth = headPos + Facing * mouthOffset.x + Vector3.up * mouthOffset.y;
        vomit.transform.position = mouth;
        vomit.transform.rotation = Quaternion.LookRotation(cam.transform.position - mouth);
    }

    static bool Pressed()
    {
        var m = Mouse.current; var k = Keyboard.current; var g = Gamepad.current;
        return (m != null && m.leftButton.wasPressedThisFrame)
            || (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
            || (g != null && g.buttonSouth.wasPressedThisFrame);
    }

    IEnumerator Begin()
    {
        started = true;             // also ends the idle groans
        if (startText) startText.SetActive(false);
        if (beginText) beginText.SetActive(true);
        boomer.CrossFade(vomitState, 0.15f);
        if (voice) voice.Stop();    // cut off any groan that's mid-play

        // Open the jaw so it's fully open right as the vomit comes out.
        // The retching voice starts with the jaw, like the wind-up in the game.
        float openAt = Mathf.Max(0f, vomitStartDelay - jawOpenTime);
        yield return new WaitForSeconds(openAt);
        Play(voice, Pick(vomitVoices, -1, out _), vomitVolume);
        yield return Jaw(0f, jawOpenAmount, jawOpenTime);
        yield return new WaitForSeconds(vomitStartDelay - openAt - jawOpenTime);

        vomit.Play(true);
        Play(spray, vomitSpray, vomitVolume);
        yield return new WaitForSeconds(vomitDuration);
        vomit.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (spray) StartCoroutine(FadeOut(spray, 0.4f));   // the gush clip is longer than the vomit
        yield return Jaw(jawOpenAmount, 0f, jawOpenTime * 1.5f);
        yield return new WaitForSeconds(loadDelay);

        SceneManager.LoadScene(gameScene);
    }

    IEnumerator IdleSounds()
    {
        yield return new WaitForSeconds(1f);    // first groan shortly after the scene opens
        while (!started)
        {
            var clip = Pick(idleSounds, lastIdle, out lastIdle);   // never the same groan twice in a row
            Play(voice, clip, idleVolume);
            float wait = clip ? Mathf.Max(idleInterval, clip.length + 0.3f) : idleInterval;
            for (float t = 0f; t < wait && !started; t += Time.deltaTime) yield return null;
        }
    }

    AudioSource MakeSource(string name)
    {
        // 2D sound: the camera is right in his face, so no 3D falloff needed.
        var src = new GameObject(name).AddComponent<AudioSource>();
        src.transform.SetParent(transform, false);
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        return src;
    }

    static void Play(AudioSource src, AudioClip clip, float volume)
    {
        if (!src || !clip) return;
        src.clip = clip;
        src.volume = volume;
        src.Play();
    }

    static AudioClip Pick(AudioClip[] clips, int avoid, out int picked)
    {
        picked = -1;
        if (clips == null || clips.Length == 0) return null;
        picked = Random.Range(0, clips.Length);
        if (clips.Length > 1 && picked == avoid) picked = (picked + 1 + Random.Range(0, clips.Length - 1)) % clips.Length;
        return clips[picked];
    }

    static IEnumerator FadeOut(AudioSource src, float time)
    {
        float v = src.volume;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            src.volume = Mathf.Lerp(v, 0f, t / time);
            yield return null;
        }
        src.Stop();
    }

#if UNITY_EDITOR
    // Fills any empty sound slots from the project folders, so the existing scene
    // picks the clips up without dragging them in by hand. Save the scene afterwards.
    void OnValidate() => FillSoundsFromProject();

    public void FillSoundsFromProject()
    {
        bool changed = false;
        if (idleSounds == null || idleSounds.Length == 0) { idleSounds = LoadClips("Assets/Boomer/Audio/Idle"); changed |= idleSounds.Length > 0; }
        if (vomitVoices == null || vomitVoices.Length == 0) { vomitVoices = LoadClips("Assets/Boomer/Audio/Vomit"); changed |= vomitVoices.Length > 0; }
        if (!vomitSpray)
        {
            var s = LoadClips("Assets/Boomer/Audio/VomitSpray");
            if (s.Length > 0) { vomitSpray = s[0]; changed = true; }
        }
        if (changed) UnityEditor.EditorUtility.SetDirty(this);
    }

    static AudioClip[] LoadClips(string folder)
    {
        if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) return new AudioClip[0];
        var list = new System.Collections.Generic.List<AudioClip>();
        foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
        {
            var c = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
            if (c) list.Add(c);
        }
        return list.ToArray();
    }
#endif

    IEnumerator Jaw(float from, float to, float time)
    {
        if (jawIndex < 0) yield break;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / time);
            face.SetBlendShapeWeight(jawIndex, Mathf.Lerp(from, to, k));
            yield return null;
        }
        face.SetBlendShapeWeight(jawIndex, to);
    }

    static int FindBlendShape(SkinnedMeshRenderer smr, string name)
    {
        if (!smr || !smr.sharedMesh) return -1;
        var m = smr.sharedMesh;
        for (int i = 0; i < m.blendShapeCount; i++)
            if (m.GetBlendShapeName(i).EndsWith(name)) return i;
        return -1;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            var r = FindDeep(c, name);
            if (r) return r;
        }
        return null;
    }
}
