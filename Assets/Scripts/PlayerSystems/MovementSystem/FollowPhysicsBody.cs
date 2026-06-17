using UnityEngine;

public class FollowPhysicsBody : MonoBehaviour
{
    [SerializeField] private Rigidbody physicsBody;
    [SerializeField] private float smooth = 15f;

    private void LateUpdate()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            physicsBody.position,
            Time.deltaTime * smooth
        );
    }
}