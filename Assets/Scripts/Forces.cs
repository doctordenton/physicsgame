using UnityEngine;

public class Forces : MonoBehaviour
{


    public Controls myControls;
    [SerializeField] float moveSpeed;
    public Rigidbody myRb;
    Vector3 startPosition;
    public Manager myManager;
    Quaternion startRotation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        myControls = GetComponent<Controls>();
        myRb = GetComponent<Rigidbody>();

        startPosition = transform.position;
        startRotation = transform.rotation;
     
        //add a force to my rigid body, along my  transform forward direction, with a magnitude thats multiplied by the y axis , +1 holding w, -1, 
        // add for x, right instead of forward, +1 for d, - 1 for a.
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {


        if (myManager.CanWalk() == false)
        {
            return;
        }
        Vector2 input = myControls.MovementInput;
        myRb.AddForce(transform.right * input.x * moveSpeed);
        myRb.AddForce(transform.forward * input.y * moveSpeed);


    }

    public void ResetPlayer()
    {
        transform.position = startPosition;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        transform.rotation = startRotation;
    }
}



