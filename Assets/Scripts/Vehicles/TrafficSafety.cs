using System.Collections.Generic;
using UnityEngine;

namespace Coastline
{
    public struct TrafficSample
    {
        public float Distance, Lateral, Speed;
    }
    public static class TrafficSafety
    {
        /// <summary>Reject occupied lanes and a faster car approaching from behind.</summary>
        public static bool IsLaneClear(float lane, float speed, IList<TrafficSample> traffic)
        {
            foreach (var other in traffic)
            {
                if (Mathf.Abs(other.Lateral - lane) > 2.8f)
                    continue;
                float closing = Mathf.Max(0, other.Speed - speed);
                if (other.Distance > -12 - closing * 1.5f && other.Distance < 26)
                    return false;
            }
            return true;
        }
    }
}
