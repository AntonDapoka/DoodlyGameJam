using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Interactor
{
    public class PlayerRotationScript : MonoBehaviour
    {
        [SerializeField] private LayerMask groundMask;
        
        [SerializeField] private AnimationCurve animationCurve;

        private void FixedUpdate()
        {
            SurfaceAllignment();
        }

        private void SurfaceAllignment()
        {
            Ray ray = new Ray(transform.position, -transform.up);
            RaycastHit info = new RaycastHit();
            Quaternion RotationRef = Quaternion.Euler(0f, 0f, 0f);

            if(Physics.Raycast(ray, out info, groundMask))
            {
                RotationRef = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(Vector3.up, info.normal), 1f); //, animationCurve.Evaluate(Time)
                transform.rotation = Quaternion.Euler(RotationRef.eulerAngles.x, RotationRef.eulerAngles.y, RotationRef.eulerAngles.z);
            }
        }
    }
}



//upright = Vector3.Cross(transform.right, -(fHit.point - bHit.point).normalized)

//transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.right, upright));