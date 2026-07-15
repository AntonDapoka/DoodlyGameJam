using UnityEngine;

public readonly struct TrickContext
{
    public readonly Rigidbody Rigidbody;
    public readonly Transform ControllerTransform;
    public readonly Camera Camera;
    public readonly Vector3 BoardForward;
    public readonly Vector3 CurrentBoardForward;
    public readonly Vector3 BoardForwardAtAirStart;
    public readonly Vector3 CameraForwardAtAirStart;
    public readonly Vector3 CurrentVelocityDirection;
    public readonly float CurrentSpeed;
    public readonly float CumulativeYaw;
    public readonly bool IsGrounded;
    public readonly bool IsAirborne;
    public readonly bool IsJumping;
    public readonly bool JumpRequestedThisFrame;

    public TrickContext(
        Rigidbody rigidbody,
        Transform controllerTransform,
        Camera camera,
        Vector3 boardForward,
        Vector3 currentBoardForward,
        Vector3 boardForwardAtAirStart,
        Vector3 cameraForwardAtAirStart,
        Vector3 currentVelocityDirection,
        float currentSpeed,
        float cumulativeYaw,
        bool isGrounded,
        bool isAirborne,
        bool isJumping,
        bool jumpRequestedThisFrame)
    {
        Rigidbody = rigidbody;
        ControllerTransform = controllerTransform;
        Camera = camera;
        BoardForward = boardForward;
        CurrentBoardForward = currentBoardForward;
        BoardForwardAtAirStart = boardForwardAtAirStart;
        CameraForwardAtAirStart = cameraForwardAtAirStart;
        CurrentVelocityDirection = currentVelocityDirection;
        CurrentSpeed = currentSpeed;
        CumulativeYaw = cumulativeYaw;
        IsGrounded = isGrounded;
        IsAirborne = isAirborne;
        IsJumping = isJumping;
        JumpRequestedThisFrame = jumpRequestedThisFrame;
    }
}
