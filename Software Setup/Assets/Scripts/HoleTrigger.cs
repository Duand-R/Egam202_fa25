using UnityEngine;

public class HoleTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // ball must be tagged "Player"
        {
            GameManager.I.GameOver("You fell into a hole! Press R to restart.");
        }
    }
}
