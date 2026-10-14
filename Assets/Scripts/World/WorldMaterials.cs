using System.Collections.Generic;
using UnityEngine;
namespace Coastline
{
    public sealed class WorldMaterials : MonoBehaviour
    {
        readonly List<Object> owned = new List<Object>();
        public Material Asphalt, Grass, Sand, Dark, White, Coral, Teal, Water, Metal, Glass, Tyre;
        public void Initialize(bool wet)
        {
            Asphalt = Make("Asphalt", new Color(.23f, .26f, .28f), wet ? .7f : .13f);
            Grass = Make("Coastal grass", new Color(.34f, .44f, .24f));
            Sand = Make("Warm sand", new Color(.81f, .71f, .47f));
            Dark = Make("Graphite", new Color(.075f, .115f, .14f));
            White = Make("Warm white", new Color(.93f, .92f, .82f));
            Coral = Make("Racing coral", new Color(.93f, .28f, .19f));
            Teal = Make("Mint", new Color(.2f, .94f, .72f));
            Metal = Make("Aluminium", new Color(.55f, .62f, .6f), .55f);
            Glass = Make("Glass", new Color(.08f, .19f, .23f), .8f);
            Tyre = Make("Tyre", new Color(.035f, .04f, .045f));
            Water = Make("Ocean", new Color(.09f, .4f, .47f), .75f);
            var texture = new Texture2D(128, 128, TextureFormat.RGB24, false);
            texture.name = "Procedural asphalt grain";
            var pixels = new Color[128 * 128];
            var random = new System.Random(421);
            for (int i = 0; i < pixels.Length; i++)
            {
                float v = .8f + (float)random.NextDouble() * .4f;
                pixels[i] = new Color(v, v, v);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Repeat;
            Asphalt.mainTexture = texture;
            Asphalt.mainTextureScale = new Vector2(4, 40);
            owned.Add(texture);
        }
        public Material Make(string name, Color color, float gloss = .2f)
        {
            var mat = new Material(Resources.Load<Material>("SurfaceReference")) { name = name, color = color, enableInstancing = true };
            mat.SetFloat("_Glossiness", gloss);
            owned.Add(mat);
            return mat;
        }
        public Material Unlit(string name, Color color)
        {
            var mat = new Material(Resources.Load<Shader>("CoastalUnlit")) { name = name, color = color };
            owned.Add(mat);
            return mat;
        }
        public void Own(Object asset)
        {
            owned.Add(asset);
        }
        void OnDestroy()
        {
            foreach (var asset in owned)
                if (asset)
                    Destroy(asset);
        }
    }
    public static class WorldObject
    {
        public static GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider)
                Object.Destroy(go.GetComponent<Collider>());
            return go;
        }
        public static Bounds BoundsOf(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            var b = new Bounds(obj.transform.position, Vector3.zero);
            bool first = true;
            foreach (var r in renderers)
            {
                if (!r.enabled)
                    continue;
                if (first)
                {
                    b = r.bounds;
                    first = false;
                }
                else
                    b.Encapsulate(r.bounds);
            }
            return b;
        }
        public static GameObject Model(string resource, Vector3 position, float scale, Quaternion rotation, Transform parent)
        {
            var asset = Resources.Load<GameObject>(resource);
            if (!asset)
                return null;
            var wrapper = new GameObject(asset.name);
            wrapper.transform.SetParent(parent, false);
            var model = Object.Instantiate(asset, wrapper.transform);
            model.name = "Art";
            var bounds = BoundsOf(model);
            model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            wrapper.transform.localScale = Vector3.one * scale;
            wrapper.transform.localPosition = position;
            wrapper.transform.localRotation = rotation;
            return wrapper;
        }
        public static void Label(string text, Vector3 pos, Quaternion rotation, float size, Color color, Transform parent)
        {
            var go = new GameObject("Sign: " + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rotation;
            var label = go.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = size;
            label.fontSize = 64;
            label.color = color;
        }
    }
}
