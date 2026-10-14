using System.Collections.Generic;
using UnityEngine;

namespace Coastline
{
    /// <summary>Physical pursuit driver with a braking envelope and checked passing lanes.</summary>
    [RequireComponent(typeof(VehicleController))]
    public sealed class AIDriver : MonoBehaviour
    {
        public float Pace = .9f, Lane, MaximumSpeed = 44, Aggression = .5f;
        public RaceDirector Director;
        public RaceEntrant Entrant;
        VehicleController car;
        readonly List<TrafficSample> traffic = new List<TrafficSample>(6);
        float stuck, laneTarget, passTime, preferredLane;
        float[] speeds;
        bool initializedLane;
        void Awake() => car = GetComponent<VehicleController>();
        public void ResetDriver(float lane)
        {
            Lane = lane;
            preferredLane = Mathf.Sign(lane) * .65f;
            laneTarget = preferredLane;
            passTime = stuck = 0;
            initializedLane = true;
        }
        void Update()
        {
            if (!car.ControlEnabled || Director == null || !Director.IsRaceActive)
            {
                car.Input = DriveInput.Stopped;
                return;
            }
            if (!initializedLane)
                ResetDriver(Lane);
            var plan = Director.Circuit.Plan;
            if (speeds == null)
                speeds = plan.BuildSpeedProfile(car.Tuning.topSpeed * .94f,
                    (car.Weather == RoadWeather.Wet ? 7.8f : 10.3f) * car.Tuning.tyreGrip / 1.65f, 8);
            var location = car.Location;
            float distance = location.Distance, speed = car.Speed;
            float desired = Mathf.Min(MaximumSpeed * Pace, plan.Sample(speeds, distance + 5) * Pace);
            // Retain a conservative road-curvature limit as a guard against aggressive line shapes.
            for (float ahead = 5; ahead < 75; ahead += 8)
            {
                float corner = Mathf.Sqrt((car.Weather == RoadWeather.Wet ? 7.8f : 10.8f) / Mathf.Max(.001f, car.Track.Curvature(distance + ahead))) * Pace;
                desired = Mathf.Min(desired, Mathf.Sqrt(corner * corner + 16 * Mathf.Max(0, ahead - 5)));
            }
            traffic.Clear();
            float nearest = float.MaxValue, leadingSpeed = 0;
            foreach (var other in Director.Entrants)
            {
                if (other.Vehicle == car)
                    continue;
                float gap = Mathf.Repeat(other.Vehicle.Location.Distance - distance + car.Track.Length * .5f, car.Track.Length) - car.Track.Length * .5f;
                if (gap < -65 || gap > 65)
                    continue;
                float lateral = other.Vehicle.Location.Lateral;
                traffic.Add(new TrafficSample { Distance = gap, Lateral = lateral, Speed = other.Vehicle.Speed });
                Vector3 relative = transform.InverseTransformPoint(other.Vehicle.transform.position);
                if (gap > 0 && relative.z > 0 && Mathf.Abs(relative.x) < 2.5f && gap < nearest)
                {
                    nearest = gap;
                    leadingSpeed = other.Vehicle.Speed;
                }
            }
            passTime = Mathf.Max(0, passTime - Time.deltaTime);
            float baseline = plan.Lane(distance + 12);
            if (passTime <= 0)
                laneTarget = preferredLane;
            if (nearest < 22 && passTime <= 0 && !Entrant.Progress.Finished)
            {
                float left = -2.8f, right = 2.8f;
                bool canLeft = TrafficSafety.IsLaneClear(baseline + left, speed, traffic);
                bool canRight = TrafficSafety.IsLaneClear(baseline + right, speed, traffic);
                if (canLeft || canRight)
                {
                    laneTarget = canLeft && canRight ? (Lane <= 0 ? left : right) : canLeft ? left : right;
                    passTime = 3.5f;
                }
            }
            // Check again while moving: a newly occupied lane pauses the manoeuvre.
            bool safeTarget = TrafficSafety.IsLaneClear(baseline + laneTarget, speed, traffic);
            if (safeTarget || Mathf.Abs(Lane - laneTarget) < .15f)
                Lane = Mathf.MoveTowards(Lane, laneTarget, Time.deltaTime * 1.5f);
            float followingGap = 5 + speed * Mathf.Lerp(.65f, .38f, Aggression);
            if (nearest < followingGap)
                desired = Mathf.Min(desired, Mathf.Max(0, leadingSpeed + (nearest - followingGap) * .8f));
            if (location.Offset > car.Track.HalfWidth - 1.2f)
                desired = Mathf.Min(desired, 18);
            if (Entrant.Progress.Finished)
                desired = Mathf.Min(desired, 24);
            float lookahead = Mathf.Clamp(7 + speed * .55f, 8, 30);
            Vector3 local = transform.InverseTransformPoint(plan.Position(distance + lookahead, Lane));
            float steerDegrees = Mathf.Atan2(5.4f * local.x, Mathf.Max(4, local.x * local.x + local.z * local.z)) * Mathf.Rad2Deg;
            float steering = steerDegrees / Mathf.Lerp(car.Tuning.steeringAngle, car.Tuning.highSpeedSteering, Mathf.Clamp01(speed / 50));
            float error = desired - speed;
            car.Input = new DriveInput
            {
                Throttle = Mathf.Clamp01(error * .25f),
                Brake = Mathf.Clamp01(-error * .18f),
                Steering = Mathf.Clamp(steering, -1, 1),
                Boost = !Entrant.Progress.Finished && error > 7 && Mathf.Abs(steering) < .14f && speed > 25 && location.Offset < 4 && nearest > 30
            };
            stuck = speed < 2 ? stuck + Time.deltaTime : 0;
            if (!Entrant.Progress.Finished && (stuck > 5 || transform.up.y < .35f || transform.position.y < -8))
            {
                Director.Recover(Entrant);
                stuck = 0;
            }
        }
    }
}
