using UnityEngine;

public class DragModule : MonoBehaviour
{
    private SkateboardMovementInteractorScript _controller;
    private GroundingEvaluator _grounding;

    public void Initialize(
        SkateboardMovementInteractorScript controller,
        GroundingEvaluator grounding)
    {
        _controller = controller;
        _grounding = grounding;
    }
    public void Tick(float deltaTime)
    {
    }
}
