using UnityEngine;
using UnityEngine.InputSystem;




public class Controls : MonoBehaviour
{
    PlayerInput playerInput;



    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    public Vector2 MovementInput => playerInput.actions["Move"].ReadValue<Vector2>();
    public Vector2 LookInput => playerInput.actions["Look"].ReadValue<Vector2>();
    public bool JumpTriggered => playerInput.actions["Jump"].triggered;
    public bool JumpHeld => playerInput.actions["Jump"].ReadValue<float>() > 0.9f;
    public bool AttackHeld => playerInput.actions["Attack"].ReadValue<float>() > 0.9f;
    public bool AttackTriggered => playerInput.actions["Attack"].triggered;



}