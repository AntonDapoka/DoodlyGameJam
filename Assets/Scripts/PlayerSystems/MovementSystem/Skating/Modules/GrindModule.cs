using UnityEngine;

public class GrindModule : MonoBehaviour
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
}
