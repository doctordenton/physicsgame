using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    private Rigidbody rb;
    private Controls controls;
    private Vector3 velocity;
    private Vector3 velocityInput;
    private Vector3 velocityPhysics;
    [SerializeField] private float jumpStartingPush;
    [SerializeField] private float gravity; 
    [SerializeField] private float terminalVelocity;
    [SerializeField] private float speed;
    private bool wasGroundedLastFrame;
    private bool isGrounded;

    [SerializeField] private float fallSpeedMax;
    private bool isOnPlatform;


    private void Start()

    {
        rb = GetComponent<Rigidbody>();
        controls = GetComponent<Controls>();
    }


    private void Update()
    {
        if (controls.JumpTriggered && isOnPlatform)

        {
            // if i am on a platform, jump

            BeginJump();
        }
    }

    void FixedUpdate()
    {
        //input veolcity - horizontal movement logic
        velocityInput.x = controls.MovementInput.x * speed;

        //jumping and gravity llogic

        // apply gravity
        if (!isOnPlatform)
        {
            velocityPhysics.y -= gravity * Time.fixedDeltaTime;

            // cap maximum fall speed
            if (velocityPhysics.y < -terminalVelocity)
            {
                velocityPhysics.y = -terminalVelocity;
            }

        }


        // add up velocityies and apply to rigid body
        velocity = (velocityInput * Time.fixedDeltaTime) + (velocityPhysics * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + velocity);
    }

    private void BeginJump()
    {
        //print("Begin Jump");
        velocityPhysics.y = jumpStartingPush;
    }

    private void OnCollisionStay(Collision collision)
    // did the thing i just collide with, does it have the platform component, using this rather than the tag system
    {
        Platform myPlatform = collision.gameObject.GetComponent<Platform>();
        if (myPlatform != null)

        {
            isOnPlatform = true;
        }
    }


    private void OnCollisionExit(Collision collision)
    // did the thing i just collide with, does it have the platform component, using this rather than the tag system

    {
        Platform myPlatform = collision.gameObject.GetComponent<Platform>();
        if (myPlatform != null)

        {
            isOnPlatform = false;
        }



    }

}