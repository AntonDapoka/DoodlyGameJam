using UnityEngine;

public class FollowPhysicsBody : MonoBehaviour
{
    [SerializeField] private Rigidbody physicsBody;
    private Vector3 velocity;
    [SerializeField] private float x = 0.05f;

    private void LateUpdate()
    {
        transform.position = Vector3.SmoothDamp(transform.position, 
        physicsBody.transform.position, ref velocity, x);
    }
}