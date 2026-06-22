using UnityEngine;

public class FollowPhysicsBody : MonoBehaviour
{
    [SerializeField] private Rigidbody physicsBody;
    [SerializeField] private float smooth = 15f;
    Vector3 velocity;

    private void LateUpdate()
    {
        transform.position = Vector3.SmoothDamp(
            transform.position,
            physicsBody.transform.position,
            ref velocity,
            0.05f);
    }
}