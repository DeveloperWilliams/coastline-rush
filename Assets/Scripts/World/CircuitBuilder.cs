using UnityEngine;
namespace Coastline
{
    public sealed class CircuitBuilder : MonoBehaviour
    {
        public TrackPath Track
        {
            get; private set;
        }
        public WorldMaterials Materials
        {
            get; private set;
        }
        public Transform Props
        {
            get; private set;
        }
        public GameObject RacingLine
        {
            get; private set;
        }
        public void Build(TrackPath track, bool wet)
        {
            Track = track;
            Materials = gameObject.AddComponent<WorldMaterials>();
            Materials.Initialize(wet);
            Props = new GameObject("Trackside / Kenney CC0").transform;
            Props.SetParent(transform, false);
            ConfigureEnvironment(wet);
            Ground();
            Ribbon("Smooth tarmac", -track.HalfWidth, track.HalfWidth, Materials.Asphalt, .03f, true);
            Ribbon("Left runoff", -track.HalfWidth - 2, -track.HalfWidth, Materials.Sand, .015f, true);
            Ribbon("Right runoff", track.HalfWidth, track.HalfWidth + 2, Materials.Sand, .015f, true);
            Ribbon("White edge left", -track.HalfWidth + .2f, -track.HalfWidth + .35f, Materials.White, .045f, false);
            Ribbon("White edge right", track.HalfWidth - .35f, track.HalfWidth - .2f, Materials.White, .045f, false);
            CurbsAndBarriers();
            StartFinish();
            Scenery();
            RacingGuide();
            Physics.SyncTransforms();
        }
        void ConfigureEnvironment(bool wet)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.65f, .72f, .76f);
            RenderSettings.ambientEquatorColor = new Color(.48f, .51f, .49f);
            RenderSettings.ambientGroundColor = new Color(.3f, .3f, .25f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = wet ? new Color(.55f, .64f, .66f) : new Color(.78f, .7f, .54f);
            RenderSettings.fogDensity = .0017f;
            var sky = new Material(Shader.Find("Coastline/Coastal Sky"));
            sky.SetColor("_Top", wet ? new Color(.22f, .34f, .42f) : new Color(.12f, .38f, .52f));
            sky.SetColor("_Horizon", wet ? new Color(.72f, .76f, .75f) : new Color(.97f, .77f, .52f));
            RenderSettings.skybox = sky;
            Materials.Own(sky);
            var sun = new GameObject("Golden hour sun").AddComponent<Light>();
            sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(32, -34, 0);
            sun.color = wet ? new Color(.82f, .89f, .96f) : new Color(1, .83f, .65f);
            sun.intensity = wet ? 1.15f : 1.55f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .62f;
            sun.shadowBias = .08f;
            QualitySettings.shadowDistance = 120;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.antiAliasing = 4;
        }
        void Ribbon(string name, float left, float right, Material material, float y, bool collision)
        {
            int n = Track.Count;
            var vertices = new Vector3[(n + 1) * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[n * 6];
            for (int i = 0; i <= n; i++)
            {
                int k = i % n;
                vertices[i * 2] = Track.Points[k] + Track.Right(k) * left + Vector3.up * y;
                vertices[i * 2 + 1] = Track.Points[k] + Track.Right(k) * right + Vector3.up * y;
                uv[i * 2] = new Vector2(0, Track.Distances[i] / 20);
                uv[i * 2 + 1] = new Vector2(1, Track.Distances[i] / 20);
                if (i < n)
                {
                    int v = i * 2, t = i * 6;
                    triangles[t] = v;
                    triangles[t + 1] = v + 2;
                    triangles[t + 2] = v + 1;
                    triangles[t + 3] = v + 1;
                    triangles[t + 4] = v + 2;
                    triangles[t + 5] = v + 3;
                }
            }
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Materials.Own(mesh);
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collision)
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
        public float GroundHeight(Vector3 p)
        {
            var location = Track.Locate(p);
            float blend = Mathf.Clamp01((location.Offset - Track.HalfWidth - 2) / 45);
            float height = Mathf.Lerp(location.Point.y - .16f, -.55f, blend);
            return height;
        }
        void Ground()
        {
            WorldObject.Box("Ocean", new Vector3(0, -3.2f, 0), new Vector3(1800, 1, 1800), Materials.Water, transform);
            const int n = 84;
            var vertices = new Vector3[(n + 1) * (n + 1)];
            var colors = new Color[vertices.Length];
            var triangles = new int[n * n * 6];
            for (int z = 0; z <= n; z++)
                for (int x = 0; x <= n; x++)
                {
                    int i = z * (n + 1) + x;
                    Vector3 p = new Vector3(-230 + x * 460f / n, 0, -210 + z * 460f / n);
                    float coast = Mathf.Sqrt(p.x * p.x / (220 * 220) + (p.z - 20) * (p.z - 20) / (225 * 225));
                    p.y = GroundHeight(p);
                    if (coast > .9f)
                        p.y = Mathf.Lerp(p.y, -3.5f, Mathf.Clamp01((coast - .9f) / .2f));
                    vertices[i] = p;
                    colors[i] = Color.white;
                    if (x < n && z < n)
                    {
                        int t = (z * n + x) * 6;
                        triangles[t] = i;
                        triangles[t + 1] = i + n + 1;
                        triangles[t + 2] = i + 1;
                        triangles[t + 3] = i + 1;
                        triangles[t + 4] = i + n + 1;
                        triangles[t + 5] = i + n + 2;
                    }
                }
            var mesh = new Mesh { name = "Coastal terrain" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            Materials.Own(mesh);
            var ground = new GameObject("Island / physics terrain");
            ground.transform.SetParent(transform, false);
            ground.AddComponent<MeshFilter>().sharedMesh = mesh;
            ground.AddComponent<MeshRenderer>().sharedMaterial = Materials.Grass;
            ground.AddComponent<MeshCollider>().sharedMesh = mesh;
            var beach = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beach.name = "Sand shelf";
            beach.transform.SetParent(transform, false);
            beach.transform.localPosition = new Vector3(0, -2.2f, 20);
            beach.transform.localScale = new Vector3(463, .5f, 472);
            beach.GetComponent<Renderer>().sharedMaterial = Materials.Sand;
            Destroy(beach.GetComponent<Collider>());
        }
        void CurbsAndBarriers()
        {
            for (float d = 0; d < Track.Length; d += 3)
            {
                Quaternion q = Track.Rotation(d);
                int stripe = (int)(d / 3) % 2;
                for (int side = -1; side <= 1; side += 2)
                {
                    var curb = WorldObject.Box("Rumble strip", Track.Position(d, side * (Track.HalfWidth + .45f)) + Vector3.up * .075f, new Vector3(.9f, .12f, 3.1f), stripe == 0 ? Materials.Coral : Materials.White, transform);
                    curb.transform.localRotation = q;
                }
            }
            for (float d = 0; d < Track.Length; d += 5)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 a = Track.Position(d, side * (Track.HalfWidth + 3)), b = Track.Position(d + 5, side * (Track.HalfWidth + 3));
                    var wall = WorldObject.Box("Armco collision rail", (a + b) * .5f + Vector3.up * .6f, new Vector3(.35f, 1.2f, Vector3.Distance(a, b) + .4f), Materials.Metal, transform, true);
                    wall.transform.rotation = Quaternion.LookRotation(b - a);
                    WorldObject.Box("Rail support", a + Vector3.up * .35f, new Vector3(.15f, .9f, .15f), Materials.Dark, transform);
                }
            }
            for (float d = 70; d < Track.Length; d += 110)
            {
                float curvature = Track.Curvature(d + 35);
                if (curvature < .01f)
                    continue;
                int side = Vector3.SignedAngle(Track.Direction(d + 20), Track.Direction(d + 50), Vector3.up) > 0 ? -1 : 1;
                var board = WorldObject.Box("Corner chevron", Track.Position(d + 35, side * (Track.HalfWidth + 4.5f)) + Vector3.up * 1.6f, new Vector3(3, 1.6f, .15f), Materials.Dark, Props);
                board.transform.rotation = Track.Rotation(d + 35);
                WorldObject.Label(side > 0 ? "<<<" : ">>>", new Vector3(0, 0, -.09f), Quaternion.identity, .08f, new Color(1, .82f, .3f), board.transform);
                WorldObject.Label("100", Track.Position(d - 45, side * (Track.HalfWidth + 4)) + Vector3.up * 1.6f, Track.Rotation(d - 45), .075f, Color.white, Props);
                WorldObject.Label("50", Track.Position(d - 5, side * (Track.HalfWidth + 4)) + Vector3.up * 1.6f, Track.Rotation(d - 5), .075f, Color.white, Props);
            }
        }
        void StartFinish()
        {
            var root = new GameObject("Start / finish gantry").transform;
            root.SetParent(transform, false);
            root.position = Track.Position(0);
            root.rotation = Track.Rotation(0);
            for (int i = 0; i < 17; i++)
                for (int j = 0; j < 4; j++)
                    WorldObject.Box("Finish checks", new Vector3(i - 8, j * .0f + .07f, j - 1.5f), new Vector3(1, .025f, 1), (i + j) % 2 == 0 ? Materials.White : Materials.Dark, root);
            WorldObject.Box("Left pillar", new Vector3(-10.4f, 3.7f, 0), new Vector3(.65f, 7.4f, .65f), Materials.Dark, root, true);
            WorldObject.Box("Right pillar", new Vector3(10.4f, 3.7f, 0), new Vector3(.65f, 7.4f, .65f), Materials.Dark, root, true);
            WorldObject.Box("Header", new Vector3(0, 7.1f, 0), new Vector3(21.5f, 1.3f, .6f), Materials.Dark, root);
            WorldObject.Label("COASTLINE  /  RUSH", new Vector3(0, 7.1f, -.32f), Quaternion.identity, .075f, new Color(.35f, 1, .8f), root);
            for (int i = 0; i < 5; i++)
            {
                var lamp = WorldObject.Box("Starting light", new Vector3((i - 2) * .8f, 6.25f, -.35f), new Vector3(.35f, .35f, .18f), Materials.Coral, root);
            }
            for (int i = 0; i < 6; i++)
            {
                float d = -8 - i * 6;
                Vector3 p = Track.Position(d, i % 2 == 0 ? -2.8f : 2.8f);
                var mark = WorldObject.Box("Grid marking", p + Vector3.up * .05f, new Vector3(2.5f, .025f, .1f), Materials.White, Props);
                mark.transform.rotation = Track.Rotation(d);
            }
            for (int i = 0; i < 6; i++)
            {
                float d = 10 + i * 15;
                var garage = WorldObject.Model("KenneyRacing/pitsGarage", Track.Position(d, -24), 1.2f, Track.Rotation(d) * Quaternion.Euler(0, 90, 0), Props);
                var stand = WorldObject.Model("KenneyRacing/grandStandCovered", Track.Position(d, 29), 1.35f, Track.Rotation(d) * Quaternion.Euler(0, -90, 0), Props);
            }
            // Downloaded modular road tiles form the service road and pit apron.
            for (int i = 0; i < 8; i++)
                WorldObject.Model("KenneyRacing/roadStraight", Track.Position(10 + i * 12, -16) + Vector3.up * .01f, 1.2f, Track.Rotation(0) * Quaternion.Euler(0, 90, 0), Props);
            WorldObject.Model("KenneyRacing/pitsOfficeRoof", Track.Position(105, -24), 1.5f, Track.Rotation(0) * Quaternion.Euler(0, 90, 0), Props);
        }
        void Scenery()
        {
            var random = new System.Random(72);
            string[] palms = { "tree_palmDetailedTall", "tree_palmDetailedShort", "tree_palmBend", "tree_palmTall" };
            for (int i = 0; i < 160; i++)
            {
                Vector3 p = new Vector3(-205 + (float)random.NextDouble() * 410, 0, -185 + (float)random.NextDouble() * 400);
                var location = Track.Locate(p);
                if (location.Offset < Track.HalfWidth + 15 || p.magnitude > 215)
                    continue;
                p.y = GroundHeight(p);
                WorldObject.Model("KenneyNature/" + palms[i % 4], p, 5 + (float)random.NextDouble() * 4, Quaternion.Euler(0, (float)random.NextDouble() * 360, 0), Props);
            }
            for (int i = 0; i < 65; i++)
            {
                float angle = i * Mathf.PI * 2 / 65;
                Vector3 p = new Vector3(Mathf.Cos(angle) * 217, -1.3f, 20 + Mathf.Sin(angle) * 221);
                WorldObject.Model("KenneyNature/rock_large" + (char)('A' + i % 6), p, 8 + i % 5, Quaternion.Euler(0, i * 47, 0), Props);
            }
            for (float d = 0; d < Track.Length; d += 100)
            {
                WorldObject.Model("KenneyRacing/flagCheckers", Track.Position(d, Track.HalfWidth + 5), .8f, Track.Rotation(d), Props);
                WorldObject.Model("KenneyRacing/bannerTowerRed", Track.Position(d + 10, -Track.HalfWidth - 6), .65f, Track.Rotation(d), Props);
            }
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2 / 16;
                var mountain = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mountain.name = "Distant island";
                mountain.transform.SetParent(Props, false);
                mountain.transform.localPosition = new Vector3(Mathf.Cos(a) * 600, -35, Mathf.Sin(a) * 600);
                mountain.transform.localScale = new Vector3(150, 80 + i % 4 * 25, 150);
                mountain.GetComponent<Renderer>().sharedMaterial = Materials.Make("Haze island", new Color(.3f, .43f, .44f));
                Destroy(mountain.GetComponent<Collider>());
            }
            var lighthouse = new GameObject("Lighthouse").transform;
            lighthouse.SetParent(Props, false);
            lighthouse.position = new Vector3(-190, 0, 155);
            WorldObject.Box("Tower", new Vector3(0, 7, 0), new Vector3(4, 14, 4), Materials.White, lighthouse);
            WorldObject.Box("Stripe", new Vector3(0, 9, 0), new Vector3(4.1f, 2, 4.1f), Materials.Coral, lighthouse);
            WorldObject.Box("Lantern", new Vector3(0, 15, 0), new Vector3(5, 2, 5), Materials.Glass, lighthouse);
            WorldObject.Box("Roof", new Vector3(0, 16.4f, 0), new Vector3(6, .7f, 6), Materials.Dark, lighthouse);
            for (int i = 0; i < 10; i++)
            {
                Vector3 p = new Vector3(265 + i * 12, -2.4f, -140 + i * 25);
                WorldObject.Box("Distant sailboat hull", p, new Vector3(3, .7f, 8), Materials.White, Props);
                WorldObject.Box("Sailboat mast", p + Vector3.up * 5, new Vector3(.15f, 10, .15f), Materials.Metal, Props);
                WorldObject.Box("Sail", p + new Vector3(1.3f, 5, 0), new Vector3(2.6f, 7, .05f), Materials.White, Props);
            }
        }
        void RacingGuide()
        {
            RacingLine = new GameObject("Driving guide");
            RacingLine.transform.SetParent(transform, false);
            for (float d = 4; d < Track.Length; d += 9)
            {
                float curvature = Track.Curvature(d + 25);
                Material mat = curvature > .018f ? Materials.Coral : curvature > .01f ? Materials.White : Materials.Teal;
                var marker = WorldObject.Box("Suggested line", Track.Position(d) + Vector3.up * .055f, new Vector3(.25f, .016f, 2), mat, RacingLine.transform);
                marker.transform.rotation = Track.Rotation(d);
            }
        }
    }
}
