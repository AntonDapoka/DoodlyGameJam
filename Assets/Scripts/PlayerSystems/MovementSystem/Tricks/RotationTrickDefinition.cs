using UnityEngine;

public enum RotationTrickMoment
{
    InAir,
    OnLanding,
    Both
}

[CreateAssetMenu(fileName = "NewRotationTrick", menuName = "Skateboard/Tricks/Rotation Trick")]
public class RotationTrickDefinition : TrickDefinition
{
    [SerializeField] private float targetRotation = 180f;
    [SerializeField] private float rotationTolerance = 15f;
    [SerializeField] private bool requireFacingMovement = true;
    [SerializeField] private RotationTrickMoment triggerMoment = RotationTrickMoment.InAir;

    public override bool CanExecute(in TrickContext context, ExecutionPhase phase, TrickModule module)
    {
        switch (triggerMoment)
        {
            case RotationTrickMoment.InAir:
                if (phase != ExecutionPhase.InAir) return false;
                break;
            case RotationTrickMoment.OnLanding:
                if (phase != ExecutionPhase.Landing) return false;
                break;
            case RotationTrickMoment.Both:
                if (phase != ExecutionPhase.InAir && phase != ExecutionPhase.Landing) return false;
                break;
        }

        if (phase == ExecutionPhase.InAir && !context.IsAirborne) return false;

        float yaw = Mathf.Abs(module.CumulativeYaw);
        float diff = Mathf.Abs(yaw - targetRotation);
        if (diff > rotationTolerance) return false;

        float dot = Vector3.Dot(context.BoardForwardAtAirStart, context.CameraForwardAtAirStart);
        const float threshold = 0.1f;

        return requireFacingMovement ? dot >= threshold : dot <= -threshold;
    }
}
