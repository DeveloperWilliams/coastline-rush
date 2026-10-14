using System.Collections.Generic;
using UnityEngine;

namespace Coastline
{
    public sealed class RacingGuide : MonoBehaviour
    {
        readonly List<Renderer> markers = new List<Renderer>();
        readonly List<float> distances = new List<float>();
        RacingPlan plan;
        VehicleController vehicle;
        Material green, amber, red;
        float[] speeds;
        float nextUpdate;
        public void Initialize(RacingPlan plan, TrackPath track, WorldMaterials materials)
        {
            this.plan = plan;
            green = materials.Unlit("Guide / accelerate", new Color(.2f, .94f, .65f));
            amber = materials.Unlit("Guide / lift", new Color(1, .74f, .18f));
            red = materials.Unlit("Guide / brake", new Color(1, .24f, .16f));
            for (float distance = 4; distance < track.Length; distance += 7)
            {
                var marker = WorldObject.Box("Braking guide", plan.Position(distance) + Vector3.up * .055f,
                    new Vector3(.33f, .018f, 2.3f), green, transform);
                marker.transform.rotation = Quaternion.LookRotation(plan.Direction(distance));
                markers.Add(marker.GetComponent<Renderer>());
                distances.Add(distance);
            }
        }
        public void SetVehicle(VehicleController target)
        {
            vehicle = target;
            float grip = target.Tuning.tyreGrip / 1.65f * (target.Weather == RoadWeather.Wet ? .76f : 1);
            speeds = plan.BuildSpeedProfile(target.Tuning.topSpeed * .94f, 9.5f * grip, 8);
        }
        void Update()
        {
            if (Time.unscaledTime < nextUpdate)
                return;
            nextUpdate = Time.unscaledTime + .1f;
            for (int i = 0; i < markers.Count; i++)
            {
                float target = plan.Sample(speeds ?? plan.PreviewSpeed, distances[i]);
                float excess = vehicle && vehicle.ControlEnabled ? vehicle.Speed - target : 40 - target;
                markers[i].sharedMaterial = excess > 3 ? red : excess > .5f ? amber : green;
            }
        }
        public float SuggestedSpeed(float distance) => plan.Sample(speeds ?? plan.PreviewSpeed, distance);
    }
}
