using UnityEngine;

public class KeyController : MonoBehaviour
{
    private bool hasReachedDoor = false;
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Key collided with: " + collision.gameObject.tag);
        // Only check for door collision if we haven't already reached it
        if (!hasReachedDoor && collision.gameObject.CompareTag("Door"))
        {
            hasReachedDoor = true;
            Debug.Log("Key has reached the door! You win!");
            GameStatus.instance.WinGame();
          
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = true;
        }
    }
}