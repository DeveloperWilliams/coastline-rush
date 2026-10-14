using System;
using UnityEngine;
namespace Coastline
{
    /// <summary>Closed Catmull-Rom circuit, sampled and queried in metres rather than sample indices.</summary>
    public sealed class TrackPath
    {
        public readonly Vector3[] Points;
        public readonly float[] Distances;
        public readonly float HalfWidth;
        public float Length
        {
            get; private set;
        }
        public string Name
        {
            get; private set;
        }
        public string Id
        {
            get; private set;
        }
        public int Count => Points.Length;
        public TrackPath(int layout = 0, int resolution = 600)
        {
            HalfWidth = layout == 0 ? 8.5f : 8;
            Name = layout == 0 ? "PALM COAST" : "HIGHLAND LOOP";
            Id = layout == 0 ? "palm-coast-v2" : "highland-loop-v2";
            Vector3[] controls = layout == 0 ? new[] { V(0, 0, -100), V(105, 0, -100), V(170, 1, -50), V(150, 3, 35), V(60, 4, 60), V(105, 3, 125), V(35, 1, 175), V(-70, .5f, 145), V(-85, 0, 65), V(-160, 0, 25), V(-150, 0, -65), V(-70, 0, -100) } : new[] { V(0, 0, -110), V(105, 1, -110), V(175, 6, -55), V(160, 8, 40), V(95, 5, 70), V(50, 3, 145), V(-45, 2, 175), V(-140, 4, 100), V(-95, 6, 25), V(-160, 2, -45), V(-90, 0, -110) };
            Points = new Vector3[resolution];
            Distances = new float[resolution + 1];
            for (int i = 0; i < resolution; i++)
            {
                float t = (float)i / resolution * controls.Length;
                int k = (int)t;
                float u = t - k;
                Vector3 a = controls[(k + controls.Length - 1) % controls.Length], b = controls[k], c = controls[(k + 1) % controls.Length], d = controls[(k + 2) % controls.Length];
                Points[i] = .5f * (2 * b + (-a + c) * u + (2 * a - 5 * b + 4 * c - d) * u * u + (-a + 3 * b - 3 * c + d) * u * u * u);
                if (i > 0)
                    Distances[i] = Distances[i - 1] + Vector3.Distance(Points[i - 1], Points[i]);
            }
            Length = Distances[resolution] = Distances[resolution - 1] + Vector3.Distance(Points[resolution - 1], Points[0]);
        }
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        public int Wrap(int i) => (i % Count + Count) % Count;
        public Vector3 Forward(int i) => (Points[Wrap(i + 2)] - Points[Wrap(i - 2)]).normalized;
        public Vector3 Right(int i) => Vector3.Cross(Vector3.up, Forward(i)).normalized;
        public Quaternion Rotation(float metres) => Quaternion.LookRotation(Direction(metres), Vector3.up);
        public int Index(float metres)
        {
            float d = Mathf.Repeat(metres, Length);
            int n = Array.BinarySearch(Distances, d);
            if (n < 0)
                n = ~n - 1;
            return Mathf.Clamp(n, 0, Count - 1);
        }
        public Vector3 Position(float metres, float lane = 0)
        {
            int i = Index(metres);
            float d = Mathf.Repeat(metres, Length), t = (d - Distances[i]) / (Distances[i + 1] - Distances[i]);
            return Vector3.Lerp(Points[i], Points[Wrap(i + 1)], t) + Right(i) * lane;
        }
        public Vector3 Direction(float metres) => (Position(metres + 2) - Position(metres - 2)).normalized;
        public float Curvature(float metres)
        {
            Vector3 a = Direction(metres - 8), b = Direction(metres + 8);
            return Vector3.Angle(a, b) * Mathf.Deg2Rad / 16;
        }
        public TrackLocation Locate(Vector3 p)
        {
            int nearest = 0;
            float best = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                float d = (Points[i].x - p.x) * (Points[i].x - p.x) + (Points[i].z - p.z) * (Points[i].z - p.z);
                if (d < best)
                {
                    best = d;
                    nearest = i;
                }
            }
            int previous = Wrap(nearest - 1), next = nearest;
            float ds = float.MaxValue;
            TrackLocation result = default;
            foreach (int i in new[] { previous, next })
            {
                Vector3 a = Points[i], b = Points[Wrap(i + 1)], ab = b - a;
                Vector3 planar = new Vector3(ab.x, 0, ab.z);
                float t = Mathf.Clamp01(Vector3.Dot(new Vector3(p.x - a.x, 0, p.z - a.z), planar) / Mathf.Max(.001f, planar.sqrMagnitude));
                Vector3 foot = Vector3.Lerp(a, b, t);
                float d = (new Vector2(p.x - foot.x, p.z - foot.z)).sqrMagnitude;
                if (d < ds)
                {
                    ds = d;
                    result = new TrackLocation { Index = i, Point = foot, Distance = Distances[i] + Vector3.Distance(a, b) * t, Lateral = Vector3.Dot(p - foot, Right(i)), Offset = Mathf.Sqrt(d) };
                }
            }
            return result;
        }
    }
    public struct TrackLocation
    {
        public int Index; public Vector3 Point; public float Distance, Lateral, Offset;
    }
}
