using UnityEngine;

public class FrictionModule : MonoBehaviour
{
    [Header("Grounded Deceleration")]
    [SerializeField] private float groundedDeceleration = 2f;
    [SerializeField] private float decelerationMinSpeed = 0.1f;

    [Header("Side Friction")]
    [SerializeField] private float sideFriction = 5f;


    private SkateboardMovementInteractorScript _controller;
    private GroundingEvaluator _grounding;
    private Rigidbody _rigidbody;

    public void Initialize(
        SkateboardMovementInteractorScript controller,
        GroundingEvaluator grounding,
        Rigidbody rigidbody)
    {
        _controller = controller;
        _grounding = grounding;
        _rigidbody = rigidbody;
    }

    public void Tick(float deltaTime)
    {
        if (_grounding == null || !_grounding.IsGrounded) return;

        Vector3 velocity = _rigidbody.velocity;
        Vector3 planarVelocity = Vector3.ProjectOnPlane(velocity, _grounding.GroundNormal);

        ApplySideFriction(ref velocity, planarVelocity, deltaTime);
        ApplyGroundedDeceleration(ref velocity, planarVelocity, deltaTime);

        _rigidbody.velocity = velocity;
    }

    private void ApplySideFriction(ref Vector3 velocity, Vector3 planarVelocity, float deltaTime)
    {
        Vector3 sideVelocity = Vector3.Project(planarVelocity, _controller.transform.right);
        velocity -= deltaTime * sideFriction * sideVelocity;
    }

    private void ApplyGroundedDeceleration(ref Vector3 velocity, Vector3 planarVelocity, float deltaTime)
    {
        if (groundedDeceleration <= 0f) return;

        float speed = planarVelocity.magnitude;
        if (speed < decelerationMinSpeed) return;

        Vector3 decelerationDirection = -planarVelocity.normalized;
        float decelerationAmount = groundedDeceleration * deltaTime;

        if (decelerationAmount > speed) decelerationAmount = speed;

        velocity += decelerationDirection * decelerationAmount;
    }
}
