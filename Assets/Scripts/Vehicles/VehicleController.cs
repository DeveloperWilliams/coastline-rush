using UnityEngine;
namespace Coastline
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        public VehicleTuning Tuning
        {
            get; private set;
        }
        public Rigidbody Body
        {
            get; private set;
        }
        public WheelCollider[] Wheels
        {
            get; private set;
        }
        public DriveInput Input
        {
            get; set;
        }
        public bool IsPlayer, ControlEnabled;
        public RoadWeather Weather;
        public TrackPath Track;
        public float Nitro { get; private set; } = 1;
        public float Speed => Body ? Body.linearVelocity.magnitude : 0;
        public float ForwardSpeed => Body ? Vector3.Dot(Body.linearVelocity, transform.forward) : 0;
        public float SpeedKph => Speed * 3.6f;
        public int GroundedWheels
        {
            get; private set;
        }
        public float Slip
        {
            get; private set;
        }
        public bool Boosting
        {
            get; private set;
        }
        public bool OnGrass
        {
            get; private set;
        }
        public float SteeringAngle
        {
            get; private set;
        }
        public float Impact
        {
            get; private set;
        }
        public int CollisionCount
        {
            get; private set;
        }
        public readonly AutomaticGearbox Drivetrain = new AutomaticGearbox();
        public int Gear => Drivetrain.Gear;
        public float EngineRpm => Drivetrain.EngineRpm;
        public TrackLocation Location
        {
            get; private set;
        }
        public Transform[] WheelVisuals;
        public TrailRenderer[] SkidTrails;
        Transform visualBody; Vector3 lastVelocity; float steer, roll, pitch;
        public void Initialize(VehicleTuning tuning, TrackPath track, Transform bodyVisual, Transform[] wheelVisuals)
        {
            Tuning = tuning;
            Track = track;
            visualBody = bodyVisual;
            WheelVisuals = wheelVisuals;
            Body = GetComponent<Rigidbody>();
            Body.mass = tuning.mass;
            Body.centerOfMass = tuning.centreOfMass;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.linearDamping = .045f;
            Body.angularDamping = 1.6f;
            Body.maxAngularVelocity = 4;
            Body.solverIterations = 10;
            Body.solverVelocityIterations = 4;
            Wheels = new WheelCollider[4];
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject(i < 2 ? "Front wheel physics" : "Rear wheel physics");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(i % 2 == 0 ? -.98f : .98f, .57f, i < 2 ? 1.35f : -1.35f);
                var w = go.AddComponent<WheelCollider>();
                w.radius = tuning.wheelRadius;
                w.mass = 24;
                w.suspensionDistance = tuning.suspensionTravel;
                w.wheelDampingRate = .7f;
                w.forceAppPointDistance = .15f;
                var spring = w.suspensionSpring;
                spring.spring = tuning.spring;
                spring.damper = tuning.damper;
                spring.targetPosition = .5f;
                w.suspensionSpring = spring;
                w.ConfigureVehicleSubsteps(8, 8, 12);
                var f = w.forwardFriction;
                f.extremumSlip = .35f;
                f.extremumValue = 1;
                f.asymptoteSlip = .8f;
                f.asymptoteValue = .7f;
                f.stiffness = tuning.tyreGrip;
                w.forwardFriction = f;
                var s = w.sidewaysFriction;
                s.extremumSlip = .22f;
                s.extremumValue = 1;
                s.asymptoteSlip = .65f;
                s.asymptoteValue = .78f;
                s.stiffness = tuning.tyreGrip;
                w.sidewaysFriction = s;
                Wheels[i] = w;
            }
            Location = track.Locate(transform.position);
            lastVelocity = Vector3.zero;
        }
        void FixedUpdate()
        {
            Simulate(Time.fixedDeltaTime);
        }
        public void Simulate(float dt)
        {
            if (Tuning == null)
                return;
            Location = Track.Locate(Body.position);
            OnGrass = Location.Offset > Track.HalfWidth + .3f;
            var input = ControlEnabled ? Input : DriveInput.Stopped;
            steer = Mathf.MoveTowards(steer, input.Steering, Tuning.steeringResponse * dt);
            float maxSteer = Mathf.Lerp(Tuning.steeringAngle, Tuning.highSpeedSteering, Mathf.Clamp01(Speed / 50));
            SteeringAngle = steer * maxSteer;
            float throttle = input.Throttle, brake = input.Brake;
            if (throttle < 0 && ForwardSpeed > 1)
            {
                brake = Mathf.Max(brake, -throttle);
                throttle = 0;
            }
            if (throttle > 0 && ForwardSpeed < -1)
            {
                brake = Mathf.Max(brake, throttle);
                throttle = 0;
            }
            float wheelRpm = 0;
            foreach (var wheel in Wheels)
                wheelRpm += Mathf.Abs(wheel.rpm) * .25f;
            Drivetrain.Tick(wheelRpm, throttle, ForwardSpeed, dt);
            GroundedWheels = 0;
            Slip = 0;
            for (int i = 0; i < 4; i++)
            {
                var w = Wheels[i];
                bool grounded = w.GetGroundHit(out WheelHit hit);
                if (grounded)
                {
                    GroundedWheels++;
                    Slip = Mathf.Max(Slip, Mathf.Abs(hit.sidewaysSlip));
                }
                float grip = Tuning.tyreGrip * (Weather == RoadWeather.Wet ? .76f : 1) * (OnGrass ? .52f : 1);
                var lateral = w.sidewaysFriction;
                lateral.stiffness = grip * (input.Handbrake && i >= 2 ? Tuning.handbrakeGrip : 1);
                w.sidewaysFriction = lateral;
                var forward = w.forwardFriction;
                forward.stiffness = grip;
                w.forwardFriction = forward;
                float torque = throttle * Tuning.torquePerWheel * Drivetrain.TorqueFactor * Tuning.torqueCurve.Evaluate(Mathf.Clamp01(Speed / Tuning.topSpeed));
                if ((ForwardSpeed > Tuning.topSpeed && throttle > 0) || (ForwardSpeed < -Tuning.reverseSpeed && throttle < 0))
                    torque = 0;
                if (grounded && Mathf.Abs(hit.forwardSlip) > .55f)
                    torque *= 1 - Tuning.tractionControl;
                w.motorTorque = torque;
                w.brakeTorque = brake * Tuning.brakeTorque + (input.Handbrake && i >= 2 ? Tuning.brakeTorque * .8f : 0);
                w.steerAngle = i < 2 ? SteeringAngle : 0;
            }
            Boosting = ControlEnabled && input.Boost && Nitro > .03f && GroundedWheels >= 2 && !OnGrass && throttle > 0;
            Nitro = Mathf.Clamp01(Nitro + dt * (Boosting ? -Tuning.boostDrain : Tuning.boostRecharge));
            if (Boosting && Speed < Tuning.topSpeed * 1.25f)
                Body.AddForce(transform.forward * Tuning.boostAcceleration, ForceMode.Acceleration);
            if (GroundedWheels >= 2)
            {
                Body.AddForce(-transform.up * (Tuning.downforce * Speed * Speed));
                AntiRoll(Wheels[0], Wheels[1]);
                AntiRoll(Wheels[2], Wheels[3]);
                if (OnGrass)
                    Body.AddForce(-Body.linearVelocity * Body.mass * .22f);
            }
            // Aerodynamic drag makes terminal speed emerge from forces rather than transform clamping.
            Body.AddForce(-Body.linearVelocity * (.36f * Speed));
            Impact = Mathf.MoveTowards(Impact, 0, dt * 3);
            Vector3 acceleration = (Body.linearVelocity - lastVelocity) / Mathf.Max(.001f, dt);
            lastVelocity = Body.linearVelocity;
            roll = Mathf.Lerp(roll, -Vector3.Dot(acceleration, transform.right) * .22f, dt * 6);
            pitch = Mathf.Lerp(pitch, Vector3.Dot(acceleration, transform.forward) * .15f, dt * 6);
        }
        void AntiRoll(WheelCollider left, WheelCollider right)
        {
            bool l = left.GetGroundHit(out WheelHit lh), r = right.GetGroundHit(out WheelHit rh);
            float lt = 1, rt = 1;
            if (l)
                lt = (-left.transform.InverseTransformPoint(lh.point).y - left.radius) / left.suspensionDistance;
            if (r)
                rt = (-right.transform.InverseTransformPoint(rh.point).y - right.radius) / right.suspensionDistance;
            float force = (lt - rt) * Tuning.antiRoll;
            if (l)
                Body.AddForceAtPosition(left.transform.up * -force, left.transform.position);
            if (r)
                Body.AddForceAtPosition(right.transform.up * force, right.transform.position);
        }
        void LateUpdate()
        {
            if (Wheels == null)
                return;
            for (int i = 0; i < 4; i++)
            {
                if (WheelVisuals != null && i < WheelVisuals.Length && WheelVisuals[i])
                {
                    Wheels[i].GetWorldPose(out Vector3 p, out Quaternion q);
                    WheelVisuals[i].SetPositionAndRotation(p, q);
                }
                if (SkidTrails != null && i < SkidTrails.Length && SkidTrails[i])
                {
                    bool grounded = Wheels[i].GetGroundHit(out WheelHit contact);
                    if (grounded)
                        SkidTrails[i].transform.position = contact.point + contact.normal * .018f;
                    SkidTrails[i].emitting = ControlEnabled && grounded && Speed > 8 && (Slip > .28f || Input.Handbrake);
                }
            }
            if (visualBody)
                visualBody.localRotation = Quaternion.Euler(Mathf.Clamp(pitch, -2, 2), 0, Mathf.Clamp(roll, -3, 3));
        }
        void OnCollisionEnter(Collision collision)
        {
            CollisionCount++;
            Impact = Mathf.Clamp01(collision.relativeVelocity.magnitude / 18);
            if (IsPlayer && Impact > .2f)
                GetComponent<VehicleAudio>()?.PlayImpact(Impact);
        }
        public void ResetAt(float metres, float lane = 0, bool refillBoost = true)
        {
            Vector3 p = Track.Position(metres, lane) + Vector3.up * .32f;
            Body.position = p;
            Body.rotation = Track.Rotation(metres);
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(p, Body.rotation);
            lastVelocity = Vector3.zero;
            steer = 0;
            if (refillBoost)
                Nitro = 1;
            Drivetrain.Reset();
            Input = DriveInput.Stopped;
            if (SkidTrails != null)
                foreach (var trail in SkidTrails)
                    if (trail)
                    {
                        trail.emitting = false;
                        trail.Clear();
                    }
        }
    }
}
