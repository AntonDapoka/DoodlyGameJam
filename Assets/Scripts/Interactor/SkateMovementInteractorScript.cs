using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SkateMovementInteractorScript : MonoBehaviour
{
    [Header("References:")]
    //[SerializeField] private GameObject playerObject; //Interactor should not be on playerObject, we need to change it (maybe)
    [SerializeField] private TrickInteractorScript trickInteractor;
    private CharacterController controller;

    [Header("Skate Settings:")]
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float groundDrag = 0.994f;
    [SerializeField] private float airDrag = 0.98f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float skateImpulseForceForward = 1f;
    [SerializeField] private float skateImpulseForceBackward = -0.7f;
    [SerializeField] private float skateImpulseCoolDownTime = 5f;
    [SerializeField] private float turnAmountWhileGrounded = 45f;
    [SerializeField] private float turnAmountWhileInTheAir = 30f;
    [SerializeField] private float critSlopeAngle = 42.5f;
    [SerializeField] private float jumpForce = 10f;

    //private float afterJumpDelay = 0.4f;
    //private float savedSlopeSpeed = 0f;

    private Vector3 velocity;
    private float currentSpeed = 0;
    //private float currentTurnAngle = 0;


    public bool isAbleToPushForward = true;
    public bool isPushingForward = false;
    public bool isGrinding = false;
    private bool isAbleToTurn = true;
    //private bool isTurning = false;
    private bool isRolling = false; //???
    private bool isTricking = false;

    private InputState currentInput;
    private InputState previousInput;

    [Header("Ground Settings:")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundDistance = 0.3f;
    [SerializeField] private float groundSnapForce = -2f;

    public bool isGrounded = false;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        velocity = Vector3.zero;
        currentSpeed = 0f;
    }

    public void SetInput(InputState newInput)
    {
        previousInput = currentInput;
        currentInput = newInput;
    }

    private void FixedUpdate()
    {
        CheckState();
        HandleMovement();
        ApplyGravity();
    }

    private void HandleMovement()
    {
        HandleForwardBackwardMovement();

        HandleTurning();

        HandleActions();

        ApplyMovement();

        ApplyDrag();
    }

    private void HandleForwardBackwardMovement()
    {
        if (currentInput.forward && !currentInput.backward && isAbleToPushForward && !isRolling && !isGrinding)
        {
            if (!isPushingForward)
            {
                StartCoroutine(SkateImpulseCoolingDown(skateImpulseCoolDownTime));
                ApplySkateImpulse(true);
            }
        }
        else if (currentInput.backward && !currentInput.forward && !isGrinding)
        {
            ApplySkateImpulse(false);
        }
    }

    private IEnumerator SkateImpulseCoolingDown(float waitTime) //ex-ForwardPushDelay
    {
        isAbleToPushForward = false;
        yield return new WaitForSeconds(waitTime);
        isAbleToPushForward = true;
        isPushingForward = false;
    }

    private void HandleTurning()
    {
        if (!isAbleToTurn) return;

        int turn = (currentInput.right ? 1 : 0) - (currentInput.left ? 1 : 0);
        if (turn != 0)
            ApplyTurning(turn > 0);
    }

    private void HandleActions()
    {
        if (currentInput.jumpPressed) TryJump();

        if (currentInput.trick1Pressed) TryPerformTrick(TrickType.Kickflip);

        if (currentInput.trick2Pressed) TryPerformTrick(TrickType.Ollie); 
    }

    private void ApplyMovement()
    {
        if (!isGrinding)
        {
            Vector3 moveDirection = transform.forward * currentSpeed;
            controller.Move(moveDirection * Time.fixedDeltaTime);
        }
    }

    private void ApplyDrag()
    {
        float drag = isGrounded ? groundDrag : airDrag;
        currentSpeed = Mathf.Max(0, currentSpeed * drag);
    }

    private void ApplyGravity()
    {
        if (!isGrounded && !isGrinding)
            velocity.y -= gravity * Time.fixedDeltaTime;
        else
            velocity.y = groundSnapForce; // Small downward force to keep grounded

        controller.Move(velocity * Time.fixedDeltaTime);
    }

    private void CheckState()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);

        if (isGrounded)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundDistance + 0.1f, groundMask))
            {
                float angle = Vector3.Angle(Vector3.up, hit.normal);

                if (angle < critSlopeAngle)  // Handle slope physics
                {
                    float slopeEffect = Mathf.Clamp01((angle / critSlopeAngle) * 0.5f);
                    currentSpeed += slopeEffect * gravity * 0.01f;
                    currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
                }

                if (hit.collider.gameObject.TryGetComponent(out GrindableMarker marker)) HandleGrind();
                else isGrinding = false;
            }

            if (!isGrinding) velocity.y = 0f;
        }
        else
        {
            isRolling = false;
            isGrinding = false;
        }

        isAbleToTurn = !(isTricking || isGrinding);
        isAbleToPushForward = !isRolling;
    }

    private void TryJump()
    {
        if (isGrounded && !isGrinding)
        {
            velocity.y = jumpForce;
            isGrounded = false;
        }
    }

    private void HandleGrind()
    {
        isGrinding = true;
        isGrounded = true; // Treat as grounded while grinding -------------- ???????????
       
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10f, groundMask)) // Move along the grind rail
        {
            // Follow the rail position
            Vector3 targetPos = new Vector3(transform.position.x, hit.point.y + controller.height / 2, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
        }
    }

    private void ApplySkateImpulse(bool direction) //ex-Push
    {
        isPushingForward = true;
        currentSpeed += direction ? skateImpulseForceForward : skateImpulseForceBackward;
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed / 2, maxSpeed);
    }

    private void ApplyTurning(bool direction)
    {
        float turnAmount = isGrounded ? turnAmountWhileGrounded : turnAmountWhileInTheAir;
        float turnDirection = direction ? 1f : -1f;

        transform.Rotate(Vector3.up, turnDirection * turnAmount * Time.deltaTime);

        if (isGrounded && !isGrinding && currentSpeed > 2f)  // Add slight sideways movement when turning on ground
        {
            Vector3 sidewaysMove = transform.right * turnDirection * 0.1f * currentSpeed * Time.deltaTime;
            controller.Move(sidewaysMove);
        }

        Invoke(nameof(EnableTurn), 0.1f); // Reset turn ability after delay
    }

    private void EnableTurn()
    {
        isAbleToTurn = true;
    }

    private void TryPerformTrick(TrickType type) //By Anton: Maybe make different Class for Tricks?
    {
        if (!isGrounded && !isGrinding) // Only do tricks in air
        {
            isTricking = true;

            if (type == TrickType.Kickflip)
            {
                transform.Rotate(Vector3.forward, 360f, Space.Self);
            }
            else if (type == TrickType.Ollie && currentSpeed > 2f)
            {
                    velocity.y = 5f;
            }
            Invoke(nameof(EndTrick), 0.8f);
        }
    }

    private void EndTrick()
    {
        isTricking = false;
    }
}
