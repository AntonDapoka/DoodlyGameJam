using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkateInputControllerScript : MonoBehaviour
{
    [SerializeField] private SkateboardMovementInteractorScript skateMovementInteractor;

    private InputState currentInput;
    //private bool jumpBuffered;

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        //if (Input.GetKeyDown(ControlsCollection.jump))
            //jumpBuffered = true;

        currentInput.jumpPressed = Input.GetKey(ControlsCollection.jump);
        currentInput.forward = Input.GetKeyDown(ControlsCollection.forward);
        currentInput.backward = Input.GetKey(ControlsCollection.backward);
        currentInput.left = Input.GetKey(ControlsCollection.left);
        currentInput.right = Input.GetKey(ControlsCollection.right);

        skateMovementInteractor.SetInput(currentInput);
    }

    private void FixedUpdate()
    {
        //currentInput.jumpPressed = jumpBuffered;
        //jumpBuffered = false;

        skateMovementInteractor.SetInput(currentInput);
    }
}

