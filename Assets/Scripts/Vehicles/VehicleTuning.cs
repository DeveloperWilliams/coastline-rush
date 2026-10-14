using UnityEngine;
namespace Coastline
{
    [CreateAssetMenu(menuName = "Coastline/Vehicle tuning")]
    public sealed class VehicleTuning : ScriptableObject
    {
        [Header("Chassis (SI units)")][Min(100)] public float mass = 1200;
        public Vector3 centreOfMass = new Vector3(0, .32f, 0);
        [Min(.1f)] public float wheelRadius = .36f;
        [Min(.05f)] public float suspensionTravel = .28f;
        [Min(1000)] public float spring = 42000;
        [Min(100)] public float damper = 5500;
        [Min(0)] public float antiRoll = 7500;
        [Header("Drivetrain")][Min(0)] public float torquePerWheel = 730;
        [Min(0)] public float brakeTorque = 2600;
        [Min(10)] public float topSpeed = 54;
        [Min(5)] public float reverseSpeed = 9;
        public AnimationCurve torqueCurve = AnimationCurve.EaseInOut(0, 1, 1, .35f);
        [Header("Handling")][Range(10, 45)] public float steeringAngle = 30;
        [Range(3, 20)] public float highSpeedSteering = 10;
        [Min(.1f)] public float steeringResponse = 4;
        [Range(.2f, 3)] public float tyreGrip = 1.65f;
        [Range(0, 1)] public float tractionControl = .55f;
        [Range(.1f, 1)] public float handbrakeGrip = .52f;
        [Min(0)] public float downforce = 3.5f;
        [Header("Boost")][Min(1)] public float boostAcceleration = 9;
        [Min(.01f)] public float boostDrain = .22f;
        [Min(.01f)] public float boostRecharge = .065f;
        public static VehicleTuning ForModel(VehicleTuning template, int model)
        {
            var tune = Instantiate(template);
            tune.name = VehicleFactory.Names[model % 3] + " handling";
            if (model % 3 == 1)
            {
                tune.mass *= .94f;
                tune.torquePerWheel *= .89f;
                tune.topSpeed *= .93f;
                tune.tyreGrip *= 1.10f;
                tune.steeringResponse *= 1.15f;
                tune.highSpeedSteering *= .90f;
                tune.tractionControl = .70f;
                tune.boostAcceleration *= .90f;
            }
            else if (model % 3 == 2)
            {
                tune.mass *= .92f;
                tune.torquePerWheel *= 1.07f;
                tune.topSpeed *= 1.09f;
                tune.tyreGrip *= .94f;
                tune.steeringResponse *= .90f;
                tune.highSpeedSteering *= .95f;
                tune.tractionControl = .38f;
                tune.downforce *= 1.12f;
                tune.boostAcceleration *= 1.08f;
            }
            return tune;
        }
        public static VehicleTuning Defaults() => CreateInstance<VehicleTuning>();
    }
    public struct DriveInput
    {
        public float Throttle, Steering, Brake; public bool Handbrake, Boost; public static DriveInput Stopped => new DriveInput { Brake = 1 };
    }
}
