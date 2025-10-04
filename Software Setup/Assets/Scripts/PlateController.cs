using UnityEngine;

public class PlateController : MonoBehaviour
{
    [Header("Tilt")]
    public float maxTilt = 15f;          // degrees
    public float smoothTime = 0.06f;     // rotation smooth time for SmoothDampAngle

    [Header("Edge Attenuation")]
    public float plateRadius = 5f;                  // plate radius
    [Range(0f, 1f)] public float minEdgeStrength = 0.4f; // strength at edge
    public float attenuationExponent = 2f;          // >1 = faster falloff

    [Header("Input")]
    public float shiftScale = 0.4f;      // strength when holding Shift
    public float scaleBlendTime = 0.18f; // seconds to blend input scale when toggling Shift
    public float deadzone = 0.01f;       // ignore tiny inputs

    [Header("References")]
    public Transform playerBall;

    // internal state
    private Vector3 targetEuler;
    private Vector3 currentEuler;
    private Vector3 eulerVel;            // for SmoothDampAngle
    private float currentInputScale = 1f;
    private float inputScaleVel = 0f;    // for SmoothDamp on scale

    void Start()
    {
        if (!playerBall)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go) playerBall = go.transform;
        }
        currentEuler = transform.rotation.eulerAngles;
        targetEuler = currentEuler;
    }

    void Update()
    {
        // read input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // deadzone
        if (Mathf.Abs(h) < deadzone) h = 0f;
        if (Mathf.Abs(v) < deadzone) v = 0f;

        // target scale: shift precision vs normal
        bool precision = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float targetScale = precision ? Mathf.Clamp01(shiftScale) : 1f;

        // smooth the scale so releasing Shift does not jump
        currentInputScale = Mathf.SmoothDamp(currentInputScale, targetScale, ref inputScaleVel, Mathf.Max(0.0001f, scaleBlendTime));

        // edge attenuation
        float edgeScale = 1f;
        if (playerBall && plateRadius > 0.0001f)
        {
            Vector3 centerXZ = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 ballXZ = new Vector3(playerBall.position.x, 0f, playerBall.position.z);
            float dist = Vector3.Distance(centerXZ, ballXZ);
            float t = Mathf.Clamp01(dist / plateRadius);
            float curve = Mathf.Pow(t, Mathf.Max(0.001f, attenuationExponent));
            edgeScale = Mathf.Lerp(1f, Mathf.Clamp01(minEdgeStrength), curve);
        }

        float finalScale = currentInputScale * edgeScale;
        h *= finalScale;
        v *= finalScale;

        // desired tilt
        targetEuler = new Vector3(v * maxTilt, 0f, -h * maxTilt);
    }

    void FixedUpdate()
    {
        // smooth angles separately to avoid sudden snaps
        currentEuler.x = Mathf.SmoothDampAngle(currentEuler.x, targetEuler.x, ref eulerVel.x, smoothTime);
        currentEuler.z = Mathf.SmoothDampAngle(currentEuler.z, targetEuler.z, ref eulerVel.z, smoothTime);

        transform.rotation = Quaternion.Euler(currentEuler);
    }
}
