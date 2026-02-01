using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class SkateboardControls : MonoBehaviour
{

    [SerializeField] GameObject Player;
    private Rigidbody playerRB;
    private float currentSpeed = 0;
    private float turnAngle = 0;
    private float fwdAcceleration = 0;
    private float maxSpeed = 10f;
    private float groundDrag = 0.994f;
    private float airDrag = 0.98f;
    private const float gravity = 9.4f;
    private float accelerationDelay = 0.8f;
    private float afterJumpDelay = 0.4f;
    private float savedSlopeSpeed = 0f;

    private List<KeyCode> keyQueue = new List<KeyCode>();

    private CharacterController controller;
    private Vector3 velocity;
    private float critSlopeAngle = 42.5f;
    public bool isGrounded { get; private set; }
    private bool canPushFwd = true;
    public bool isGrinding { get; private set; }
    private bool canTurn = true;
    private bool isRolling = false;
    private bool isTricking = false;

    public Transform groundCheck;
    [SerializeField] private float groundDistance = 0.3f;
    public LayerMask groundMask;
    public StyleSystem Style;

    public int[] trickList = new int[3] { -1, -1, -2 };

    private bool a;

    private Vector3 zeroVec = Vector3.zero;

    void Awake()
    {
        controller = Player.GetComponent<CharacterController>();
        playerRB = Player.GetComponent<Rigidbody>();
        velocity = Vector3.zero;
        currentSpeed = 0f;
        savedSlopeSpeed = 0f;
        isGrounded = false;
    }

    void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
        CheckState();
        HandleMovement();
        // HandleInertiaAndSlopes()
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
        if (Input.GetKeyDown(ControlsCollection.trick1)) { keyQueue.Add(ControlsCollection.trick2); }
    }

    private void HandleMovement()
    {
        foreach(KeyCode key in keyQueue)
        {
            switch (true)
            {
                case true when key == ControlsCollection.forward && canPushFwd && !isRolling && !isGrinding:
                    StartCoroutine(FwdPushDelay(accelerationDelay));
                    Push(true);
                    break;
                case true when key == ControlsCollection.backward&& !isGrinding:
                    Push(false);
                        break;
                case true when key == ControlsCollection.left || key == ControlsCollection.right && canTurn:
                    canTurn = false;
                    Turn(a = key == ControlsCollection.right ? true : false, isGrounded, isGrinding);
                    break;
                case true when key == ControlsCollection.jump:
                    AddTrick(0);
                    break;
                case true when key == ControlsCollection.trick1 || key == ControlsCollection.trick2:
                    Trick(a = key == ControlsCollection.trick2 ? true : false, isGrounded, isGrinding);
                    break;
            }
        }

        keyQueue.Clear();

        float drag = isGrounded ? groundDrag : airDrag;
        currentSpeed *= drag;
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
                if (hit.collider.tag == "Grindable"){ HandleGrind(); }
                else { isGrinding  = false; }

            }
        }
        else
        {
            isRolling = false;
        }

        canTurn = !(isTricking || isGrinding);

        canPushFwd = !isRolling;

    }

    private void HandleGrind()
    {
        isGrinding = true;



    }

    private void Push(bool direction)
    {
        if (direction)
        {

        }
        else
        {

        }
    }

    private void Turn(bool direction, bool grounded, bool grinding)
    {


        canTurn = true;
    }

    private void Trick(bool type, bool grounded, bool grinding)
    {
        if (type)
        {
            if (grinding) AddTrick(6);
            else if (!grounded) AddTrick(4);
            else AddTrick(2);
        }
        else
        {
            if (grinding) AddTrick(5);
            else if (!grounded) AddTrick(3);
            else AddTrick(1);
        }
    }

    private void AddTrick(int code)
    {
        this.trickList[0] = code;
        this.trickList[1] = this.trickList[0];
        this.trickList[2] = this.trickList[1];
        Style.TrickAddScore(ref this.trickList);
    }

    //void HandleInertiaAndSlopes()
    //{
    //    if (!isGrounded)
    //    {
    //        velocity.y += gravity;
    //    }
    //    else
    //    {
    //        // On ground, align with surface normal
    //        RaycastHit hit;
    //        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundDistance + 0.1f, groundMask))
    //        {
    //            float slopeFactor = Mathf.Sin(Mathf.Deg2Rad * Vector3.Angle(Vector3.up, hit.normal));
    //            Vector3 slopeDirection = Vector3.Cross(hit.normal, Vector3.right).normalized;

    //            float slopeDot = Vector3.Dot(slopeDirection, transform.forward);
    //            if (Vector3.Angle(Vector3.up, hit.normal) > 5f) // Not flat ground
    //            {
    //                float downSlopeAcceleration = slopeFactor * 15f;
    //                currentSpeed += downSlopeAcceleration * Time.deltaTime;

    //                if (currentSpeed > maxSpeed * 0.7f)
    //                {
    //                    currentSpeed = maxSpeed * 0.7f;
    //                }
    //            }
    //        }

    //        velocity.y = 0f;
    //    }
    //    currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);
    //}
}
