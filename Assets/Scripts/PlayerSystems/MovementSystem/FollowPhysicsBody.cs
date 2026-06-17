using UnityEngine;

public class FollowPhysicsBody : MonoBehaviour
{
    [SerializeField] private Rigidbody physicsBody;

    private Vector3 lastPosition;

    private void Awake()
    {
        lastPosition = physicsBody.position;
        transform.position = lastPosition;
    }

    private void LateUpdate()
    {
        Vector3 position = physicsBody.position;

        if (position == lastPosition)
            return;

        transform.position = position;
        lastPosition = position;
    }
}