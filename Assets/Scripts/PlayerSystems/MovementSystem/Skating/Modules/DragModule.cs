using UnityEngine;

public class DragModule : MonoBehaviour
{
    private SkateboardMovementInteractorScript _controller;
    private VelocityHandler _velocity;
    private GroundingEvaluator _grounding;

    public void Initialize(
        SkateboardMovementInteractorScript controller,
        VelocityHandler velocity,
        GroundingEvaluator grounding)
    {
        _controller = controller;
        _velocity = velocity;
        _grounding = grounding;
    }
    public void Tick(float deltaTime)
    {
    }
}
