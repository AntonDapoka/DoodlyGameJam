using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class SkateboardControls : MonoBehaviour
{

    [SerializeField] GameObject Player;
    private float velocity = 0;
    private float turning = 0;
    private float fwdAcceleration = 0;

    private KeyCode fwd = KeyCode.W;
    private KeyCode turnRight = KeyCode.D;
    private KeyCode turnLeft = KeyCode.A;
    private KeyCode bck = KeyCode.S;
    private KeyCode jump = KeyCode.Space;

    private bool grounded = false;
    private bool canPushFwd = true;

    void Start()
    {
        
    }

    void Update()
    {
        //check state


        // Get Input
        if (grounded)
        {
            if (canPushFwd)
            {

            }
        }


    }

    IEnumerator FwdPushDelay()
    {
        canPushFwd = false;
        yield return new WaitForSeconds(0.9f);
        canPushFwd = true;
    }
}
