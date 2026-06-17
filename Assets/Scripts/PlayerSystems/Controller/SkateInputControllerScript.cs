using UnityEngine;

public class SkateInputControllerScript : MonoBehaviour
{
    [SerializeField] private SkateboardMovementInteractorScript skateMovementInteractor;

    private void Update()
    {
        if (skateMovementInteractor == null) return;

        if (Input.GetKeyDown(ControlsCollection.forward))
            new PushForwardCommand(skateMovementInteractor).Execute();

        int turn = (Input.GetKey(ControlsCollection.right) ? 1 : 0) - (Input.GetKey(ControlsCollection.left) ? 1 : 0);
        if (turn != 0)
            new TurnCommand(skateMovementInteractor, turn).Execute();

        if (Input.GetKeyDown(ControlsCollection.jump))
            new JumpCommand(skateMovementInteractor).Execute();

        skateMovementInteractor.SetForwardHeld(Input.GetKey(ControlsCollection.forward));
        skateMovementInteractor.SetReverseHeld(Input.GetKey(ControlsCollection.backward));
        skateMovementInteractor.SetJumpHeld(Input.GetKey(ControlsCollection.jump));
    }
}
