using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float clickForce;
    private Rigidbody rb;
    Controls controls;
    public float maxFuel = 10f;
    public float fuel;
    public Manager myManager;
    public Animator myAnimator;
    public ParticleSystem vomitSpray;
    public AudioSource vomitVoice;
    public AudioSource vomitSfx;
    public AudioClip[] vomitVoices;
    bool wasVomiting;
   
    public float groundY;
    public float hangTime = 1f;
    public float fallGravity = 2f;
    public float maxFallSpeed = 20f;
    float fallTimer;
    public float deathSpeed = 15f;
    public float eatRange = 1.5f;
    public float fuelGain = .75f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        controls = GetComponent<Controls>();
        myAnimator = GetComponentInChildren<Animator>();
        fuel = maxFuel;
        groundY = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        /*if (Mouse.current.leftButton.wasReleasedThisFrame) //check if left mouse button was just clicked
        {
            //where did the mouse click in terms of the world. 
            Ray mouseCameraRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit mouseRaycastHit;

            if (Physics.Raycast(mouseCameraRay, out mouseRaycastHit))
            {

                //did the mouse raycast just hit this ball's collider?
                //if (mouseRaycastHit.collider.gameObject == this.gameObject)
                //{
                    //push the ball away from the position where the raycast hit it
                    Vector3 forceDirection = transform.position - mouseRaycastHit.point;
                    forceDirection.Normalize();
                forceDirection.y = upForce;
                    rb.AddForceAtPosition(forceDirection * clickForce,
                        mouseRaycastHit.point, ForceMode.Impulse);
               // } */

        /* if (controls.AttackTriggered)
         {
             rb.AddForce(-Camera.main.transform.forward * clickForce, ForceMode.Impulse);

         } */



        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            Collider[] near = Physics.OverlapSphere(transform.position, eatRange);

            foreach (Collider c in near)
            {
                if (c.CompareTag("Food"))
                {
                    fuel += fuelGain;
                    Destroy(c.attachedRigidbody.gameObject);
                    break;
                }
            }
        }
    }
            

           /*if (f)
            

            if (c = )        // is this one food?
            {
                ___           // 1. add foodFuel to fuel
                ___           // 2. destroy the whole food item
            ___           // 3. stop the loop
        }
        }
            */



    private void FixedUpdate()
    {

        bool vomiting = myManager.CanFly() && controls.AttackHeld && fuel > 0;
            myAnimator.SetBool("Vomiting", vomiting);
        if (vomiting && !vomitSpray.isPlaying) vomitSpray.Play();
        if (!vomiting && vomitSpray.isPlaying) vomitSpray.Stop();
        if (vomiting && !vomitSfx.isPlaying) vomitSfx.Play();
        if (!vomiting && vomitSfx.isPlaying) vomitSfx.Stop();

        if (vomiting && !wasVomiting) vomitVoice.PlayOneShot(vomitVoices[Random.Range(0, vomitVoices.Length)]);
        wasVomiting = vomiting;

        if (myManager.CanFly() == false)
        {
            return;
        }

        // if airborne damping goes down, damping goes down
        bool airborne = transform.position.y > groundY + 0.5f;
        rb.linearDamping = airborne ? 0.8f : 4f;


        if (airborne) // fall timer
        { 
            if (vomiting) fallTimer = 0; else fallTimer += Time.fixedDeltaTime;
            float hang = 1 - Mathf.Clamp01(fallTimer / hangTime); //
            rb.AddForce(Vector3.up * 9.81f * hang, ForceMode.Acceleration); //upward push
            rb.AddForce(Vector3.down * 9.81f * fallGravity * (1 - hang), ForceMode.Acceleration);
            Vector3 v = rb.linearVelocity;
            if (v.y < -maxFallSpeed) { v.y = -maxFallSpeed; rb.linearVelocity = v; } //speed cap
        }



        //add force opposite of camera
        if (controls.AttackHeld && fuel > 0)
        {
            rb.AddForce(-Camera.main.transform.forward * clickForce);
            fuel = fuel - Time.deltaTime;
        }

    }

    public void ResetFuel()
    {

        fuel = maxFuel;
    }
    void LateUpdate()
        // late update goes after animations , aim vomit spray toward camera
    { vomitSpray.transform.rotation = Camera.main.transform.rotation; }

    void OnCollisionEnter(Collision collision)
    { 
        Debug.Log("hit speed: " + collision.relativeVelocity.magnitude);

        if (myManager.CanFly()   && collision.relativeVelocity.magnitude > 18f)
        {
            GetComponent<BoomerExplode>().Explode(collision.relativeVelocity);

        }
    }
}