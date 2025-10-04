using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Plate References")]
    public Transform plate;                // rotating parent (e.g., Plate root)
    public Collider plateCollider;         // BoxCollider or MeshCollider on the floor cube

    [Header("Player Avoidance")]
    public Transform playerBall;           // drag your PlayerBall here
    public float avoidPlayerPadding = 0.35f; // extra spacing vs player ball

    [Header("Prefabs")]
    public GameObject[] pickupPrefabs;     // trigger colliders, no rigidbody
    public GameObject[] hazardPrefabs;     // solid colliders, no rigidbody
    public GameObject[] holePrefabs;       // Quad "fake holes" with trigger + HoleTrigger

    [Header("Counts")]
    public int pickupCount = 20;
    public int hazardCount = 5;
    public int holeCount = 2;

    [Header("Placement")]
    public bool parentToPlate = true;          // parent spawns to plate
    public bool multiplyPlateRotation = true;  // plate.rotation * prefab.rotation (for pickups/hazards)
    public float wallInset = 0.6f;             // keep away from edges
    public float yOffset = 0.01f;              // small lift above surface
    public float minSpacing = 0.3f;            // extra buffer between items
    public int maxAttemptsPerItem = 40;

    [Header("Hole Placement")]
    public bool holeForceUp = true;            // lie flat on plate (Quad requires 90° X)
    public bool holeRandomYaw = true;          // random yaw for holes
    public float holeExtraSpacing = 0.15f;     // extra buffer only for holes

    [Header("Layers")]
    public LayerMask groundMask = ~0;          // raycast mask (prefer only Ground layer)

    private readonly List<Transform> spawned = new List<Transform>();

    void Start()
    {
        if (!Validate()) return;

        int placedPickups = SpawnBatch(pickupPrefabs, pickupCount, Category.Pickup);
        int placedHazards = SpawnBatch(hazardPrefabs, Mathf.Max(0, hazardCount), Category.Hazard);
        int placedHoles = SpawnBatch(holePrefabs, Mathf.Max(0, holeCount), Category.Hole);

        if (GameManager.I != null)
            GameManager.I.RegisterTotalPickups(placedPickups);
    }

    bool Validate()
    {
        if (!plate || !plateCollider)
        {
            Debug.LogWarning("Spawner: plate or plateCollider missing.");
            return false;
        }
        return true;
    }

    enum Category { Pickup, Hazard, Hole }

    int SpawnBatch(GameObject[] prefabs, int count, Category category)
    {
        if (prefabs == null || prefabs.Length == 0 || count <= 0) return 0;

        Bounds b = plateCollider.bounds;
        float minX = b.min.x + wallInset;
        float maxX = b.max.x - wallInset;
        float minZ = b.min.z + wallInset;
        float maxZ = b.max.z - wallInset;

        int placed = 0;

        for (int i = 0; i < count; i++)
        {
            bool done = false;

            for (int attempt = 0; attempt < maxAttemptsPerItem; attempt++)
            {
                GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                if (!prefab) break;

                // candidate world XZ above plate
                Vector3 posTop = new Vector3(
                    Random.Range(minX, maxX),
                    b.max.y + 2f,
                    Random.Range(minZ, maxZ)
                );

                // raycast to plate surface
                if (!Physics.Raycast(posTop, Vector3.down, out RaycastHit hit, 5f, groundMask, QueryTriggerInteraction.Ignore))
                    continue;

                Vector3 surface = hit.point;

                // radius from prefab collider
                float candidateRadius = GetApproxRadius(prefab);
                float spacing = minSpacing + (category == Category.Hole ? holeExtraSpacing : 0f);

                // overlap check vs spawned AND vs playerBall
                if (IsOverlapping(surface, candidateRadius, spacing))
                    continue;

                Quaternion rot = GetSpawnRotationFor(prefab, category);
                Transform parent = parentToPlate ? plate : null;

                GameObject go = Instantiate(prefab, surface, rot, parent);
                LiftByColliderHeight(go, surface, yOffset);

                spawned.Add(go.transform);
                placed++;
                done = true;
                break;
            }

            // fallback (rare)
            if (!done)
            {
                GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                Vector3 surface = new Vector3(
                    Random.Range(minX, maxX),
                    b.center.y,
                    Random.Range(minZ, maxZ)
                );

                Quaternion rot = GetSpawnRotationFor(prefab, category);
                Transform parent = parentToPlate ? plate : null;

                GameObject go = Instantiate(prefab, surface, rot, parent);
                LiftByColliderHeight(go, surface, yOffset);

                spawned.Add(go.transform);
                placed++;
            }
        }

        return placed;
    }

    // rotation logic
    Quaternion GetSpawnRotationFor(GameObject prefab, Category category)
    {
        if (category == Category.Hole)
        {
            Quaternion baseRot = holeForceUp ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
            if (holeRandomYaw)
                baseRot = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * baseRot;
            Quaternion plateRot = multiplyPlateRotation ? plate.rotation : Quaternion.identity;
            return plateRot * baseRot * prefab.transform.rotation;
        }
        else
        {
            Quaternion plateRot = multiplyPlateRotation ? plate.rotation : Quaternion.identity;
            return plateRot * prefab.transform.rotation;
        }
    }

    // lift instance so collider sits on the plate + offset
    void LiftByColliderHeight(GameObject go, Vector3 surface, float extraOffset)
    {
        Collider c = go.GetComponent<Collider>();
        if (!c)
        {
            go.transform.position = surface + Vector3.up * extraOffset;
            return;
        }

        float upExtent = c.bounds.extents.y; // world half-height
        go.transform.position = surface + Vector3.up * (upExtent + extraOffset);
    }

    // approximate radius from a prefab collider
    float GetApproxRadius(GameObject prefab)
    {
        Collider col = prefab.GetComponent<Collider>();
        if (!col) return 0.5f;

        if (col is SphereCollider sphere)
            return sphere.radius * prefab.transform.localScale.x;

        if (col is BoxCollider box)
        {
            Vector3 scaled = Vector3.Scale(box.size, prefab.transform.localScale);
            return scaled.magnitude * 0.5f; // half diagonal
        }

        return col.bounds.extents.magnitude;
    }

    // approximate radius from an instance transform (for playerBall)
    float GetApproxRadiusFromTransform(Transform t)
    {
        if (!t) return 0.5f;
        Collider col = t.GetComponent<Collider>();
        if (!col) return 0.5f;

        if (col is SphereCollider sphere)
            return sphere.radius * t.localScale.x;

        if (col is BoxCollider box)
        {
            Vector3 scaled = Vector3.Scale(box.size, t.localScale);
            return scaled.magnitude * 0.5f;
        }

        return col.bounds.extents.magnitude;
    }

    // true overlap using radii; ALSO checks distance to playerBall (XZ distance)
    bool IsOverlapping(Vector3 candidateSurfacePos, float candidateRadius, float spacing)
    {
        // 1) vs already spawned items
        foreach (Transform t in spawned)
        {
            if (!t) continue;
            Collider c = t.GetComponent<Collider>();
            if (!c) continue;

            float otherRadius = 0.5f;
            if (c is SphereCollider sc)
                otherRadius = sc.radius * t.localScale.x;
            else if (c is BoxCollider bc)
            {
                Vector3 scaled = Vector3.Scale(bc.size, t.localScale);
                otherRadius = scaled.magnitude * 0.5f;
            }
            else
                otherRadius = c.bounds.extents.magnitude;

            // use XZ planar distance to avoid Y affecting proximity
            Vector2 a = new Vector2(candidateSurfacePos.x, candidateSurfacePos.z);
            Vector2 b = new Vector2(t.position.x, t.position.z);
            float distXZ = Vector2.Distance(a, b);

            if (distXZ < candidateRadius + otherRadius + spacing)
                return true;
        }

        // 2) vs player ball
        if (playerBall)
        {
            float playerRadius = GetApproxRadiusFromTransform(playerBall);
            Vector2 a = new Vector2(candidateSurfacePos.x, candidateSurfacePos.z);
            Vector2 b = new Vector2(playerBall.position.x, playerBall.position.z);
            float distXZ = Vector2.Distance(a, b);

            if (distXZ < candidateRadius + playerRadius + avoidPlayerPadding)
                return true;
        }

        return false;
    }

    public void ClearAndRespawn()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i]) Destroy(spawned[i].gameObject);
        spawned.Clear();

        if (!Validate()) return;

        int placedPickups = SpawnBatch(pickupPrefabs, pickupCount, Category.Pickup);
        int placedHazards = SpawnBatch(hazardPrefabs, Mathf.Max(0, hazardCount), Category.Hazard);
        int placedHoles = SpawnBatch(holePrefabs, Mathf.Max(0, holeCount), Category.Hole);

        if (GameManager.I != null)
            GameManager.I.RegisterTotalPickups(placedPickups);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!plateCollider) return;
        Bounds b = plateCollider.bounds;

        Vector3 size = new Vector3(
            Mathf.Max(0.01f, b.size.x - 2f * wallInset),
            0.02f,
            Mathf.Max(0.01f, b.size.z - 2f * wallInset)
        );
        Vector3 center = new Vector3(b.center.x, b.max.y + 0.02f, b.center.z);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
