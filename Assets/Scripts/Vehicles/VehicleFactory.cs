using UnityEngine;
namespace Coastline
{
    public static class VehicleFactory
    {
        public static readonly string[] Models = { "race", "hatchback-sports", "race-future" };
        public static readonly string[] Names = { "APEX GT", "CLUB SPORT", "NOVA R" };
        public static VehicleController Create(int modelIndex, Color accent, bool player, TrackPath track, VehicleTuning tuning, WorldMaterials materials, Transform parent)
        {
            tuning = VehicleTuning.ForModel(tuning, modelIndex);
            materials.Own(tuning);
            var root = new GameObject(player ? "Player / " + Names[modelIndex % 3] : "Rival");
            root.transform.SetParent(parent, false);
            var visual = new GameObject("Suspended body").transform;
            visual.SetParent(root.transform, false);
            var asset = Resources.Load<GameObject>("KenneyCars/" + Models[modelIndex % 3]);
            if (asset)
            {
                var art = Object.Instantiate(asset, visual);
                art.name = "Kenney body";
                foreach (var r in art.GetComponentsInChildren<Renderer>())
                    if (r.name.StartsWith("wheel"))
                        r.gameObject.SetActive(false);
                Bounds b = WorldObject.BoundsOf(art);
                float scale = 4.25f / Mathf.Max(.1f, b.size.z);
                art.transform.localScale = Vector3.one * scale;
                art.transform.localPosition = new Vector3(-b.center.x * scale, .24f - b.min.y * scale, -b.center.z * scale);
            }
            else
                WorldObject.Box("Fallback body", new Vector3(0, .7f, 0), new Vector3(1.9f, .5f, 4.2f), materials.Teal, visual);
            var accentMat = materials.Make("Livery " + root.name, accent, .4f);
            WorldObject.Box("Rear identification", new Vector3(0, .7f, -2.14f), new Vector3(1.7f, .15f, .06f), accentMat, visual);
            WorldObject.Box("Splitter", new Vector3(0, .33f, 1.99f), new Vector3(2.05f, .12f, .38f), materials.Dark, visual);
            WorldObject.Box("Left rear lamp", new Vector3(-.65f, .8f, -2.13f), new Vector3(.3f, .12f, .03f), materials.Coral, visual);
            WorldObject.Box("Right rear lamp", new Vector3(.65f, .8f, -2.13f), new Vector3(.3f, .12f, .03f), materials.Coral, visual);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, .7f, 0);
            collider.size = new Vector3(1.86f, .65f, 4.04f);
            var physicsMaterial = new PhysicsMaterial("Vehicle contact") { dynamicFriction = .12f, staticFriction = .12f, bounciness = .05f, frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum };
            collider.sharedMaterial = physicsMaterial;
            materials.Own(physicsMaterial);
            root.AddComponent<Rigidbody>();
            var car = root.AddComponent<VehicleController>();
            car.IsPlayer = player;
            var wheels = new Transform[4];
            var wheelAsset = Resources.Load<GameObject>("KenneyCars/wheel-racing");
            var trails = new TrailRenderer[4];
            var skidMat = materials.Unlit("Tyre marks", new Color(.055f, .065f, .07f, .8f));
            for (int i = 0; i < 4; i++)
            {
                var anchor = new GameObject("Animated wheel " + i).transform;
                anchor.SetParent(root.transform, false);
                wheels[i] = anchor;
                if (wheelAsset)
                {
                    var mesh = Object.Instantiate(wheelAsset, anchor);
                    mesh.transform.localScale = Vector3.one * (tuning.wheelRadius / .3f);
                    if (i % 2 == 0)
                        mesh.transform.localRotation = Quaternion.Euler(0, 180, 0);
                }
                else
                {
                    var mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    mesh.transform.SetParent(anchor, false);
                    mesh.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    mesh.transform.localScale = new Vector3(.72f, .2f, .72f);
                    mesh.GetComponent<Renderer>().sharedMaterial = materials.Tyre;
                    Object.Destroy(mesh.GetComponent<Collider>());
                }
                var trailObject = new GameObject("Skid trail " + i);
                trailObject.transform.SetParent(root.transform, false);
                trailObject.transform.localPosition = new Vector3(i % 2 == 0 ? -.98f : .98f, .045f, i < 2 ? 1.35f : -1.35f);
                var trail = trailObject.AddComponent<TrailRenderer>();
                trail.sharedMaterial = skidMat;
                trail.widthMultiplier = .23f;
                trail.time = 16;
                trail.minVertexDistance = .35f;
                trail.emitting = false;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trails[i] = trail;
            }
            car.Initialize(tuning, track, visual, wheels);
            car.SkidTrails = trails;
            root.AddComponent<VehicleAudio>();
            return car;
        }
    }
}
