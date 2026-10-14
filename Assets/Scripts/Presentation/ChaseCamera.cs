using UnityEngine;
namespace Coastline
{
    public sealed class ChaseCamera : MonoBehaviour
    {
        public RaceDirector Director;
        public VehicleController Target;
        Camera lens; Vector3 smoothVelocity; int mode; float orbit;
        void Awake()
        {
            lens = GetComponent<Camera>();
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
                mode = (mode + 1) % 3;
        }
        void LateUpdate()
        {
            if (!Target || !Director)
                return;
            float dt = Time.unscaledDeltaTime;
            Vector3 position = Target.transform.position, forward = Target.transform.forward;
            if (Director.State == RaceState.Garage)
            {
                orbit += dt * .16f;
                Vector3 offset = new Vector3(Mathf.Sin(orbit) * 7, 3.3f, Mathf.Cos(orbit) * 7);
                transform.position = Vector3.Lerp(transform.position, position + offset, 1 - Mathf.Exp(-dt * 3));
                Vector3 garageLook = position + Vector3.up * .65f;
                Vector3 right = Vector3.Cross(Vector3.up, (garageLook - transform.position).normalized);
                transform.LookAt(garageLook - right * 2.7f);
                lens.fieldOfView = 48;
                return;
            }
            bool rear = Input.GetKey(KeyCode.Q);
            if (rear)
                forward = -forward;
            Vector3 desired;
            if (mode == 1)
            {
                desired = position + forward * 1.35f + Vector3.up * 1.3f;
                transform.position = desired;
                transform.rotation = Quaternion.LookRotation(forward + Vector3.down * .05f);
                lens.fieldOfView = 78;
                return;
            }
            float distance = mode == 2 ? 13 : 7.8f, height = mode == 2 ? 6.5f : 3.1f;
            Vector3 velocityDirection = Target.Speed > 5 ? Target.Body.linearVelocity.normalized : forward;
            Vector3 trailing = Vector3.Lerp(forward, velocityDirection, .18f).normalized;
            desired = position - trailing * (distance + Target.Speed * .025f) + Vector3.up * height;
            // Only scenery and track walls can obstruct the camera; ignore the followed chassis.
            if (Physics.Linecast(position + Vector3.up * 1.1f, desired, out RaycastHit hit) && hit.collider.attachedRigidbody != Target.Body)
                desired = hit.point + hit.normal * .35f;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref smoothVelocity, .11f, 250, dt);
            Vector3 look = position + forward * 4.5f + Vector3.up * .9f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look - transform.position), 1 - Mathf.Exp(-dt * 8));
            lens.fieldOfView = Mathf.Lerp(lens.fieldOfView, 62 + Target.Speed * .24f + (Target.Boosting ? 6 : 0), 1 - Mathf.Exp(-dt * 5));
            if (Target.Impact > .2f)
                transform.position += transform.right * Mathf.Sin(Time.unscaledTime * 70) * Target.Impact * .06f;
        }
        public void Snap()
        {
            if (!Target)
                return;
            transform.position = Target.transform.position - Target.transform.forward * 8 + Vector3.up * 3.2f;
            transform.LookAt(Target.transform.position + Target.transform.forward * 5 + Vector3.up);
            smoothVelocity = Vector3.zero;
        }
    }
}
