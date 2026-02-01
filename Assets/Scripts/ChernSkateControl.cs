using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(CharacterController))]

public class ChernSkateControl : MonoBehaviour
{
    [SerializeField] GameObject Player;
    private CharacterController controller;
    private float currentSpeed = 0;
    private float turnAngle = 0;
    private float maxSpeed = 10f;
    private float groundDrag = 0.994f;
    private float airDrag = 0.98f;
    private const float gravity = 9.81f;
    private float accelerationDelay = 0.8f;
    private float afterJumpDelay = 0.4f;
    private float savedSlopeSpeed = 0f;

    private List<KeyCode> keyQueue = new List<KeyCode>();

    private Vector3 velocity;
    private float critSlopeAngle = 42.5f;
    private bool isGrounded = false;
    private bool canPushFwd = true;
    private bool isGrinding = false;
    private bool canTurn = true;
    private bool isRolling = false;
    private bool isTricking = false;

    public Transform groundCheck;
    [SerializeField] private float groundDistance = 0.3f;
    public LayerMask groundMask;

    private bool a;

    private Vector3 zeroVec = Vector3.zero;

    void Start()
    {
        controller = Player.GetComponent<CharacterController>();
        velocity = Vector3.zero;
        currentSpeed = 0f;
        savedSlopeSpeed = 0f;
    }

    void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
        CheckState();
        HandleMovement();
        ApplyGravity();
    }

    IEnumerator FwdPushDelay(float waitTime)
    {
        canPushFwd = false;
        yield return new WaitForSeconds(waitTime);
        canPushFwd = true;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(ControlsCollection.forward)) { keyQueue.Add(ControlsCollection.forward); }
        if (Input.GetKeyDown(ControlsCollection.backward)) { keyQueue.Add(ControlsCollection.backward); }
        if (Input.GetKey(ControlsCollection.left)) { keyQueue.Add(ControlsCollection.left); }
        if (Input.GetKey(ControlsCollection.right)) { keyQueue.Add(ControlsCollection.right); }
        if (Input.GetKeyDown(ControlsCollection.jump)) { keyQueue.Add(ControlsCollection.jump); }
        if (Input.GetKeyDown(ControlsCollection.shift)) { keyQueue.Add(ControlsCollection.shift); }
        if (Input.GetKeyDown(ControlsCollection.trick1)) { keyQueue.Add(ControlsCollection.trick1); } // Fixed: was trick2
        if (Input.GetKeyDown(ControlsCollection.trick2)) { keyQueue.Add(ControlsCollection.trick2); } // Added missing check
    }

    private void HandleMovement()
    {
        foreach (KeyCode key in keyQueue)
        {
            switch (key) // Simplified switch statement
            {
                case var k when k == ControlsCollection.forward && canPushFwd && !isRolling && !isGrinding:
                    StartCoroutine(FwdPushDelay(accelerationDelay));
                    Push(true);
                    break;
                case var k when k == ControlsCollection.backward && !isGrinding:
                    Push(false);
                    break;
                case var k when k == ControlsCollection.left:
                    if (canTurn)
                    {
                        canTurn = false;
                        Turn(false, isGrounded, isGrinding);
                    }
                    break;
                case var k when k == ControlsCollection.right:
                    if (canTurn)
                    {
                        canTurn = false;
                        Turn(true, isGrounded, isGrinding);
                    }
                    break;
                case var k when k == ControlsCollection.jump:
                    Jump();
                    break;
                case var k when k == ControlsCollection.trick1:
                    Trick(false, isGrounded, isGrinding);
                    break;
                case var k when k == ControlsCollection.trick2:
                    Trick(true, isGrounded, isGrinding);
                    break;
            }
        }

        keyQueue.Clear();

        // Apply movement based on current speed
        if (!isGrinding)
        {
            Vector3 moveDirection = transform.forward * currentSpeed;
            controller.Move(moveDirection * Time.fixedDeltaTime);
        }

        float drag = isGrounded ? groundDrag : airDrag;
        currentSpeed *= drag;
    }

    private void ApplyGravity()
    {
        if (!isGrounded && !isGrinding)
        {
            velocity.y -= gravity * Time.fixedDeltaTime;
        }
        else
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        controller.Move(velocity * Time.fixedDeltaTime);
    }

    private void CheckState()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);

        if (isGrounded)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundDistance + 0.1f, groundMask))
            {
                float angle = Vector3.Angle(Vector3.up, hit.normal);

                if (angle < critSlopeAngle)
                {
                    // Handle slope physics
                    Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                    float slopeEffect = Mathf.Clamp01((angle / critSlopeAngle) * 0.5f);
                    currentSpeed += slopeEffect * gravity * 0.01f;
                    currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
                }

                if (hit.collider.CompareTag("Grindable"))
                {
                    HandleGrind();
                }
                else
                {
                    isGrinding = false;
                }
            }

            if (!isGrinding)
            {
                velocity.y = 0f;
            }
        }
        else
        {
            isRolling = false;
            isGrinding = false;
        }

        canTurn = !(isTricking || isGrinding);
        canPushFwd = !isRolling;
    }

    private void HandleGrind()
    {
        isGrinding = true;
        isGrounded = true; // Treat as grounded while grinding
        // Move along the grind rail
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10f, groundMask))
        {
            // Follow the rail position
            Vector3 targetPos = new Vector3(transform.position.x, hit.point.y + controller.height / 2, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
        }
    }

    private void Push(bool direction)
    {
        float pushForce = direction ? 1f : -0.7f; // Forward push stronger than backward
        currentSpeed += pushForce;
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed / 2, maxSpeed);
    }

    private void Turn(bool direction, bool grounded, bool grinding)
    {
        float turnAmount = grounded ? 45f : 30f; // Less turning in air
        float turnDirection = direction ? 1f : -1f;

        transform.Rotate(Vector3.up, turnDirection * turnAmount * Time.deltaTime);

        // Add slight sideways movement when turning on ground
        if (grounded && !grinding && currentSpeed > 2f)
        {
            Vector3 sidewaysMove = transform.right * turnDirection * 0.1f * currentSpeed * Time.deltaTime;
            controller.Move(sidewaysMove);
        }

        // Reset turn ability after delay
        Invoke(nameof(EnableTurn), 0.1f);
    }

    private void EnableTurn()
    {
        canTurn = true;
    }

    private void Trick(bool type, bool grounded, bool grinding)
    {
        if (!grounded && !grinding) // Only do tricks in air
        {
            isTricking = true;

            // Perform different tricks based on type
            if (type)
            {
                // Kickflip or similar
                transform.Rotate(Vector3.forward, 360f, Space.Self);
            }
            else
            {
                // Ollie or similar
                if (currentSpeed > 2f)
                {
                    velocity.y = 5f; // Add upward velocity
                }
            }

            // End trick after delay
            Invoke(nameof(EndTrick), 0.8f);
        }
    }

    private void EndTrick()
    {
        isTricking = false;
    }

    private void Jump()
    {
        if (isGrounded && !isGrinding)
        {
            velocity.y = 8f; // Jump force
            isGrounded = false;
        }
    }
}
