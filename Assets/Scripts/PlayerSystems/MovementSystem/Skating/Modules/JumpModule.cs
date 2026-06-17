using UnityEngine;

public class JumpModule : MonoBehaviour
{
    [SerializeField] private float jumpForce;
    private GroundingEvaluator _grounding;

    private bool _requested;

    public void Initialize(
        GroundingEvaluator grounding)
    {
        _grounding = grounding;
    }

    public void RequestJump()
    {
        _requested = true;
    }

    public void Tick(float deltaTime)
    {
        if (!_requested)
            return;

        _requested = false;

        if (!_grounding.IsGrounded)
            return;

        //_velocity.ApplyImpulse(Vector3.up * jumpForce);
    }
}
