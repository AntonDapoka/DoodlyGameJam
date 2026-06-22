using UnityEngine;

public class FollowPhysicsBody : MonoBehaviour
{
    [SerializeField] private Rigidbody physicsBody;
    [SerializeField] private float smooth = 15f;

    private void LateUpdate()
{
    transform.position = physicsBody.transform.position;
}
}