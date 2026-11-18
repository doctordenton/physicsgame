using UnityEngine;

public class Pickups : MonoBehaviour
{

    public Player myPlayer;
    public bool eaten;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (eaten == true)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            myPlayer.fuel++;
            eaten = true;
            
        }
    }
}
