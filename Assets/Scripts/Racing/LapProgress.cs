using UnityEngine;
namespace Coastline
{
    /// <summary>Ordered sectors, signed progress and a finish crossing. Teleports cannot award laps.</summary>
    public sealed class LapProgress
    {
        public const int SectorCount = 12;
        public int CompletedLaps
        {
            get; private set;
        }
        public int NextSector { get; private set; } = 1;
        public float AbsoluteDistance
        {
            get; private set;
        }
        public float LastLap
        {
            get; private set;
        }
        public float BestLap
        {
            get; private set;
        }
        public bool WrongWay
        {
            get; private set;
        }
        public bool LapInvalid
        {
            get; private set;
        }
        public bool Finished
        {
            get; private set;
        }
        public bool LastLapEligible
        {
            get; private set;
        }
        public bool RecordEligible { get; private set; } = true;
        float previous, lapStarted, wrongTime; bool crossedStart;
        public void Reset(float gridDistance, float length)
        {
            CompletedLaps = 0;
            NextSector = 1;
            AbsoluteDistance = gridDistance;
            previous = Mathf.Repeat(gridDistance, length);
            LastLap = BestLap = 0;
            lapStarted = wrongTime = 0;
            crossedStart = false;
            LapInvalid = Finished = WrongWay = false;
            RecordEligible = true;
            LastLapEligible = false;
        }
        public bool Tick(float courseDistance, float lateral, float length, float halfWidth, float time, float dt, int laps)
        {
            float delta = courseDistance - previous;
            if (delta > length * .5f)
                delta -= length;
            if (delta < -length * .5f)
                delta += length;
            previous = courseDistance;
            if (Mathf.Abs(delta) > 35)
            {
                LapInvalid = true;
                return false;
            }
            AbsoluteDistance += delta;
            wrongTime = delta < -.04f ? wrongTime + dt : Mathf.Max(0, wrongTime - dt * 2);
            WrongWay = wrongTime > 1.5f;
            if (!crossedStart)
            {
                if (AbsoluteDistance >= 0)
                {
                    crossedStart = true;
                    lapStarted = time;
                }
                return false;
            }
            float within = AbsoluteDistance - CompletedLaps * length;
            if (Mathf.Abs(lateral) > halfWidth + 3 && delta > 0)
                LapInvalid = true;
            float next = NextSector * length / SectorCount;
            if (NextSector < SectorCount && within >= next && within < next + length / SectorCount * .7f && Mathf.Abs(lateral) <= halfWidth + 2)
                NextSector++;
            if (within >= length && delta > 0)
            {
                if (NextSector == SectorCount && !LapInvalid)
                {
                    LastLap = time - lapStarted;
                    LastLapEligible = RecordEligible;
                    if (LastLapEligible && (BestLap == 0 || LastLap < BestLap))
                        BestLap = LastLap;
                    RecordEligible = true;
                    CompletedLaps++;
                    Finished = CompletedLaps >= laps;
                    NextSector = 1;
                    lapStarted = time;
                    return true;
                }
                // An invalid circuit must be driven again, with every sector visited in order.
                AbsoluteDistance = CompletedLaps * length + Mathf.Repeat(within, length);
                NextSector = 1;
                lapStarted = time;
                LapInvalid = false;
                RecordEligible = true;
            }
            return false;
        }
        public void Recover(float distance, float length)
        {
            previous = Mathf.Repeat(distance, length);
            AbsoluteDistance = distance;
            LapInvalid = false;
            RecordEligible = false;
            WrongWay = false;
            wrongTime = 0;
        }
    }
}
