using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalLocalPos;

    private float shakeTime = 0f;
    private float magnitude = 0.2f;
    private float damping = 2.0f;

    void Awake()
    {
        originalLocalPos = transform.localPosition;
    }

    void Update()
    {
        if (shakeTime > 0f)
        {
            transform.localPosition = originalLocalPos + Random.insideUnitSphere * magnitude;
            shakeTime -= Time.deltaTime * damping;
        }
        else
        {
            shakeTime = 0f;
            transform.localPosition = originalLocalPos;
        }
    }

    // Overload with 3 parameters
    public void TriggerShake(float duration, float strength, float dampingSpeed)
    {
        shakeTime = duration;
        magnitude = strength;
        damping = Mathf.Max(0.0001f, dampingSpeed);
    }

    // Optional overload with 2 parameters (convenience)
    public void TriggerShake(float duration, float strength)
    {
        TriggerShake(duration, strength, 2.0f);
    }

    // Optional overload with defaults
    public void TriggerShake()
    {
        TriggerShake(0.2f, 0.2f, 2.0f);
    }
}
