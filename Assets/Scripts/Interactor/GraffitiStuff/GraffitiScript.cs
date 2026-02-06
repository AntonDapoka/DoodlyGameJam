using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GraffitiScript : MonoBehaviour, IInteractable
{
    [SerializeField] private GraffitiManagementInteractorScript _graffitiManagementInteractor;
    public GameObject _objectGraffitiHint;
    private bool _isTurnOn = false;

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
        _isTurnOn = true;
        //Add sprite
        gameObject.SetActive(true);
    }

    public void TurnOff()
    {
        _isTurnOn = false;
        gameObject.SetActive(false);
        _graffitiManagementInteractor.SetRandomGraffitiSpot(this);
    }

    public bool GetIsTurnOn()
    {
        return _isTurnOn;
    }
}
