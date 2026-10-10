using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Builds the game's models. Heroes, cows and bunnies come from Kenney's CC0 "Mini Characters" and "Cube Pets"
// packs (Resources/Kenney); everything else is made from tiny procedural meshes. If a Kenney model is missing,
// the procedural version is used instead.
public static class MooArt
{
    // Round and Orb are smooth-shaded (a rounded box and a sphere) for the cute cube-pet critters.
    public enum Shape { Cube, Ball, Cylinder, Cone, Round, Orb }

    private static readonly Dictionary<Shape, Mesh> meshes = new Dictionary<Shape, Mesh>();
    private static readonly Dictionary<Color, Material> lit = new Dictionary<Color, Material>();
    private static readonly Dictionary<Color, Material> glow = new Dictionary<Color, Material>();

    public static readonly Color White = MooData.Hex("fffaf0"), Black = MooData.Hex("2f2620"), Pink = MooData.Hex("f2a7a0"),
        Skin = MooData.Hex("f7d2b6"), Gold = MooData.Hex("f2c14e"), Brown = MooData.Hex("8a5a35"), Hay = MooData.Hex("e8b64f"),
        Cheek = MooData.Hex("f59b9b"), Horn = MooData.Hex("eadbb0");

    public static Mesh Mesh(Shape shape)
    {
        if (meshes.TryGetValue(shape, out Mesh mesh) && mesh != null) return mesh;
        if (shape == Shape.Round || shape == Shape.Orb) return meshes[shape] = SmoothMesh(shape);
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

    // A unit box with rounded edges (Round) or a sphere (Orb), both with smooth normals. Each cube face is a grid
    // whose rows bunch up near the edges so the rounding stays smooth.
    private static Mesh SmoothMesh(Shape shape)
    {
        const float radius = .16f;
        const int steps = 4;
        var coords = new List<float>();
        for (int k = 0; k <= steps; k++) coords.Add(-.5f + radius * (1 - Mathf.Cos(k * Mathf.PI / 2 / steps)));
        for (int k = steps; k >= 0; k--) coords.Add(.5f - radius * (1 - Mathf.Cos(k * Mathf.PI / 2 / steps)));
        if (shape == Shape.Orb)
        {
            coords.Clear();
            for (int k = 0; k <= 8; k++) coords.Add(-.5f + k / 8f);
        }
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var indices = new List<int>();
        Vector3[] axes = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (Vector3 n in axes)
        {
            Vector3 u = n.y != 0 ? Vector3.right : Vector3.up, v = Vector3.Cross(n, u);
            int start = vertices.Count, count = coords.Count;
            foreach (float a in coords)
                foreach (float b in coords)
                {
                    Vector3 q = n * .5f + u * a + v * b, normal;
                    if (shape == Shape.Orb)
                    {
                        normal = q.normalized;
                        q = normal * .5f;
                    }
                    else
                    {
                        float inner = .5f - radius;
                        var core = new Vector3(Mathf.Clamp(q.x, -inner, inner), Mathf.Clamp(q.y, -inner, inner), Mathf.Clamp(q.z, -inner, inner));
                        normal = (q - core).normalized;
                        q = core + normal * radius;
                    }
                    vertices.Add(q);
                    normals.Add(normal);
                }
            for (int i = 0; i < count - 1; i++)
                for (int j = 0; j < count - 1; j++)
                {
                    int a = start + i * count + j, b = a + 1, c = a + count, d = c + 1;
                    indices.AddRange(new[] { a, c, b, b, c, d });
                }
        }
        var mesh = new Mesh { name = shape + " (smooth)" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(indices, 0);
        // Make sure every triangle faces outward.
        int[] tri = mesh.triangles;
        for (int i = 0; i < tri.Length; i += 3)
        {
            Vector3 p0 = vertices[tri[i]], p1 = vertices[tri[i + 1]], p2 = vertices[tri[i + 2]];
            if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), normals[tri[i]] + normals[tri[i + 1]] + normals[tri[i + 2]]) < 0)
            {
                int swap = tri[i + 1]; tri[i + 1] = tri[i + 2]; tri[i + 2] = swap;
            }
        }
        mesh.triangles = tri;
        mesh.RecalculateBounds();
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

