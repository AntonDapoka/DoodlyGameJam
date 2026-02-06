using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GraffitiScript : MonoBehaviour, IInteractable
{
    public GameObject _objectGraffitiHint;
    private bool isTurnOn = false;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void Interact()
    {
        TurnOff();
    }

    public void TurnOn()
    {
        isTurnOn = true;
        //Add sprite
        gameObject.SetActive(true);
    }

    public void TurnOff()
    {
        isTurnOn = false;
        gameObject.SetActive(false);
    }
}
