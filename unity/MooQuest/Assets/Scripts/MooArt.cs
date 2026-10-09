using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Builds every model in the game from tiny flat-shaded procedural meshes, so no art assets are needed.
public static class MooArt
{
    public enum Shape { Cube, Ball, Cylinder, Cone }

    private static readonly Dictionary<Shape, Mesh> meshes = new Dictionary<Shape, Mesh>();
    private static readonly Dictionary<Color, Material> lit = new Dictionary<Color, Material>();
    private static readonly Dictionary<Color, Material> glow = new Dictionary<Color, Material>();

    public static readonly Color White = MooData.Hex("fffaf0"), Black = MooData.Hex("2f2620"), Pink = MooData.Hex("f2a7a0"),
        Skin = MooData.Hex("f7d2b6"), Gold = MooData.Hex("f2c14e"), Brown = MooData.Hex("8a5a35"), Hay = MooData.Hex("e8b64f"),
        Cheek = MooData.Hex("f59b9b"), Horn = MooData.Hex("eadbb0");

    public static Mesh Mesh(Shape shape)
    {
        if (meshes.TryGetValue(shape, out Mesh mesh) && mesh != null) return mesh;
        var tris = new List<Vector3>();
        switch (shape)
        {
            case Shape.Cube:
                Vector3[] c =
                {
                    new Vector3(-.5f, -.5f, -.5f), new Vector3(.5f, -.5f, -.5f), new Vector3(.5f, .5f, -.5f), new Vector3(-.5f, .5f, -.5f),
                    new Vector3(-.5f, -.5f, .5f), new Vector3(.5f, -.5f, .5f), new Vector3(.5f, .5f, .5f), new Vector3(-.5f, .5f, .5f),
                };
                int[][] faces = { new[] { 0, 1, 2, 3 }, new[] { 5, 4, 7, 6 }, new[] { 4, 0, 3, 7 }, new[] { 1, 5, 6, 2 }, new[] { 3, 2, 6, 7 }, new[] { 4, 5, 1, 0 } };
                foreach (int[] f in faces) Quad(tris, c[f[0]], c[f[1]], c[f[2]], c[f[3]]);
                break;
            case Shape.Ball:
                const int rings = 5, segments = 8;
                for (int r = 0; r < rings; r++)
                    for (int s = 0; s < segments; s++)
                        Quad(tris, Sphere(r, s, rings, segments), Sphere(r, s + 1, rings, segments),
                            Sphere(r + 1, s + 1, rings, segments), Sphere(r + 1, s, rings, segments));
                break;
            case Shape.Cylinder:
            case Shape.Cone:
                int sides = shape == Shape.Cone ? 7 : 8;
                float top = shape == Shape.Cone ? 0 : .5f;
                for (int i = 0; i < sides; i++)
                {
                    float a0 = i * Mathf.PI * 2 / sides, a1 = (i + 1) * Mathf.PI * 2 / sides;
                    var b0 = new Vector3(Mathf.Cos(a0) * .5f, -.5f, Mathf.Sin(a0) * .5f);
                    var b1 = new Vector3(Mathf.Cos(a1) * .5f, -.5f, Mathf.Sin(a1) * .5f);
                    var t0 = new Vector3(b0.x * top * 2, .5f, b0.z * top * 2);
                    var t1 = new Vector3(b1.x * top * 2, .5f, b1.z * top * 2);
                    if (shape == Shape.Cone) Tri(tris, b0, b1, Vector3.up * .5f);
                    else
                    {
                        Quad(tris, b0, b1, t1, t0);
                        Tri(tris, Vector3.up * .5f, t0, t1);
                    }
                    Tri(tris, Vector3.down * .5f, b0, b1);
                }
                break;
        }
        mesh = new Mesh { name = shape + " (low poly)" };
        var vertices = new Vector3[tris.Count];
        var normals = new Vector3[tris.Count];
        var indices = new int[tris.Count];
        for (int i = 0; i < tris.Count; i += 3)
        {
            Vector3 a = tris[i], b = tris[i + 1], d = tris[i + 2];
            Vector3 n = Vector3.Cross(b - a, d - a).normalized;
            // All shapes are convex around the origin, so faces pointing inward just need flipping.
            if (Vector3.Dot(n, a + b + d) < 0) { Vector3 swap = b; b = d; d = swap; n = -n; }
            vertices[i] = a; vertices[i + 1] = b; vertices[i + 2] = d;
            normals[i] = normals[i + 1] = normals[i + 2] = n;
            indices[i] = i; indices[i + 1] = i + 1; indices[i + 2] = i + 2;
        }
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = indices;
        mesh.RecalculateBounds();
        meshes[shape] = mesh;
        return mesh;
    }

