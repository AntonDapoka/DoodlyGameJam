
using UnityEngine;

namespace KinematicCharacterController.Examples
{
    public class ExamplePlayer : MonoBehaviour
    {
        [SerializeField] private GameObject Character;
        [SerializeField] private Transform cameraFollowPoint;
        [SerializeField] private ExampleCharacterCamera CharacterCamera;

        private const string MouseXInput = "Mouse X";
        private const string MouseYInput = "Mouse Y";

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            CharacterCamera.SetFollowTransform(cameraFollowPoint);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                Cursor.lockState = CursorLockMode.Locked;
        }

        private void LateUpdate()
        {
            HandleCameraInput();
        }

        private void HandleCameraInput()
        {
            float mouseLookAxisUp = Input.GetAxisRaw(MouseYInput);
            float mouseLookAxisRight = Input.GetAxisRaw(MouseXInput);
            Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            if (Cursor.lockState != CursorLockMode.Locked) lookInputVector = Vector3.zero;

            // Apply inputs to the camera (zoom is ignored; distance is fixed at 0)
            CharacterCamera.UpdateWithInput(Time.deltaTime, 0f, lookInputVector);
        }
    }
}
