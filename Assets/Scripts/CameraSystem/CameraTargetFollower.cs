using UnityEngine;

public class CameraTargetFollower : MonoBehaviour
{
    [Tooltip("The skateboard/controller transform the camera target should follow.")]
    [SerializeField] private Transform target;

    [Header("Offsets")]
    [Tooltip("World-space offset used on flat ground / small tilts (original smooth camera behavior).")]
    [SerializeField] private Vector3 flatOffset = new Vector3(0f, 1.25f, 0f);

    [Tooltip("Local-space offset used at large tilts. It rotates with the board.")]
    [SerializeField] private Vector3 loopOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Tilt Blend")]
    [Tooltip("Angle (degrees) at which the camera switches from flat to loop behavior.")]
    [SerializeField] private float tiltThreshold = 30f;

    [Tooltip("Range around the threshold over which the two modes are blended. 0 = hard switch.")]
    [SerializeField] private float tiltBlendRange = 10f;

    [Tooltip("How quickly the blend between flat and loop modes changes.")]
    [SerializeField] private float blendSpeed = 5f;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera target reaches the computed position. Lower = smoother, higher = tighter.")]
    [SerializeField] private float positionSmoothSpeed = 25f;

    private Vector3 _currentVelocity;
    private float _currentBlend;

    private void Awake()
    {
        if (target == null) return;

        transform.position = target.position + flatOffset;
        transform.rotation = Quaternion.identity;
        _currentBlend = 0f;
    }

    private void Update()
    {
        if (target == null) return;

        UpdateBlend();

        Vector3 flatPosition = target.position + flatOffset;
        Vector3 loopPosition = target.TransformPoint(loopOffset);
        Vector3 desiredPosition = Vector3.Lerp(flatPosition, loopPosition, _currentBlend);

        float smoothTime = positionSmoothSpeed > 0f ? 1f / positionSmoothSpeed : 0f;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, smoothTime);

        Quaternion desiredRotation = Quaternion.Slerp(Quaternion.identity, target.rotation, _currentBlend);
        transform.rotation = desiredRotation;
    }

    private void UpdateBlend()
    {
        float angle = Vector3.Angle(target.up, Vector3.up);
        float halfRange = Mathf.Max(0f, tiltBlendRange * 0.5f);
        float targetBlend = Mathf.InverseLerp(tiltThreshold - halfRange, tiltThreshold + halfRange, angle);
        targetBlend = Mathf.Clamp01(targetBlend);

        _currentBlend = Mathf.MoveTowards(_currentBlend, targetBlend, blendSpeed * Time.deltaTime);
    }
}