    private static Vector3 Sphere(int ring, int segment, int rings, int segments)
    {
        float lat = Mathf.PI * ring / rings, lon = Mathf.PI * 2 * segment / segments;
        return new Vector3(Mathf.Sin(lat) * Mathf.Cos(lon), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Sin(lon)) * .5f;
    }

    private static void Tri(List<Vector3> tris, Vector3 a, Vector3 b, Vector3 c)
    {
        if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-8f) return;
        tris.Add(a); tris.Add(b); tris.Add(c);
    }

    private static void Quad(List<Vector3> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Tri(tris, a, b, c);
        Tri(tris, a, c, d);
    }

    public static Material Mat(Color color)
    {
        if (lit.TryGetValue(color, out Material material) && material != null) return material;
        material = new Material(Shader.Find("Legacy Shaders/Diffuse")) { color = color };
        lit[color] = material;
        return material;
    }

    public static Material Glow(Color color)
    {
        if (glow.TryGetValue(color, out Material material) && material != null) return material;
        material = new Material(Shader.Find("Unlit/Color")) { color = color };
        glow[color] = material;
        return material;
    }

    private static Material shadow;

    // Soft see-through disc used as a cheap "blob" shadow so characters feel grounded.
    public static Transform BlobShadow(Transform parent)
    {
        if (shadow == null)
            shadow = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse")) { color = new Color(0, 0, 0, .28f), renderQueue = 3000 };
        var item = new GameObject("Shadow");
        item.transform.SetParent(parent, false);
        item.AddComponent<MeshFilter>().sharedMesh = Mesh(Shape.Cylinder);
        var renderer = item.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = shadow;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return item.transform;
    }

    public static Transform Part(Transform parent, Shape shape, Vector3 position, Vector3 scale, Color color,
        Vector3 euler = default, bool glowing = false)
    {
        var item = new GameObject(shape.ToString());
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localEulerAngles = euler;
        item.transform.localScale = scale;
        item.AddComponent<MeshFilter>().sharedMesh = Mesh(shape);
        var renderer = item.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = glowing ? Glow(color) : Mat(color);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return item.transform;
    }

    public static Transform Root(string name, Transform parent = null)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        return root;
    }

    public static void Kill(Object item)
    {
        if (item == null) return;
        if (Application.isPlaying) Object.Destroy(item); else Object.DestroyImmediate(item);
    }

    // Collects static scenery and merges it into one mesh per color to keep draw calls low.
    public sealed class Batch
    {
        private readonly Dictionary<Color, List<CombineInstance>> parts = new Dictionary<Color, List<CombineInstance>>();
        private readonly Dictionary<Color, bool> glowing = new Dictionary<Color, bool>();

        public void Add(Shape shape, Vector3 position, Vector3 scale, Color color, Vector3 euler = default, bool glow = false)
        {
            if (!parts.TryGetValue(color, out var list)) parts[color] = list = new List<CombineInstance>();
            glowing[color] = glow;
            list.Add(new CombineInstance { mesh = Mesh(shape), transform = Matrix4x4.TRS(position, Quaternion.Euler(euler), scale) });
        }

        public void Finish(Transform parent)
        {
            foreach (var pair in parts)
            {
                var mesh = new Mesh { indexFormat = IndexFormat.UInt32, name = "Scenery" };
                mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                var item = new GameObject("Scenery");
                item.transform.SetParent(parent, false);
                item.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = item.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = glowing[pair.Key] ? Glow(pair.Key) : Mat(pair.Key);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            parts.Clear();
        }
    }

    public sealed class Rig
    {
        public Transform Root, Body, Feather, LeftLeg, RightLeg, LeftArm, RightArm, Crown;
    }

    public static Rig Girl(MooCharacter who, Transform parent)
    {
        var rig = new Rig { Root = Root(who.Name, parent) };
        Transform body = rig.Body = Root("Body", rig.Root);
        rig.LeftLeg = Root("Left leg", body); rig.LeftLeg.localPosition = new Vector3(-.13f, .5f, 0);
        rig.RightLeg = Root("Right leg", body); rig.RightLeg.localPosition = new Vector3(.13f, .5f, 0);
        foreach (Transform leg in new[] { rig.LeftLeg, rig.RightLeg })
        {
            Part(leg, Shape.Cylinder, new Vector3(0, -.22f, 0), new Vector3(.16f, .45f, .16f), White);
            Part(leg, Shape.Cube, new Vector3(0, -.44f, .04f), new Vector3(.2f, .12f, .28f), MooData.Hex("8a4f3a"));
        }
        // Cow-print overalls over a colorful shirt.
        Part(body, Shape.Cube, new Vector3(0, .78f, 0), new Vector3(.52f, .58f, .34f), White);
        Part(body, Shape.Cube, new Vector3(.12f, .86f, .17f), new Vector3(.16f, .14f, .03f), Black);
        Part(body, Shape.Cube, new Vector3(-.14f, .66f, .17f), new Vector3(.12f, .1f, .03f), Black);
        Part(body, Shape.Cube, new Vector3(-.06f, .9f, -.17f), new Vector3(.2f, .16f, .03f), Black);
        Part(body, Shape.Cube, new Vector3(0, 1.0f, 0), new Vector3(.5f, .14f, .32f), who.Shirt);
        rig.LeftArm = Root("Left arm", body); rig.LeftArm.localPosition = new Vector3(-.33f, 1.0f, 0);
        rig.RightArm = Root("Right arm", body); rig.RightArm.localPosition = new Vector3(.33f, 1.0f, 0);
        foreach (Transform arm in new[] { rig.LeftArm, rig.RightArm })
        {
            Part(arm, Shape.Cylinder, new Vector3(0, -.2f, 0), new Vector3(.14f, .4f, .14f), who.Shirt);
            Part(arm, Shape.Ball, new Vector3(0, -.42f, 0), Vector3.one * .14f, Skin);
        }
        rig.Feather = Root("Tickle feather", rig.RightArm);
        rig.Feather.localPosition = new Vector3(0, -.42f, .05f);
        Part(rig.Feather, Shape.Cylinder, new Vector3(0, 0, .35f), new Vector3(.03f, .35f, .03f), White, new Vector3(90, 0, 0));
        Part(rig.Feather, Shape.Ball, new Vector3(0, 0, .75f), new Vector3(.18f, .08f, .5f), MooData.Hex("ff8fc8"));
        rig.Feather.gameObject.SetActive(false);

        Part(body, Shape.Ball, new Vector3(0, 1.38f, 0), new Vector3(.6f, .58f, .56f), Skin);
        foreach (int side in new[] { -1, 1 })
        {
            Part(body, Shape.Ball, new Vector3(side * .12f, 1.4f, .24f), new Vector3(.15f, .17f, .08f), Color.white);
            Part(body, Shape.Ball, new Vector3(side * .12f, 1.39f, .275f), new Vector3(.1f, .12f, .05f), who.Eyes);
            Part(body, Shape.Ball, new Vector3(side * .12f, 1.39f, .295f), new Vector3(.045f, .055f, .02f), Black);
            Part(body, Shape.Ball, new Vector3(side * .2f, 1.3f, .23f), new Vector3(.1f, .06f, .05f), Cheek);
        }
        Part(body, Shape.Cube, new Vector3(0, 1.26f, .27f), new Vector3(.12f, .03f, .03f), MooData.Hex("c0504d"));

        Color hair = who.Hair;
        Part(body, Shape.Ball, new Vector3(0, 1.48f, -.05f), new Vector3(.66f, .54f, .62f), hair);
        Part(body, Shape.Cube, new Vector3(0, 1.6f, .2f), new Vector3(.5f, .12f, .2f), hair, new Vector3(-15, 0, 0));
        switch (who.HairStyle)
        {
            case 0: // pigtails
                foreach (int side in new[] { -1, 1 })
                {
                    Part(body, Shape.Ball, new Vector3(side * .36f, 1.36f, -.06f), Vector3.one * .24f, hair);
                    Part(body, Shape.Ball, new Vector3(side * .4f, 1.16f, -.06f), Vector3.one * .2f, hair);
                    Part(body, Shape.Ball, new Vector3(side * .33f, 1.48f, -.06f), Vector3.one * .08f, MooData.Hex("ff7aa8"));
                }
                break;
            case 1: // ponytail
                Part(body, Shape.Ball, new Vector3(0, 1.5f, -.36f), Vector3.one * .26f, hair);
                Part(body, Shape.Ball, new Vector3(0, 1.28f, -.42f), new Vector3(.22f, .28f, .22f), hair);
                Part(body, Shape.Ball, new Vector3(0, 1.06f, -.4f), Vector3.one * .16f, hair);
                Part(body, Shape.Cylinder, new Vector3(0, 1.48f, -.3f), new Vector3(.14f, .06f, .14f), MooData.Hex("ffd34d"), new Vector3(70, 0, 0));
                break;
            case 2: // curly bob
                for (int i = 0; i < 9; i++)
                {
                    float angle = Mathf.PI * (.05f + i / 8f);
                    Part(body, Shape.Ball, new Vector3(Mathf.Cos(angle) * .34f, 1.26f + (i % 2) * .05f, -Mathf.Sin(angle) * .3f + .02f),
                        Vector3.one * .22f, hair);
                }
                break;
            default: // long and wavy
                Part(body, Shape.Cube, new Vector3(0, 1.12f, -.27f), new Vector3(.56f, .7f, .16f), hair);
                foreach (int side in new[] { -1, 1 })
                    Part(body, Shape.Cube, new Vector3(side * .3f, 1.2f, -.02f), new Vector3(.1f, .5f, .32f), hair);
                Part(body, Shape.Ball, new Vector3(.22f, 1.62f, .12f), new Vector3(.14f, .1f, .08f), MooData.Hex("9ad3a0"));
                break;
        }
        // Cow-ear headband for everyone.
        Part(body, Shape.Cylinder, new Vector3(0, 1.66f, 0), new Vector3(.62f, .03f, .5f), Black);
        foreach (int side in new[] { -1, 1 })
        {
            Part(body, Shape.Ball, new Vector3(side * .38f, 1.68f, 0), new Vector3(.24f, .12f, .12f), White, new Vector3(0, 0, side * -25));
            Part(body, Shape.Ball, new Vector3(side * .4f, 1.68f, .03f), new Vector3(.14f, .07f, .07f), Pink, new Vector3(0, 0, side * -25));
            Part(body, Shape.Cone, new Vector3(side * .15f, 1.8f, 0), new Vector3(.08f, .16f, .08f), Horn);
        }
        rig.Crown = Root("Moo-nie crown", body);
        rig.Crown.localPosition = new Vector3(0, 1.84f, 0);
        Part(rig.Crown, Shape.Cylinder, Vector3.zero, new Vector3(.36f, .1f, .36f), Gold);
        for (int i = 0; i < 5; i++)
        {
            float angle = i * Mathf.PI * 2 / 5;
            Part(rig.Crown, Shape.Cone, new Vector3(Mathf.Cos(angle) * .15f, .1f, Mathf.Sin(angle) * .15f), new Vector3(.08f, .14f, .08f), Gold);
        }
        Part(rig.Crown, Shape.Ball, new Vector3(0, .06f, .17f), Vector3.one * .07f, MooData.Hex("ff6fa8"));
        rig.Crown.gameObject.SetActive(false);
        return rig;
    }

    public static Transform Cow(string name, Transform parent, Color bandana, bool bell, bool flowers = false)
    {
        var root = Root(name, parent);
        var body = Root("Body", root);
        Part(body, Shape.Cube, new Vector3(0, .85f, 0), new Vector3(.95f, .72f, 1.5f), White);
        Part(body, Shape.Cube, new Vector3(.36f, .95f, .2f), new Vector3(.26f, .4f, .5f), Black);
        Part(body, Shape.Cube, new Vector3(-.38f, .82f, -.35f), new Vector3(.22f, .34f, .44f), Black);
        Part(body, Shape.Cube, new Vector3(0, 1.22f, -.2f), new Vector3(.4f, .03f, .5f), Black);
        foreach (int x in new[] { -1, 1 })
            foreach (int z in new[] { -1, 1 })
                Part(body, Shape.Cylinder, new Vector3(x * .3f, .25f, z * .52f), new Vector3(.2f, .5f, .2f), White);
        Part(body, Shape.Ball, new Vector3(0, .5f, -.3f), new Vector3(.32f, .2f, .32f), Pink);
        Part(body, Shape.Cylinder, new Vector3(0, .8f, -.82f), new Vector3(.05f, .5f, .05f), White, new Vector3(-20, 0, 0));
        Part(body, Shape.Ball, new Vector3(0, .55f, -.92f), Vector3.one * .12f, Black);
        Part(body, Shape.Cube, new Vector3(0, 1.25f, .85f), new Vector3(.62f, .56f, .52f), White);
        Part(body, Shape.Cube, new Vector3(0, 1.08f, 1.12f), new Vector3(.52f, .28f, .14f), Pink);
        foreach (int side in new[] { -1, 1 })
        {
            Part(body, Shape.Ball, new Vector3(side * .12f, 1.08f, 1.19f), Vector3.one * .06f, Black);
            Part(body, Shape.Ball, new Vector3(side * .16f, 1.35f, 1.1f), new Vector3(.1f, .12f, .05f), Black);
            Part(body, Shape.Ball, new Vector3(side * .4f, 1.4f, .8f), new Vector3(.28f, .1f, .16f), White);
            Part(body, Shape.Cone, new Vector3(side * .2f, 1.62f, .8f), new Vector3(.1f, .22f, .1f), Horn, new Vector3(0, 0, side * -15));
        }
        Part(body, Shape.Cylinder, new Vector3(0, .98f, .7f), new Vector3(.7f, .1f, .5f), bandana);
        if (bell) Part(body, Shape.Cone, new Vector3(0, .82f, .92f), new Vector3(.2f, .2f, .2f), Gold, new Vector3(180, 0, 0));
        if (flowers)
            for (int i = 0; i < 5; i++)
                Part(body, Shape.Ball, new Vector3(-.24f + i * .12f, 1.56f, .82f), Vector3.one * .12f,
                    i % 2 == 0 ? MooData.Hex("ff8fc8") : MooData.Hex("ffe066"));
        return root;
    }

    public static Transform Critter(int kind, Transform parent)
    {
        var root = Root(MooData.CritterNames[kind], parent);
        var body = Root("Body", root);
        switch (kind)
        {
            case 0:
                Color fur = MooData.Hex("d8b48a");
                Part(body, Shape.Ball, new Vector3(0, .35f, 0), new Vector3(.62f, .55f, .7f), fur);
                Part(body, Shape.Ball, new Vector3(0, .7f, .3f), new Vector3(.46f, .44f, .44f), fur);
                foreach (int side in new[] { -1, 1 })
                {
                    Part(body, Shape.Ball, new Vector3(side * .11f, 1.05f, .25f), new Vector3(.13f, .48f, .08f), fur, new Vector3(0, 0, side * -10));
                    Part(body, Shape.Ball, new Vector3(side * .11f, 1.05f, .29f), new Vector3(.07f, .36f, .02f), Pink, new Vector3(0, 0, side * -10));
                    Part(body, Shape.Ball, new Vector3(side * .1f, .76f, .5f), Vector3.one * .08f, Black);
                }
                Part(body, Shape.Ball, new Vector3(0, .66f, .53f), Vector3.one * .07f, Pink);
                Part(body, Shape.Ball, new Vector3(0, .38f, -.38f), Vector3.one * .22f, Color.white);
                break;
            case 1:
                Color green = MooData.Hex("6fbf4a");
                Part(body, Shape.Ball, new Vector3(0, .26f, 0), new Vector3(.82f, .46f, .82f), green);
                Part(body, Shape.Ball, new Vector3(0, .22f, .05f), new Vector3(.6f, .3f, .7f), MooData.Hex("d9ef9a"));
                foreach (int side in new[] { -1, 1 })
                {
                    Part(body, Shape.Ball, new Vector3(side * .2f, .55f, .2f), Vector3.one * .26f, green);
                    Part(body, Shape.Ball, new Vector3(side * .2f, .6f, .3f), Vector3.one * .16f, Color.white);
                    Part(body, Shape.Ball, new Vector3(side * .2f, .6f, .37f), Vector3.one * .08f, Black);
                    Part(body, Shape.Ball, new Vector3(side * .38f, .06f, .1f), new Vector3(.28f, .08f, .36f), green);
                }
                Part(body, Shape.Cube, new Vector3(0, .32f, .4f), new Vector3(.4f, .03f, .04f), MooData.Hex("c0504d"));
                break;
            default:
                Color grey = MooData.Hex("8f8f99");
                Part(body, Shape.Ball, new Vector3(0, .36f, 0), new Vector3(.66f, .55f, .9f), grey);
                Part(body, Shape.Ball, new Vector3(0, .68f, .42f), new Vector3(.5f, .44f, .46f), grey);
                Part(body, Shape.Cube, new Vector3(0, .72f, .62f), new Vector3(.46f, .12f, .1f), Black);
                foreach (int side in new[] { -1, 1 })
                {
                    Part(body, Shape.Ball, new Vector3(side * .1f, .73f, .67f), Vector3.one * .07f, Color.white);
                    Part(body, Shape.Cone, new Vector3(side * .17f, .94f, .38f), new Vector3(.14f, .16f, .1f), grey);
                }
                Part(body, Shape.Ball, new Vector3(0, .6f, .68f), Vector3.one * .08f, Black);
                for (int i = 0; i < 4; i++)
                    Part(body, Shape.Cylinder, new Vector3(0, .42f + i * .1f, -.5f - i * .1f), new Vector3(.2f, .12f, .2f),
                        i % 2 == 0 ? Black : grey, new Vector3(-50, 0, 0));
                break;
        }
        return root;
    }

    public static Transform Goat(Transform parent)
    {
        var root = Root("Grumbleweed", parent);
        var body = Root("Body", root);
        Color fur = MooData.Hex("e4ddd2"), dark = MooData.Hex("8f8577");
        Part(body, Shape.Cube, new Vector3(0, 1.0f, 0), new Vector3(1.0f, .8f, 1.6f), fur);
        foreach (int x in new[] { -1, 1 })
            foreach (int z in new[] { -1, 1 })
                Part(body, Shape.Cylinder, new Vector3(x * .32f, .32f, z * .55f), new Vector3(.18f, .64f, .18f), dark);
        Part(body, Shape.Cube, new Vector3(0, 1.5f, .95f), new Vector3(.62f, .62f, .7f), fur);
        Part(body, Shape.Cone, new Vector3(0, 1.0f, 1.2f), new Vector3(.22f, .42f, .22f), MooData.Hex("cfc6b6"), new Vector3(180, 0, 0));
        foreach (int side in new[] { -1, 1 })
        {
            Part(body, Shape.Ball, new Vector3(side * .16f, 1.62f, 1.3f), new Vector3(.16f, .14f, .06f), MooData.Hex("ffe28a"));
            Part(body, Shape.Ball, new Vector3(side * .16f, 1.6f, 1.33f), new Vector3(.07f, .09f, .03f), Black);
            Part(body, Shape.Cube, new Vector3(side * .16f, 1.74f, 1.31f), new Vector3(.2f, .05f, .04f), Black, new Vector3(0, 0, side * 22));
            Part(body, Shape.Ball, new Vector3(side * .3f, 1.95f, .85f), Vector3.one * .3f, MooData.Hex("9b6b43"));
            Part(body, Shape.Ball, new Vector3(side * .42f, 1.8f, .6f), Vector3.one * .22f, MooData.Hex("9b6b43"));
            Part(body, Shape.Ball, new Vector3(side * .42f, 1.5f, .9f), new Vector3(.3f, .1f, .14f), fur);
        }
        Part(body, Shape.Cube, new Vector3(0, 1.36f, 1.31f), new Vector3(.24f, .04f, .03f), Black, new Vector3(0, 0, 180));
        Part(body, Shape.Cylinder, new Vector3(0, 1.2f, -.85f), new Vector3(.14f, .3f, .14f), fur, new Vector3(-40, 0, 0));
        root.localScale = Vector3.one * 1.35f;
        return root;
    }
}
