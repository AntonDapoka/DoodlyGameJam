using System;
using System.Collections;
using UnityEngine;

public class StyleSystem : MonoBehaviour
{

    public float pointCounter;
    public static float pointMultiplier = 1f;
    public static float refillSpeed = 10f;
    public SkateboardControls playerScript;
    private bool grounded;
    private bool grinding;
    public static float increment = 0.4f;
    public int visibleScore;

    void Start()
    {
        pointCounter = 0;
        visibleScore = (int)pointCounter;
    }

    void FixedUpdate()
    {
        CheckState();
        if (!grounded || grinding) SetMultiplier(true);
        else SetMultiplier(false);
        Mathf.Clamp(pointMultiplier, 0.8f, 2.5f);
    }

    private void SetMultiplier(bool updown, float a = 1f)
    {
        pointMultiplier += updown ? increment * Time.fixedDeltaTime * a : -increment * Time.fixedDeltaTime * a;
        pointMultiplier = (float)Math.Round(pointMultiplier, 2);
    }

    private void CheckState()
    {
        grounded = playerScript.isGrounded;
        grinding = playerScript.isGrinding;
    }

    public void TrickAddScore(ref int[] trickarr)
    {
        if (trickarr[0] == trickarr[1] && trickarr[0] == trickarr[2]) SetMultiplier(false, 50f);
        else if (trickarr[0] == trickarr[2] && trickarr[0] != trickarr[1]) SetMultiplier(true, 25f);
        else if (trickarr[0] != trickarr[1] && trickarr[1] != trickarr[2] && trickarr[0] != trickarr[2]) SetMultiplier(true, 75f);
        switch (trickarr[0])
        {
            case 0:
                pointCounter += 20 * pointMultiplier;
                break;
            case 1:
                pointCounter += 50 * pointMultiplier;
                break;
            case 2:
                pointCounter += 50 * pointMultiplier;
                break;
            case 3:
                pointCounter += 80 * pointMultiplier;
                break;
            case 4:
                pointCounter += 80 * pointMultiplier;
                break;
            case 5:
                pointCounter += 100 * pointMultiplier;
                break;
            case 6:
                pointCounter += 100 * pointMultiplier;
                break;
        }
        visibleScore = (int)Mathf.Round(pointCounter);
    }
}