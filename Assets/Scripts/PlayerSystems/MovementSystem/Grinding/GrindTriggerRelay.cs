using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GrindTriggerRelay : MonoBehaviour
{
    private GrindModule _grindModule;

    public void Initialize(GrindModule grindModule)
    {
        _grindModule = grindModule;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_grindModule == null)
            return;

        var marker = other.GetComponent<GrindableMarker>();
        if (marker == null)
            return;

        _grindModule.OnGrindTriggerEntered(marker);
    }
}
