using UnityEngine;

public class AirControlModule : MonoBehaviour
{
    private SkateboardMovementInteractorScript _controller;
    private GroundingEvaluator _grounding;

    public float TurnInput { private get; set; }
    public bool ReverseInput { private get; set; }

    private float _airTime;

    public void Initialize(
        SkateboardMovementInteractorScript controller,
        GroundingEvaluator grounding)
    {
        _controller = controller;
        _grounding = grounding;
    }

    public void Tick(float deltaTime)
    {
        if (_grounding.IsGrounded)
        {
            _airTime = 0f;
            return;
        }

        _airTime += deltaTime;
    }
}
