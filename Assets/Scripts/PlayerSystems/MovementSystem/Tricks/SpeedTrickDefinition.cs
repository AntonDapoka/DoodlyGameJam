using UnityEngine;

[CreateAssetMenu(fileName = "NewSpeedTrick", menuName = "Skateboard/Tricks/Speed Trick")]
public class SpeedTrickDefinition : TrickDefinition
{
    [SerializeField] private FacingRequirement facingRequirement;

    public override bool CanExecute(in TrickContext context, ExecutionPhase phase, TrickModule module)
    {
        if (phase != ExecutionPhase.Base) return false;
        if (!context.IsJumping) return false;
        if (!context.JumpRequestedThisFrame) return false;

        float speed = context.CurrentSpeed;
        if (speed < MinSpeed || speed > MaxSpeed) return false;

        float dot = Vector3.Dot(context.BoardForward, context.CurrentVelocityDirection);
        const float threshold = 0.7f;

        switch (facingRequirement)
        {
            case FacingRequirement.AlignedWithMovement:
                return dot >= threshold;
            case FacingRequirement.OppositeToMovement:
                return dot <= -threshold;
            case FacingRequirement.None:
            default:
                return true;
        }
    }
}

public enum FacingRequirement
{
    None,
    AlignedWithMovement,
    OppositeToMovement
}
