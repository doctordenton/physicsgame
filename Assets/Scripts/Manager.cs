using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public enum GameState { Freeze, Feast, Fly, Return, Reset }
public class Manager : MonoBehaviour

{
    public GameState state;
    public bool sniped = false;

    public Forces myPlayer;
    public float resetTimerMax = 3f;
    public Player myPlayerFuel;
    float stateTimer;
    public float freezeTimerMax = 3f;
    public float feastTimerMax = 5f;
    public float returnTimerMax = 3f;

    public TMP_Text fuelText;
    public TMP_Text stateText;
    public TMP_Text feastTimerText;
    public TMP_Text countdownText;
    public LaunchPad launchPad;

    void Start()
    {
      
        state = GameState.Freeze;
        stateTimer = freezeTimerMax;
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
                EnterReturn();
                Debug.Log("return time");
            }
        }
        if (state == GameState.Return)
        {
            stateTimer -= Time.deltaTime;

            if (stateTimer < 0)
            {
                if (launchPad.playerInside == false)
                {
                    Sniped();
                    Debug.Log("Sniped!");
                }
                else
                {
                    EnterFly();
                    Debug.Log("Freebird!");
                }
            }
        }

        if (state == GameState.Fly)
        {


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
        feastTimerText.text = "time: " + Mathf.CeilToInt(stateTimer);

        bool showCountdown = state == GameState.Freeze || state == GameState.Return;
        countdownText.gameObject.SetActive(showCountdown);
        countdownText.text = Mathf.CeilToInt(stateTimer).ToString();
    }


    public void EnterFreeze()
    {
        myPlayer.ResetPlayer();
        myPlayerFuel.ResetFuel();
        sniped = false;
        state = GameState.Freeze;
        stateTimer = freezeTimerMax;

    }

    public void EnterReset()
    {
        Debug.Log("reset state started");
        state = GameState.Reset;
        stateTimer = resetTimerMax;
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
    public void EnterReturn()
    {
        state = GameState.Return;
        stateTimer = returnTimerMax;
    }

    public void EnterFly()
    {
        state = GameState.Fly;
       
    }

    public bool CanWalk()
    {
        return state == GameState.Feast || state == GameState.Return || state == GameState.Fly;
    }

    public bool CanFly()
    {

        return state == GameState.Fly;
    }
}


