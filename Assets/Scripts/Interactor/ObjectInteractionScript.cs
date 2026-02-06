using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInteractionScript : MonoBehaviour
{
    [SerializeField] private Transform _interactivitySource;
    [SerializeField] private float _interactivityRange;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) //Rewrite
        {
            Ray r = new(_interactivitySource.position, _interactivitySource.forward);

            if (Physics.Raycast(r, out RaycastHit hit, _interactivityRange))
            {
                if (hit.collider.gameObject.TryGetComponent(out IInteractable interactObject))
                {
                    interactObject.Interact();
                }
            }
        }       
    }
}
