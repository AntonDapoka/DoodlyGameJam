using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class SkateboardControls : MonoBehaviour
{

    [SerializeField] GameObject Player;
    private float currentSpeed = 0;
    private float turnAngle = 0;
    private float fwdAcceleration = 0;
    private float maxSpeed = 10f;
    private float drag = 1f;
    private const float gravity = 9.8f;
    private float accelerationDelay = 0.8f;
    private float savedSlopeSpeed = 0f;

    private List<KeyCode> keyQueue = new List<KeyCode>();

    private KeyCode fwd = ControlsCollection.forward;
    private KeyCode turnRight = ControlsCollection.right;
    private KeyCode turnLeft = ControlsCollection.left;
    private KeyCode bck = ControlsCollection.backward;
    private KeyCode jump = ControlsCollection.jump;

    private CharacterController controller;
    private Vector3 velocity;
    private float critSlopeAngle = 60f;
    private bool isGrounded = false;
    private bool canPushFwd = true;
    private bool isGrinding = false;

    public Transform groundCheck;
    [SerializeField] private float groundDistance = 0.3f;
    public LayerMask groundMask;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        velocity = Vector3.zero;
        currentSpeed = 0f;
        savedSlopeSpeed = 0f;
    }

    void Update()
    {
        HandleMovement();
        CheckGround();
    }

    private void FixedUpdate()
    {
        
    }

    IEnumerator FwdPushDelay()
    {
        canPushFwd = false;
        yield return new WaitForSeconds(accelerationDelay);
        canPushFwd = true;
    }

    private void HandleMovement()
    {

    }

    private void CheckGround()
    {

    }
}
