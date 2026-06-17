using System;
using UnityEngine;

public class StyleSystem : MonoBehaviour
{
    public float pointCounter;
    public static float pointMultiplier = 1f;
    public static float refillSpeed = 10f;
    public static float increment = 0.4f;

    public SkateboardMovementInteractorScript playerScript;

    public int visibleScore;

    private bool grounded;
    private bool grinding;

    private void Start()
    {
        pointCounter = 0;
        visibleScore = (int)pointCounter;
    }

    private void FixedUpdate()
    {
        CheckState();

        if (!grounded || grinding)
            SetMultiplier(true);
        else
            SetMultiplier(false);

        pointMultiplier = Mathf.Clamp(pointMultiplier, 0.8f, 2.5f);
    }

    private void SetMultiplier(bool updown, float a = 1f)
    {
        pointMultiplier += updown
            ? increment * Time.fixedDeltaTime * a
            : -increment * Time.fixedDeltaTime * a;

        pointMultiplier = (float)Math.Round(pointMultiplier, 2);
    }

    private void CheckState()
    {
        grounded = playerScript.IsGrounded;
        grinding = playerScript.IsGrinding;
    }
}
