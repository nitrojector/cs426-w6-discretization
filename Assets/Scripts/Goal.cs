using UnityEngine;

public class Goal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            Debug.Log("Player reached the goal!");
            // You can add more logic here, such as loading the next level or displaying a victory message.
        }
    }
}