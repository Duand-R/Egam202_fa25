using UnityEngine;

public class Hazard : MonoBehaviour
{
    [Header("Penalty")]
    public int scorePenalty = 3;

    void OnTriggerEnter(Collider other)
    {
        TryHit(other.attachedRigidbody);
    }

    void OnCollisionEnter(Collision c)
    {
        TryHit(c.rigidbody);
    }

    void TryHit(Rigidbody rb)
    {
        if (!rb) return;
        if (!rb.CompareTag("Player")) return;
        if (GameManager.I == null) return;

        GameManager.I.OnHazardHit(scorePenalty);
    }
}