    private static readonly Dictionary<string, Material> skins = new Dictionary<string, Material>();
    private static readonly string[] Loops = { "idle", "walk", "sprint", "run", "dance", "emote-yes" };

    // Loads a Kenney model from Resources/Kenney/<folder>/<file>, repaints it with the game's lit shader so it
    // matches the scenery, and starts its idle clip. Returns null if the model isn't there.
    public static Transform Model(string folder, string file, Transform parent, float scale, Material skin = null)
    {
        var prefab = Resources.Load<GameObject>("Kenney/" + folder + "/" + file);
        if (prefab == null) return null;
        if (skin == null && (!skins.TryGetValue(folder, out skin) || skin == null))
        {
            skin = new Material(Shader.Find("Legacy Shaders/Diffuse")) { mainTexture = Resources.Load<Texture2D>("Kenney/" + folder + "/colormap") };
            skins[folder] = skin;
        }
        GameObject model = Object.Instantiate(prefab, parent, false);
        model.name = file;
        model.transform.localScale = Vector3.one * scale;
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterial = skin;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
        }
        var anim = model.GetComponent<Animation>();
        if (anim != null)
        {
            anim.cullingType = AnimationCullingType.AlwaysAnimate;
            foreach (AnimationState state in anim)
                state.wrapMode = System.Array.IndexOf(Loops, state.name) >= 0 ? WrapMode.Loop : WrapMode.ClampForever;
            PlayClip(anim, "idle", 0);
        }
        return model.transform;
    }

    // A copy of a colormap with some swatch columns of its bottom row (where Kenney keeps hair and skin tones)
    // repainted, keeping each swatch's light-to-dark shading.
    private static Material Repaint(string folder, int[] swatches, Color color)
    {
        if (swatches == null || swatches.Length == 0) return null;
        string key = folder + "/" + string.Join(",", swatches) + "/" + ColorUtility.ToHtmlStringRGB(color);
        if (skins.TryGetValue(key, out Material skin) && skin != null) return skin;
        var source = Resources.Load<Texture2D>("Kenney/" + folder + "/colormap");
        if (source == null || !source.isReadable) return null;
        var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = key
        };
        texture.SetPixels32(source.GetPixels32());
        int width = source.width / 16, height = source.height / 4;
        foreach (int swatch in swatches)
        {
            Color[] block = texture.GetPixels(swatch * width, 0, width, height);
            float mean = 0;
            foreach (Color c in block) mean += c.grayscale;
            mean = Mathf.Max(mean / block.Length, .01f);
            for (int i = 0; i < block.Length; i++)
            {
                Color c = color * Mathf.Clamp(block[i].grayscale / mean, .6f, 1.4f);
                block[i] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1);
            }
            texture.SetPixels(swatch * width, 0, width, height, block);
        }
        texture.Apply(false, true);
        skin = new Material(Shader.Find("Legacy Shaders/Diffuse")) { mainTexture = texture };
        skins[key] = skin;
        return skin;
    }

    // Cross-fades to a clip unless it's already the one playing.
    public static void PlayClip(Animation anim, string clip, float fade = .15f)
    {
        if (anim == null || anim[clip] == null || anim.IsPlaying(clip)) return;
        if (fade <= 0) anim.Play(clip);
        else anim.CrossFade(clip, fade);
    }

    // Plays a clip on the first animated model under a spawned character, if it has one.
    public static void PlayClip(Transform character, string clip)
    {
        if (character != null) PlayClip(character.GetComponentInChildren<Animation>(), clip);
    }

    public static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // Builds a group of parts in the owner's own space, then pins it to a bone so it follows the animation.
    private static Transform Attach(string name, Transform owner, Transform bone, Vector3 at = default)
    {
        Transform group = Root(name, owner);
        group.localPosition = at;
        if (bone != null) group.SetParent(bone, true);
        return group;
    }

    public sealed class Rig
    {
        public Transform Root, Body, Feather, LeftLeg, RightLeg, LeftArm, RightArm, Crown;
        // Set when the hero is a Kenney model; its clips replace the hand-made limb swings.
        public Animation Anim;

        public void Play(string clip, float fade = .15f) => PlayClip(Anim, clip, fade);

        public void Restart(string clip)
        {
            if (Anim == null || Anim[clip] == null) return;
            Anim[clip].time = 0;
            Anim.CrossFade(clip, .05f);
        }
    }

    // Kenney's mini characters are .72 units tall; this makes them about 1.7.
    private const float GirlScale = 2.35f;

    public static Rig Girl(MooCharacter who, Transform parent)
    {
        var rig = new Rig { Root = Root(who.Name, parent) };
        rig.Body = Root("Body", rig.Root);
        Transform model = string.IsNullOrEmpty(who.Model) ? null
            : Model("Characters", who.Model, rig.Body, GirlScale, Repaint("Characters", who.HairSwatches, who.Hair));
        if (model == null) GirlParts(who, rig);
        else
        {
            rig.Anim = model.GetComponent<Animation>();
            CowGirlExtras(rig, FindDeep(model, "head"), FindDeep(model, "arm-right"));
        }
        return rig;
    }

    // The cow-ear headband, tickle feather and crown, pinned to a Kenney model's bones.
    private static void CowGirlExtras(Rig rig, Transform head, Transform hand)
    {
        Transform body = rig.Body;
        Transform band = Attach("Cow-ear headband", body, head);
        Part(band, Shape.Round, new Vector3(0, 1.57f, -.08f), new Vector3(1.12f, .07f, .15f), Black);
        foreach (int side in new[] { -1, 1 })
        {
            Part(band, Shape.Round, new Vector3(side * .56f, 1.47f, -.08f), new Vector3(.07f, .24f, .15f), Black);
            Part(band, Shape.Orb, new Vector3(side * .72f, 1.4f, -.08f), new Vector3(.38f, .2f, .22f), White, new Vector3(0, 0, side * -30));
            Part(band, Shape.Orb, new Vector3(side * .75f, 1.39f, -.01f), new Vector3(.24f, .11f, .12f), Pink, new Vector3(0, 0, side * -30));
            Part(band, Shape.Cone, new Vector3(side * .3f, 1.66f, -.08f), new Vector3(.13f, .17f, .13f), Horn, new Vector3(0, 0, side * -20));
        }

        // Kenney models are built in a T-pose, so the feather sticks out past the right hand along the arm.
        rig.Feather = Attach("Tickle feather", body, hand, new Vector3(.82f, .68f, .04f));
        Part(rig.Feather, Shape.Cylinder, new Vector3(.24f, 0, 0), new Vector3(.05f, .3f, .05f), White, new Vector3(0, 0, 90));
        Part(rig.Feather, Shape.Orb, new Vector3(.62f, 0, 0), new Vector3(.55f, .14f, .26f), MooData.Hex("ff8fc8"));
        rig.Feather.gameObject.SetActive(false);

        rig.Crown = Attach("Moo-nie crown", body, head);
        Transform crown = Root("Crown", rig.Crown);
        crown.localPosition = new Vector3(0, 1.66f, -.04f);
        Part(crown, Shape.Cylinder, Vector3.zero, new Vector3(.42f, .12f, .42f), Gold);
        for (int i = 0; i < 5; i++)
        {
            float angle = i * Mathf.PI * 2 / 5;
            Part(crown, Shape.Cone, new Vector3(Mathf.Cos(angle) * .17f, .12f, Mathf.Sin(angle) * .17f), new Vector3(.1f, .16f, .1f), Gold);
        }
        Part(crown, Shape.Orb, new Vector3(0, .06f, .2f), Vector3.one * .08f, MooData.Hex("ff6fa8"));
        rig.Crown.gameObject.SetActive(false);
    }

    // The original hand-built hero, used if the Kenney model can't be loaded.
    private static void GirlParts(MooCharacter who, Rig rig)
    {
        Transform body = rig.Body;
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
    }

    // Kenney's cube pets are about 1.6 units tall.
    private const float CowScale = 1.05f, CritterScale = .62f, GoatScale = .72f;

    public static Transform Cow(string name, Transform parent, Color bandana, bool bell, bool flowers = false)
    {
        var root = Root(name, parent);
        var body = Root("Body", root);
        Transform model = Model("Pets", "animal-cow", body, CowScale);
        if (model == null)
        {
            CowParts(body, bandana, bell, flowers);
            return root;
        }
        // A kerchief, bell and flower crown on the cow's body, in its own units, so they move with its clips.
        Transform cow = FindDeep(model, "body") ?? model;
        Part(cow, Shape.Round, new Vector3(0, .5f, 0), new Vector3(1.32f, .14f, 1.32f), bandana);
        if (bell)
        {
            Part(cow, Shape.Cone, new Vector3(0, .34f, .7f), new Vector3(.26f, .24f, .26f), Gold, new Vector3(180, 0, 0));
            Part(cow, Shape.Orb, new Vector3(0, .21f, .7f), Vector3.one * .09f, Gold);
        }
        else Part(cow, Shape.Cone, new Vector3(0, .34f, .67f), new Vector3(.44f, .22f, .04f), bandana, new Vector3(0, 0, 180));
        if (flowers)
            for (int i = 0; i < 5; i++)
                Part(cow, Shape.Orb, new Vector3(-.4f + i * .2f, 1.46f, .35f), Vector3.one * .18f,
                    i % 2 == 0 ? MooData.Hex("ff8fc8") : MooData.Hex("ffe066"));
        return root;
    }

    // The original hand-built cow, used if the Kenney model can't be loaded.
    private static void CowParts(Transform body, Color bandana, bool bell, bool flowers)
    {
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
    }

    public static Transform Critter(int kind, Transform parent)
    {
        var root = Root(MooData.CritterNames[kind], parent);
        var body = Root("Body", root);
        if (kind == 0)
        {
            if (Model("Pets", "animal-bunny", body, CritterScale) == null) BunnyParts(body);
        }
        else if (kind == 1) Frog(body);
        else Raccoon(body);
        return root;
    }

    // Kenney's Cube Pets has no frog, raccoon or goat, so those are built here in the same style: a big rounded
    // block on four stubby legs with a big-eyed face on the front. Sizes are in cube-pet units (about 1.5 across).
    private static Transform PetBlock(Transform parent, float scale, Vector3 size, Color fur, Color feet)
    {
        Transform pet = Root("Pet", parent);
        pet.localScale = Vector3.one * scale;
        foreach (int x in new[] { -1, 1 })
            foreach (int z in new[] { -1, 1 })
                Part(pet, Shape.Round, new Vector3(x * size.x * .19f, .16f, z * size.z * .19f), new Vector3(.38f, .32f, .38f), feet);
        Part(pet, Shape.Round, new Vector3(0, .18f + size.y / 2, 0), size, fur);
        return pet;
    }

    // Big shiny cube-pet eyes on a face at depth z.
    private static void PetEyes(Transform pet, float y, float z, float apart, Color white, Color pupil, float size = 1)
    {
        foreach (int side in new[] { -1, 1 })
        {
            float x = side * apart;
            Part(pet, Shape.Orb, new Vector3(x, y, z), new Vector3(.32f, .36f, .08f) * size, white);
            Part(pet, Shape.Orb, new Vector3(x + side * .02f, y - .02f, z + .03f), new Vector3(.21f, .25f, .06f) * size, pupil);
            Part(pet, Shape.Orb, new Vector3(x + .04f, y + .06f, z + .06f), new Vector3(.08f, .08f, .03f) * size, Color.white);
        }
    }

    private static void Frog(Transform body)
    {
        Color green = MooData.Hex("6fbf4a"), dark = MooData.Hex("559c37");
        Transform pet = PetBlock(body, CritterScale, new Vector3(1.5f, 1.0f, 1.4f), green, dark);
        float front = .7f;
        foreach (int side in new[] { -1, 1 })
            Part(pet, Shape.Round, new Vector3(side * .4f, 1.22f, .3f), new Vector3(.5f, .44f, .46f), green);
        PetEyes(pet, 1.26f, .55f, .4f, Color.white, Black);
        Part(pet, Shape.Round, new Vector3(0, .5f, front), new Vector3(1.0f, .5f, .06f), MooData.Hex("d9ef9a"));
        Part(pet, Shape.Round, new Vector3(0, .86f, front + .01f), new Vector3(.62f, .05f, .04f), Black);
        foreach (int side in new[] { -1, 1 })
            Part(pet, Shape.Orb, new Vector3(side * .52f, .86f, front + .01f), new Vector3(.2f, .11f, .04f), Cheek);
    }

    private static void Raccoon(Transform body)
    {
        Color grey = MooData.Hex("8f8f99"), dark = MooData.Hex("5d5d68"), light = MooData.Hex("dcdce4");
        Transform pet = PetBlock(body, CritterScale, new Vector3(1.4f, 1.35f, 1.4f), grey, dark);
        float front = .7f;
        Part(pet, Shape.Round, new Vector3(0, 1.08f, front), new Vector3(1.24f, .38f, .05f), Black);
        PetEyes(pet, 1.08f, front + .02f, .32f, Color.white, Black);
        Part(pet, Shape.Round, new Vector3(0, .7f, front + .08f), new Vector3(.56f, .32f, .24f), light);
        Part(pet, Shape.Orb, new Vector3(0, .8f, front + .2f), new Vector3(.18f, .13f, .1f), Black);
        foreach (int side in new[] { -1, 1 })
        {
            Part(pet, Shape.Round, new Vector3(side * .46f, 1.6f, 0), new Vector3(.36f, .32f, .18f), grey);
            Part(pet, Shape.Round, new Vector3(side * .46f, 1.58f, .09f), new Vector3(.2f, .18f, .04f), dark);
        }
        for (int i = 0; i < 4; i++)
            Part(pet, Shape.Round, new Vector3(0, .5f + i * .22f, -.78f - i * .1f), new Vector3(.38f, .26f, .32f) * (1 - i * .06f),
                i % 2 == 0 ? Black : grey, new Vector3(-30, 0, 0));
    }

    // Grumbleweed the grumpy goat, as a big cube pet with curly horns, a beard and very cross eyebrows.
    public static Transform Goat(Transform parent)
    {
        var root = Root("Grumbleweed", parent);
        var body = Root("Body", root);
        Color fur = MooData.Hex("e4ddd2"), dark = MooData.Hex("8f8577"), snout = MooData.Hex("cfc6b6"), horn = MooData.Hex("9b6b43");
        Transform pet = PetBlock(body, GoatScale, new Vector3(1.5f, 1.45f, 1.5f), fur, dark);
        float front = .75f;
        PetEyes(pet, 1.14f, front, .34f, MooData.Hex("ffe28a"), Black, .95f);
        foreach (int side in new[] { -1, 1 })
        {
            // Goats have sideways pupils, and these brows mean business.
            Part(pet, Shape.Round, new Vector3(side * .34f, 1.12f, front + .09f), new Vector3(.2f, .07f, .03f), Black);
            Part(pet, Shape.Round, new Vector3(side * .32f, 1.38f, front + .02f), new Vector3(.38f, .08f, .06f), Black, new Vector3(0, 0, side * 22));
            // Horns that sweep up and back.
            Part(pet, Shape.Round, new Vector3(side * .4f, 1.78f, -.05f), new Vector3(.24f, .3f, .24f), horn, new Vector3(-20, 0, side * -10));
            Part(pet, Shape.Cone, new Vector3(side * .44f, 2.0f, -.22f), new Vector3(.2f, .38f, .2f), horn, new Vector3(-50, 0, side * -10));
            Part(pet, Shape.Round, new Vector3(side * .88f, 1.2f, .3f), new Vector3(.42f, .14f, .24f), fur, new Vector3(0, 0, side * -20));
            Part(pet, Shape.Orb, new Vector3(side * .1f, .72f, front + .21f), new Vector3(.08f, .06f, .04f), Black);
        }
        Part(pet, Shape.Round, new Vector3(0, .64f, front + .06f), new Vector3(.66f, .4f, .3f), snout);
        Part(pet, Shape.Round, new Vector3(0, .53f, front + .22f), new Vector3(.28f, .04f, .03f), Black);
        Part(pet, Shape.Cone, new Vector3(0, .26f, front + .1f), new Vector3(.3f, .36f, .22f), snout, new Vector3(0, 0, 180));
        Part(pet, Shape.Round, new Vector3(0, 1.3f, -.82f), new Vector3(.22f, .3f, .16f), fur, new Vector3(-30, 0, 0));
        root.localScale = Vector3.one * 1.35f;
        return root;
    }

    // The original hand-built bunny, used if the Kenney model can't be loaded.
    private static void BunnyParts(Transform body)
    {
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
    }
}
