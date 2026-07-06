using UnityEngine;

public class CameraTargetFollower : MonoBehaviour
{
    [Tooltip("The transform whose position the camera target should follow (e.g. the Player).")]
    [SerializeField] private Transform target;

    [Tooltip("World-space offset applied to the target position (e.g. camera height).")]
    [SerializeField] private Vector3 positionOffset;

    [Tooltip("How quickly the target catches up to the player's position. Higher = tighter.")]
    [SerializeField] private float positionSmoothSpeed = 15f;

    [Tooltip("If true, the camera target also copies the yaw of YawReference. Usually keep false for a stable horizon.")]
    [SerializeField] private bool inheritYaw;

    [Tooltip("Reference used for yaw when InheritYaw is true. If null, the target stays upright.")]
    [SerializeField] private Transform yawReference;

    private Vector3 _currentVelocity;

    private void Awake()
    {
        if (target != null)
        {
            transform.position = target.position;
        }

        transform.rotation = Quaternion.identity;
    }

    private void Update()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + positionOffset;
        float smoothTime = positionSmoothSpeed > 0f ? 1f / positionSmoothSpeed : 0f;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, smoothTime);

        if (inheritYaw && yawReference != null)
        {
            Vector3 forward = yawReference.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
            else
            {
                transform.rotation = Quaternion.identity;
            }
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }
    }
}
