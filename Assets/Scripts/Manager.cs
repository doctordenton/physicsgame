using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public enum GameState { Freeze, Feast, Fly, Reset }
public class Manager : MonoBehaviour

{
    public GameState state;
    public bool sniped = false;

    public Forces myPlayer;
    public float resetTimerMax = 3f;
    public Player myPlayerFuel;
    float stateTimer;
    public float freezeTimerMax = 3f;
    public float feastTimerMax = 10f;

    public TMP_Text fuelText;
    public TMP_Text stateText;
    public TMP_Text feastTimerText;
    public TMP_Text countdownText;
    public Rigidbody fallingFloor;
    public GameObject feastWall;
    public GameObject[] foodPilePrefabs;
    GameObject currentPile;
    [Tooltip("Piles to use for the first rounds, by number (1 = first pile in the list). After these run out, piles are random.")]
    public int[] firstPiles = { 1, 4, 3 };
    int roundsSpawned;
    Vector3 floorStartPos;
    Quaternion floorStartRot;
    [Tooltip("Spin given to the floor when it drops, in radians per second (X = tip forward/back, Z = roll sideways).")]
    public Vector3 floorSpin = new Vector3(0.15f, 0f, 0.6f);
    [Tooltip("Downward speed the floor starts with when it drops (m/s), so the fall is obvious straight away.")]
    public float floorDropSpeed = 4f;

    // distance meter: how far along Z we get past the start line (the feast wall) each run
    public TMP_Text distanceText;
    public TMP_Text bestDistanceText;
    float distance;        // this run
    float bestDistance;    // best ever, saved between plays with PlayerPrefs


    void Start()
    {

        state = GameState.Freeze;
        stateTimer = freezeTimerMax;
        floorStartPos = fallingFloor.transform.position;
        floorStartRot = fallingFloor.transform.rotation;
        SpawnFood();
        bestDistance = PlayerPrefs.GetFloat("bestDistance", 0f);   // load the saved best (0 if none yet)
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Sniped();
        }

        if (state == GameState.Reset)
        {
            stateTimer -= Time.deltaTime;
            Debug.Log("reset timer began");

            if (stateTimer < 0)
            {
                EnterFreeze();
                Debug.Log("Freeze entered");
            }
        }
        if (state == GameState.Freeze)
        {
            stateTimer -= Time.deltaTime;

            if (stateTimer < 0)
            {
                EnterFeast();
                Debug.Log("Feast time");
            }
        }

        if (state == GameState.Feast)
        {
            stateTimer -= Time.deltaTime;

            if (stateTimer < 0)
            {
                EnterFly();
                Debug.Log("Freebird!");
            }
        }

        if (state == GameState.Fly)
        {
            // how far past the start line we are; keep the furthest point reached this run
            float now = Mathf.Abs(myPlayer.transform.position.z - feastWall.transform.position.z);
            distance = Mathf.Max(distance, now);
            if (distance > bestDistance) bestDistance = distance;


            /* if (stateTimer < 0)
             {
                 Sniped();
                 Debug.Log("Sniped!");
             } */
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        stateText.text = "state: " + state;
        fuelText.text = "fuel: " + myPlayerFuel.fuel.ToString("F1");

        feastTimerText.gameObject.SetActive(state == GameState.Feast);
        feastTimerText.text = Mathf.CeilToInt(stateTimer).ToString();

        bool showCountdown = state == GameState.Freeze;
        countdownText.gameObject.SetActive(showCountdown);
        countdownText.text = Mathf.CeilToInt(stateTimer).ToString();

        if (distanceText) distanceText.text = "distance: " + Mathf.FloorToInt(distance) + " m";
        if (bestDistanceText) bestDistanceText.text = "best: " + Mathf.FloorToInt(bestDistance) + " m";
    }


    public void EnterFreeze()
    {
        myPlayer.ResetPlayer();
        myPlayerFuel.ResetFuel();
        sniped = false;
        state = GameState.Freeze;
        stateTimer = freezeTimerMax;
        myPlayer.myRb.linearDamping = 4f;
        // stop the floor's fall and spin, then put it back flat where it started
        if (!fallingFloor.isKinematic)
        {
            fallingFloor.linearVelocity = Vector3.zero;
            fallingFloor.angularVelocity = Vector3.zero;
        }
        fallingFloor.isKinematic = true;
        fallingFloor.transform.SetPositionAndRotation(floorStartPos, floorStartRot);
        SpawnFood();
        myPlayer.GetComponent<BoomerExplode>().Revive();
        feastWall.SetActive(true);
        distance = 0f;   // new run starts at 0

    }

    void SpawnFood()
    {
        if (currentPile != null) Destroy(currentPile);
        // first rounds follow the set order (pile 1, 4, 3), then pick at random
        int index = roundsSpawned < firstPiles.Length
            ? firstPiles[roundsSpawned] - 1
            : Random.Range(0, foodPilePrefabs.Length);
        roundsSpawned++;
        currentPile = Instantiate(foodPilePrefabs[Mathf.Clamp(index, 0, foodPilePrefabs.Length - 1)]);
    }

    public void EnterReset()
    {
        Debug.Log("reset state started");
        state = GameState.Reset;
        stateTimer = resetTimerMax;

        // run's over: save the best distance to disk so it's still there next time we play
        PlayerPrefs.SetFloat("bestDistance", bestDistance);
        PlayerPrefs.Save();
    }


    void Sniped()
    {
        sniped = true;
        EnterReset();
    }

    public void EnterFeast()
    {
        state = GameState.Feast;
        stateTimer = feastTimerMax;
    }

    public void EnterFly()
    {
        state = GameState.Fly;
        fallingFloor.isKinematic = false;
        fallingFloor.angularVelocity = floorSpin;   // start it tumbling so you can see it's falling
        fallingFloor.linearVelocity = Vector3.down * floorDropSpeed;   // and give it a shove down so it drops right away
        feastWall.SetActive(false);


    }

    // also save if the game is closed mid-run
    void OnApplicationQuit()
    {
        PlayerPrefs.SetFloat("bestDistance", bestDistance);
        PlayerPrefs.Save();
    }

    public bool CanWalk()
    {
        return state == GameState.Feast || state == GameState.Fly;
    }

    public bool CanFly()
    {

        return state == GameState.Fly;

    }
}


