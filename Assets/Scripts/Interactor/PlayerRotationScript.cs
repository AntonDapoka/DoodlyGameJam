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
            SurfaceAllign();
        }

        private void SurfaceAllign()
        {
            Ray ray = new Ray(transform.position, -transform.up);
            RaycastHit info = new RaycastHit();
            Quaternion rotationRef = Quaternion.Euler(0f, 0f, 0f);

            if(Physics.Raycast(ray, out info, groundMask))
            {
                rotationRef = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(Vector3.up, info.normal), 1f); //, animationCurve.Evaluate(Time)
                transform.rotation = Quaternion.Euler(rotationRef.eulerAngles.x, rotationRef.eulerAngles.y, rotationRef.eulerAngles.z);
            }
        }
    }
}



//upright = Vector3.Cross(transform.right, -(fHit.point - bHit.point).normalized)

//transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.right, upright));