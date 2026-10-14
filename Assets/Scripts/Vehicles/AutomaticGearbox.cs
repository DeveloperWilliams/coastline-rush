using UnityEngine;

namespace Coastline
{
    /// <summary>One arcade drivetrain state drives torque, HUD and engine audio.</summary>
    public sealed class AutomaticGearbox
    {
        static readonly float[] ratios = { 4.2f, 2.65f, 1.8f, 1.34f, 1.06f };
        const float FinalDrive = 3.9f;
        public int Gear { get; private set; } = 1;
        public float EngineRpm { get; private set; } = 1100;
        public bool IsShifting => shift > 0;
        public float NormalizedRpm => Mathf.Clamp01((EngineRpm - 1100) / 5800);
        public float TorqueFactor => IsShifting ? .2f : Mathf.Lerp(1.12f, .85f, (Gear - 1) / 4f);
        float shift, cooldown;

        public void Tick(float wheelRpm, float throttle, float forwardSpeed, float dt)
        {
            shift = Mathf.Max(0, shift - dt);
            cooldown = Mathf.Max(0, cooldown - dt);
            float coupled = Mathf.Abs(wheelRpm) * ratios[Gear - 1] * FinalDrive;
            if (forwardSpeed <= 2 && Gear > 1 && cooldown <= 0)
                Change(1);
            if (forwardSpeed > 2 && cooldown <= 0)
            {
                if (Gear < 5 && coupled > 6200)
                    Change(Gear + 1);
                else if (Gear > 1 && coupled < 2400)
                    Change(Gear - 1);
            }
            float target = Mathf.Clamp(Mathf.Max(1100 + Mathf.Abs(throttle) * 450,
                Mathf.Abs(wheelRpm) * ratios[Gear - 1] * FinalDrive), 1100, 7200);
            EngineRpm = Mathf.Lerp(EngineRpm, target, 1 - Mathf.Exp(-dt * 12));
        }
        void Change(int next)
        {
            Gear = next;
            shift = .14f;
            cooldown = .55f;
        }
        public void Reset()
        {
            Gear = 1;
            EngineRpm = 1100;
            shift = cooldown = 0;
        }
    }
}
