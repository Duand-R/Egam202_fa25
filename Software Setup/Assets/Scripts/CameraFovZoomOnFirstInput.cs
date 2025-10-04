using UnityEngine;
using Unity.Cinemachine; 

public class CameraFovZoomOnFirstInput : MonoBehaviour
{
    [Header("Cinemachine VCam (CM 3.x)")]
    [SerializeField] private CinemachineCamera vcam;

    [Header("FOV Zoom")]
    [SerializeField] private float startFov = 80f;   // far at start
    [SerializeField] private float targetFov = 60f;  // normal view
    [SerializeField] private float zoomDuration = 1.5f;

    private float t = 0f;
    private bool zooming = false;
    private bool started = false;

    private void Start()
    {
        if (vcam != null)
            vcam.Lens.FieldOfView = startFov;  // start far
    }

    private void Update()
    {
        if (!started && HasInput())
        {
            started = true;
            zooming = true;
        }

        if (zooming && vcam != null)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, zoomDuration);
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            vcam.Lens.FieldOfView = Mathf.Lerp(startFov, targetFov, s);

            if (t >= 1f) zooming = false;
        }
    }

    private bool HasInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        return Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f || Input.anyKeyDown;
    }
}
