using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManagementScript : MonoBehaviour
{

    [SerializeField] private int score;
    [SerializeField] private SprayPaintUIScript sprayPaintUI;

    private bool sprayingTriggered;
    private bool superTriggered;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.D))
        {
            AddScore(1);
        }

        //Ну это надо изменить, даааа
    }

    private void AddScore(int value)
    {
        score += value;

        if (!sprayingTriggered && score >= 50)
        {
            sprayingTriggered = true;
            sprayPaintUI.SetSpritesSpraying();
        }

        if (!superTriggered && score >= 100)
        {
            superTriggered = true;
            sprayPaintUI.SetSpritesSuper();
        }

        if (score <= 100)
            sprayPaintUI.SetBackPositionPercent(score);
    }
}