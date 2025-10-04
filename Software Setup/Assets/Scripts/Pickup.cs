using UnityEngine;

public class Pickup : MonoBehaviour
{
    public int points = 1;

    void OnTriggerEnter(Collider other)
    {
        if (!other.attachedRigidbody) return;
        if (!other.attachedRigidbody.CompareTag("Player")) return;

        if (GameManager.I != null)
        {
            GameManager.I.AddScore(points);
            GameManager.I.OnPickupConsumed();
        }

        Destroy(gameObject);
    }
}
