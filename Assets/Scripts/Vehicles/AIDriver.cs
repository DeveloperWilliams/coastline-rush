using UnityEngine;
namespace Coastline
{
    /// <summary>Pure pursuit steering and a curvature/braking speed envelope. Uses the same WheelColliders as the player.</summary>
    [RequireComponent(typeof(VehicleController))]
    public sealed class AIDriver : MonoBehaviour
    {
        public float Pace = .9f, Lane, MaximumSpeed = 44;
        public RaceDirector Director;
        public RaceEntrant Entrant;
        VehicleController car; float stuck, laneTarget, passTime;
        float preferredLane;
        bool initializedLane;
        void Awake()
        {
            car = GetComponent<VehicleController>();
        }
        void Update()
        {
            if (!car.ControlEnabled || Director == null || Director.State != RaceState.Racing)
            {
                car.Input = DriveInput.Stopped;
                return;
            }
            var location = car.Track.Locate(transform.position);
            float distance = location.Distance, speed = car.Speed;
            float lookahead = Mathf.Clamp(7 + speed * .55f, 8, 30);
            Vector3 target = car.Track.Position(distance + lookahead, Lane);
            Vector3 local = transform.InverseTransformPoint(target);
            float wheelbase = 2.7f;
            float steerDegrees = Mathf.Atan2(2 * wheelbase * local.x, Mathf.Max(4, local.x * local.x + local.z * local.z)) * Mathf.Rad2Deg;
            float steering = steerDegrees / Mathf.Lerp(car.Tuning.steeringAngle, car.Tuning.highSpeedSteering, Mathf.Clamp01(speed / 50));
            float desired = MaximumSpeed * Pace;
            for (float ahead = 5; ahead < 75; ahead += 8)
            {
                float curvature = car.Track.Curvature(distance + ahead);
                float corner = Mathf.Sqrt((car.Weather == RoadWeather.Wet ? 7.8f : 10.8f) / Mathf.Max(.001f, curvature)) * Pace;
                float allowable = Mathf.Sqrt(corner * corner + 2 * 8 * Mathf.Max(0, ahead - 5));
                desired = Mathf.Min(desired, allowable);
            }
            if (location.Offset > car.Track.HalfWidth - 1.2f)
                desired = Mathf.Min(desired, 18);
            if (!initializedLane)
            {
                preferredLane = Lane;
                initializedLane = true;
            }
            passTime = Mathf.Max(0, passTime - Time.deltaTime);
            if (passTime <= 0)
                laneTarget = preferredLane;
            foreach (var other in Director.Entrants)
            {
                if (other.Vehicle == car)
                    continue;
                Vector3 ahead = transform.InverseTransformPoint(other.Vehicle.transform.position);
                if (ahead.z > 2 && ahead.z < 16 && Mathf.Abs(ahead.x) < 2.3f)
                {
                    desired = Mathf.Min(desired, Mathf.Max(8, other.Vehicle.Speed - 1));
                    if (passTime <= 0)
                    {
                        // Hold a passing lane long enough to avoid switching sides every frame.
                        laneTarget = other.Vehicle.Location.Lateral > 0 ? -3.2f : 3.2f;
                        passTime = 2.5f;
                    }
                }
            }
            Lane = Mathf.MoveTowards(Lane, laneTarget, Time.deltaTime * 2);
            float error = desired - speed;
            car.Input = new DriveInput { Throttle = Mathf.Clamp01(error * .25f), Brake = Mathf.Clamp01(-error * .18f), Steering = Mathf.Clamp(steering, -1, 1), Boost = error > 7 && Mathf.Abs(steering) < .14f && speed > 25 && location.Offset < 4 };
            stuck = speed < 2 ? stuck + Time.deltaTime : 0;
            if (stuck > 5 || transform.up.y < .35f || transform.position.y < -8)
            {
                Director.Recover(Entrant);
                stuck = 0;
            }
        }
    }
}
