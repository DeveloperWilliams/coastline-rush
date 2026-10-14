using System.Collections.Generic;
using UnityEngine;

namespace Coastline
{
    /// <summary>A conservative outside/apex/outside line with a braking speed envelope.</summary>
    public sealed class RacingPlan
    {
        readonly TrackPath track;
        readonly float[] lane, curvature;
        readonly float step;
        public readonly float[] PreviewSpeed;
        public RacingPlan(TrackPath track)
        {
            this.track = track;
            int count = track.Count;
            step = track.Length / count;
            lane = new float[count];
            curvature = new float[count];
            var bends = new List<int>();
            var signed = new float[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 before = track.Direction(i * step - 8), after = track.Direction(i * step + 8);
                signed[i] = Mathf.Sign(Vector3.Cross(before, after).y) * track.Curvature(i * step);
            }
            // Select strongest apex candidates, rejecting nearby duplicates at the seam too.
            var candidates = new List<int>();
            for (int i = 0; i < count; i++)
                if (Mathf.Abs(signed[i]) > .008f && Mathf.Abs(signed[i]) >= Mathf.Abs(signed[track.Wrap(i - 1)]) && Mathf.Abs(signed[i]) > Mathf.Abs(signed[track.Wrap(i + 1)]))
                    candidates.Add(i);
            candidates.Sort((a, b) => Mathf.Abs(signed[b]).CompareTo(Mathf.Abs(signed[a])));
            foreach (int index in candidates)
            {
                bool nearby = false;
                foreach (int bend in bends)
                    nearby |= Mathf.Abs(Delta(index * step, bend * step)) < 55;
                if (!nearby)
                    bends.Add(index);
            }
            for (int i = 0; i < count; i++)
            {
                float offset = 0;
                foreach (int bend in bends)
                {
                    float delta = Delta(i * step, bend * step);
                    offset += Mathf.Sign(signed[bend]) * (2 * Mathf.Exp(-delta * delta / 324) - Mathf.Exp(-delta * delta / 2025));
                }
                lane[i] = Mathf.Clamp(offset, -1, 1) * Mathf.Min(3, track.HalfWidth * .4f);
            }
            var smoothed = new float[count];
            for (int pass = 0; pass < 18; pass++)
            {
                for (int i = 0; i < count; i++)
                    smoothed[i] = (lane[track.Wrap(i - 1)] + lane[i] * 2 + lane[track.Wrap(i + 1)]) * .25f;
                System.Array.Copy(smoothed, lane, count);
            }
            for (int i = 0; i < count; i++)
            {
                Vector3 a = (Position(i * step) - Position(i * step - 8)).normalized;
                Vector3 b = (Position(i * step + 8) - Position(i * step)).normalized;
                curvature[i] = Vector3.Angle(a, b) * Mathf.Deg2Rad / 8;
            }
            PreviewSpeed = BuildSpeedProfile(51, 9.5f, 8);
        }
        float Delta(float a, float b) => Mathf.Repeat(a - b + track.Length * .5f, track.Length) - track.Length * .5f;
        public float Lane(float distance) => Sample(lane, distance);
        public Vector3 Position(float distance, float trafficOffset = 0) => track.Position(distance,
            Mathf.Clamp(Lane(distance) + trafficOffset, -track.HalfWidth + 1.7f, track.HalfWidth - 1.7f));
        public Vector3 Direction(float distance) => (Position(distance + 2) - Position(distance - 2)).normalized;
        public float Sample(float[] values, float distance)
        {
            float index = Mathf.Repeat(distance, track.Length) / step;
            int i = Mathf.FloorToInt(index);
            return Mathf.Lerp(values[i], values[(i + 1) % values.Length], index - i);
        }
        public float[] BuildSpeedProfile(float maximumSpeed, float lateralAcceleration, float braking)
        {
            var speeds = new float[lane.Length];
            for (int i = 0; i < speeds.Length; i++)
                speeds[i] = Mathf.Min(maximumSpeed, Mathf.Sqrt(lateralAcceleration / Mathf.Max(.001f, curvature[i])));
            // Two passes carry braking constraints through the start/finish seam.
            for (int i = speeds.Length * 2 - 1; i >= 0; i--)
            {
                int current = i % speeds.Length, next = (current + 1) % speeds.Length;
                speeds[current] = Mathf.Min(speeds[current], Mathf.Sqrt(speeds[next] * speeds[next] + 2 * braking * step));
            }
            return speeds;
        }
    }
}
