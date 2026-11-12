using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float clickForce;
    public float upForce;
    private Rigidbody rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame) //check if left mouse button was just clicked
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
               // }
            }


        }
    }
}
