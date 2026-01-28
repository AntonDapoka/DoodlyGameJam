using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChernSkateControl : MonoBehaviour
{
    [Header("Movement Settings")]
    public float accelerationForce = 10f;
    public float maxSpeed = 20f;
    public float groundDrag = 0.98f;
    public float airDrag = 0.95f;
    public float decelerationRate = 0.95f;
    public float accelerationDelay = 0.1f; 
    public float jumpForce = 8f;
    public float gravity = -9.81f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Surface Detection")]
    public float slopeAngleThreshold = 45f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isSliding;
    private bool isAccelerating;
    private float accelerationTimer;
    private float currentSpeed;
    private float savedSlopeSpeed;
    private bool wasGroundedLastFrame;

    public bool IsGrounded => isGrounded;
    public bool IsSliding => isSliding;
    public bool IsAccelerating => isAccelerating;
    public float CurrentSpeed => Mathf.Abs(currentSpeed);

    void Start()
    {
        controller = GetComponent<CharacterController>();
        velocity = Vector3.zero;
        accelerationTimer = 0f;
        currentSpeed = 0f;
        savedSlopeSpeed = 0f;
    }

    void Update()
    {
        HandleGrounding();
        HandleInertiaAndSlopes();
        HandleInput();
        ApplyMovement();

        wasGroundedLastFrame = isGrounded;
    }

    void HandleGrounding()
    {
        bool newIsGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);

        if (!wasGroundedLastFrame && newIsGrounded)
        {
            if (savedSlopeSpeed != 0)
            {
                currentSpeed = savedSlopeSpeed;
                savedSlopeSpeed = 0;
            }
        }

        isGrounded = newIsGrounded;

        if (isGrounded)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundDistance + 0.1f, groundMask))
            {
                float angle = Vector3.Angle(Vector3.up, hit.normal);
                isSliding = angle > slopeAngleThreshold;
            }
        }
        else
        {
            isSliding = false;
        }
    }

    void HandleInertiaAndSlopes()
    {
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else
        {
            // On ground, align with surface normal
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundDistance + 0.1f, groundMask))
            {
                float slopeFactor = Mathf.Sin(Mathf.Deg2Rad * Vector3.Angle(Vector3.up, hit.normal));
                Vector3 slopeDirection = Vector3.Cross(hit.normal, Vector3.right).normalized;

                float slopeDot = Vector3.Dot(slopeDirection, transform.forward);
                if (Vector3.Angle(Vector3.up, hit.normal) > 5f) // Not flat ground
                {
                    float downSlopeAcceleration = slopeFactor * 15f; 
                    currentSpeed += downSlopeAcceleration * Time.deltaTime;

                    if (currentSpeed > maxSpeed * 0.7f)
                    {
                        currentSpeed = maxSpeed * 0.7f;
                    }
                }
            }

            velocity.y = 0f;
        }

        float drag = isGrounded ? groundDrag : airDrag;
        currentSpeed *= drag;

        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);
    }

    void HandleInput()
    {
        if (isGrounded)
        {
            float verticalInput = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(verticalInput) > 0.1f)
            {
                accelerationTimer += Time.deltaTime;

                if (accelerationTimer >= accelerationDelay)
                {
                    isAccelerating = true;

                    float targetSpeed = verticalInput * accelerationForce * Time.deltaTime;

                    if ((currentSpeed >= 0 && targetSpeed >= 0) || (currentSpeed <= 0 && targetSpeed <= 0))
                    {
                        currentSpeed += targetSpeed;
                    }
                    else
                    {
                        currentSpeed += targetSpeed * 0.5f;
                    }

                    currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);
                }
            }
            else
            {
                accelerationTimer = 0f;
                isAccelerating = false;

                currentSpeed *= decelerationRate;
            }

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = jumpForce;
                savedSlopeSpeed = currentSpeed;
            }

            // Turning
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                transform.Rotate(Vector3.up, horizontalInput * 100f * Time.deltaTime);
            }
        }
    }

    void ApplyMovement()
    {
        Vector3 forwardMovement = transform.forward * currentSpeed * Time.deltaTime;

        Vector3 finalMovement = forwardMovement;
        finalMovement.y = velocity.y * Time.deltaTime;

        controller.Move(finalMovement);

        ApplyBoardTilt();
    }

    void ApplyBoardTilt()
    {
        float tiltAmount = currentSpeed / maxSpeed * 10f;
        if (tiltAmount > 0)
        {
            transform.rotation = Quaternion.Euler(
                transform.eulerAngles.x,
                transform.eulerAngles.y,
                -tiltAmount
            );
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawSphere(groundCheck.position, groundDistance);
    }

    public void SetSpeed(float newSpeed)
    {
        currentSpeed = Mathf.Clamp(newSpeed, -maxSpeed, maxSpeed);
    }

    public void AddSpeed(float additionalSpeed)
    {
        currentSpeed = Mathf.Clamp(currentSpeed + additionalSpeed, -maxSpeed, maxSpeed);
    }
}