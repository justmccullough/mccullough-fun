using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using Shape = MooArt.Shape;

public sealed partial class MooQuest
{
    public const float Tile = 2f, PlayerRadius = .5f, CritterRadius = .45f, BossRadius = .9f;
    public const int BossHp = 20;

    public sealed class Critter
    {
        public Vector2 pos, home, wander, knock, face = Vector2.down;
        public int kind, hp;
        public float sleep, stun, hurt, wanderTimer, happy, phase, snore;
        public bool giggling;
        public Transform model;
    }

    public sealed class Projectile
    {
        public Vector2 pos, vel;
        public float life;
        public bool friendly;
        public Transform model;
    }

    public sealed class Boss
    {
        public Vector2 pos, dir = Vector2.down;
        public int hp = BossHp;
        public string mode = "idle";
        public float timer = 2f, hurt, dizzyText;
        public bool active;
        public Transform model;
    }

    public sealed class Npc
    {
        public string kind, name;
        public Vector2Int tile;
        public int talks;
        public Transform model;
    }

    private sealed class Drop { public Vector2 pos; public int kind; public Transform model; }
    private sealed class Bob { public Transform t; public Vector3 home; public float phase, spin, height; }
    private sealed class Effect { public Transform t; public float time, life, size; }
    private sealed class Bit { public Transform t; public Vector3 vel; public float life; public bool spin = true; }
    private sealed class Slide { public Transform t; public Vector3 target; public bool flatten; }
    private sealed class Blob { public Transform t, follow; public float size, floor; }
    public sealed class Label { public Vector3 pos; public string text; public Color color; }
    public sealed class FloatText { public Vector3 pos; public string text; public Color color; public float time, life; public bool big; }

    public Camera cam;
    private Light sun;
    private Transform world;
    public int areaIndex = -1, width, height, giggles;
    public char[,] grid;
    public Vector2 playerPos, playerVel, knock, facing = Vector2.down;
    public float invuln, attackCooldown, specialCooldown, swing, spin, dashTime, pushTimer, bumpCooldown, bannerTime, partyTime;
    public bool hasKey, gigglingOut, walking;
    public Boss boss;
    public readonly List<Critter> critters = new List<Critter>();
    public readonly List<Projectile> projectiles = new List<Projectile>();
    public readonly Dictionary<Vector2Int, Npc> npcs = new Dictionary<Vector2Int, Npc>();
    public readonly List<Label> labels = new List<Label>();
    public readonly List<FloatText> floats = new List<FloatText>();
    private readonly Dictionary<Vector2Int, Transform> tileModels = new Dictionary<Vector2Int, Transform>();
    private readonly Dictionary<Vector2Int, char> baleUnder = new Dictionary<Vector2Int, char>();
    private readonly Dictionary<Vector2Int, string> signs = new Dictionary<Vector2Int, string>();
    private readonly List<Drop> drops = new List<Drop>();
    private readonly List<Bob> bobs = new List<Bob>();
    private readonly List<Effect> effects = new List<Effect>();
    private readonly List<Bit> bits = new List<Bit>();
    private readonly List<Slide> slides = new List<Slide>();
    private readonly List<Blob> blobs = new List<Blob>();
    private readonly List<Transform> dancers = new List<Transform>();
    private readonly HashSet<Critter> dashHits = new HashSet<Critter>();
    private bool dashHitBoss;
    private MooArt.Rig hero;
    private MooArt.Rig[] stageGirls;
    private Transform spotlight;
    private float shake, dustTimer;

    private static readonly Color[] Confetti =
    {
        MooData.Hex("ff6fa8"), MooData.Hex("ffd34d"), MooData.Hex("7fd1ff"), MooData.Hex("8fe08a"), MooData.Hex("c9a7f0"), Color.white
    };

    // ---------- Grid helpers ----------

    public static Vector2 Center(Vector2Int t) => new Vector2(t.x * Tile, t.y * Tile);
    public static Vector2Int TileAt(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x / Tile + .5f), Mathf.FloorToInt(p.y / Tile + .5f));
    private static Vector3 World(Vector2 p, float y = 0) => new Vector3(p.x, y, p.y);

    public char At(Vector2Int t) => grid != null && t.x >= 0 && t.y >= 0 && t.x < width && t.y < height ? grid[t.x, t.y] : '#';

    public bool Solid(Vector2Int t, bool player)
    {
        char ch = At(t);
        switch (ch)
        {
            case '#': case 'T': case 'o': case 'w': case 'R': case 'S': case 'n': case 'D': case 'B': return true;
            case 'E': return !player;
        }
        if (ch >= '1' && ch <= '4') return !player || !GateOpen(ch - '0');
        return false;
    }

    private bool Overlaps(Vector2 p, float r, bool player)
    {
        int x0 = Mathf.FloorToInt((p.x - r) / Tile + .5f), x1 = Mathf.FloorToInt((p.x + r) / Tile + .5f);
        int y0 = Mathf.FloorToInt((p.y - r) / Tile + .5f), y1 = Mathf.FloorToInt((p.y + r) / Tile + .5f);
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                var t = new Vector2Int(x, y);
                if (!Solid(t, player)) continue;
                Vector2 c = Center(t);
                var closest = new Vector2(Mathf.Clamp(p.x, c.x - Tile / 2, c.x + Tile / 2), Mathf.Clamp(p.y, c.y - Tile / 2, c.y + Tile / 2));
                if ((p - closest).sqrMagnitude < r * r) return true;
            }
        return false;
    }

    // Moves a circle through the tile grid, sliding along walls. Returns true if anything blocked it.
    public bool MoveCircle(ref Vector2 p, Vector2 delta, float r, bool player)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / (r * .5f)));
        Vector2 step = delta / steps;
        bool blocked = false;
        for (int i = 0; i < steps; i++)
        {
            if (step.x != 0)
            {
                var next = new Vector2(p.x + step.x, p.y);
                if (Overlaps(next, r, player)) blocked = true; else p = next;
            }
            if (step.y != 0)
            {
                var next = new Vector2(p.x, p.y + step.y);
                if (Overlaps(next, r, player)) blocked = true; else p = next;
            }
        }
        return blocked;
    }

    // Flood fill used by the build checks: can the player walk next to the target tile?
    public bool Reachable(Vector2Int from, Vector2Int target)
    {
        var seen = new HashSet<Vector2Int> { from };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(from);
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int t = queue.Dequeue();
            foreach (Vector2Int d in dirs)
            {
                Vector2Int n = t + d;
                if (n == target) return true;
                if (seen.Contains(n) || Solid(n, true) || char.IsDigit(At(n)) || At(n) == 'E') continue;
                seen.Add(n);
                queue.Enqueue(n);
            }
        }
        return false;
    }

    public Vector2Int Find(char ch)
    {
        string[] map = MooData.Areas[areaIndex].Map;
        for (int r = 0; r < map.Length; r++)
        {
            int x = map[r].IndexOf(ch);
            if (x >= 0) return new Vector2Int(x, map.Length - 1 - r);
        }
        return new Vector2Int(-1, -1);
    }

    // ---------- World building ----------

    private void ClearWorld()
    {
        if (world != null) MooArt.Kill(world.gameObject);
        world = null;
        hero = null;
        stageGirls = null;
        spotlight = null;
        boss = null;
        grid = null;
        critters.Clear(); projectiles.Clear(); npcs.Clear(); labels.Clear(); floats.Clear(); tileModels.Clear();
        baleUnder.Clear(); signs.Clear(); drops.Clear(); bobs.Clear(); effects.Clear(); bits.Clear(); slides.Clear();
        dancers.Clear(); dashHits.Clear(); blobs.Clear();
        hasKey = gigglingOut = false;
        dashTime = spin = swing = invuln = specialCooldown = attackCooldown = pushTimer = shake = dustTimer = 0;
        knock = playerVel = Vector2.zero;
    }

    private void BuildStage()
    {
        ClearWorld();
        areaIndex = -1;
        world = MooArt.Root("Stage");
        var batch = new MooArt.Batch();
        MooArea home = MooData.Areas[0];
        batch.Add(Shape.Cube, new Vector3(0, -.1f, 10), new Vector3(80, .2f, 60), home.Ground);
        batch.Add(Shape.Cube, new Vector3(0, .2f, 0), new Vector3(10.5f, .4f, 3.2f), MooData.Hex("c99a5b"));
        batch.Add(Shape.Cube, new Vector3(0, .41f, -1.55f), new Vector3(10.5f, .06f, .12f), MooData.Hex("a77a45"));
        Barn(batch, new Vector3(0, 0, 9), 10, 6, false);
        for (int i = -1; i <= 1; i += 2)
        {
            Tree(batch, new Vector3(i * 9.5f, 0, 6), MooData.Hex("3f8f3a"), MooData.Hex("58a84a"), false);
            Tree(batch, new Vector3(i * 13, 0, 11), MooData.Hex("3f8f3a"), MooData.Hex("58a84a"), false);
            for (int k = 0; k < 6; k++)
                batch.Add(Shape.Cube, new Vector3(i * (6.5f + k * 2), .55f, 3.5f), new Vector3(.25f, 1.1f, .25f), home.Wall);
            batch.Add(Shape.Cube, new Vector3(i * 11.5f, .6f, 3.5f), new Vector3(10, .14f, .1f), home.Wall);
            batch.Add(Shape.Cube, new Vector3(i * 11.5f, .95f, 3.5f), new Vector3(10, .14f, .1f), home.Wall);
        }
        Flowers(batch, new Vector3(0, 0, 0), 40, 22);
        batch.Finish(world);
        stageGirls = new MooArt.Rig[MooData.Characters.Length];
        for (int i = 0; i < stageGirls.Length; i++)
        {
            stageGirls[i] = MooArt.Girl(MooData.Characters[i], world);
            stageGirls[i].Root.localPosition = new Vector3(-3.6f + i * 2.4f, .4f, 0);
            stageGirls[i].Root.localEulerAngles = new Vector3(0, 180, 0);
            AddShadow(stageGirls[i].Root, .9f, .44f);
        }
        for (int i = -1; i <= 1; i += 2)
        {
            Transform cow = MooArt.Cow("Audience cow", world, i < 0 ? MooData.Hex("ff8fc8") : MooData.Hex("7fc8f8"), false);
            cow.localPosition = new Vector3(i * 7.2f, 0, 1.5f);
            cow.localEulerAngles = new Vector3(0, 180 + i * 35, 0);
            bobs.Add(new Bob { t = cow, home = cow.localPosition, phase = i, height = .05f });
            AddShadow(cow, 1.7f);
        }
        spotlight = MooArt.Part(world, Shape.Cylinder, new Vector3(0, .42f, 0), new Vector3(1.6f, .03f, 1.6f), MooData.Hex("fff1a8"), default, true);
        cam.transform.position = new Vector3(0, 2.6f, -8.5f);
        cam.transform.LookAt(new Vector3(0, 1.1f, 0));
        ApplyLighting(home);
    }

    // Three-tone ambient (bright sky above, neutral middle, ground-tinted below) gives the flat-shaded models more depth.
    private void ApplyLighting(MooArea area)
    {
        cam.backgroundColor = area.Sky;
        sun.color = area.Sun;
        float a = area.Ambient;
        var mid = new Color(a, a, a * 1.05f);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.Lerp(mid, area.Sky, .4f) * 1.15f;
        RenderSettings.ambientEquatorColor = mid;
        RenderSettings.ambientGroundColor = Color.Lerp(mid, area.Ground, .5f) * .7f;
    }

    public void EnterArea(int index, int fromGate)
    {
        ClearWorld();
        areaIndex = index;
        MooArea area = MooData.Areas[index];
        string[] map = area.Map;
        height = map.Length;
        width = map[0].Length;
        grid = new char[width, height];
        world = MooArt.Root("World: " + area.Name);
        var batch = new MooArt.Batch();
        batch.Add(Shape.Cube, new Vector3((width - 1) * Tile / 2, -.1f, (height - 1) * Tile / 2),
            new Vector3(width * Tile + 40, .2f, height * Tile + 40), area.Ground);
        Random.State randomState = Random.state;
        Random.InitState(1234 + index);
        Vector2Int start = new Vector2Int(1, 1), barnMin = new Vector2Int(999, 999), barnMax = new Vector2Int(-1, -1);
        int signIndex = 0;
        Vector2Int goatTile = new Vector2Int(-1, -1);
        Color checker = Color.Lerp(area.Ground, area.Grass, .35f), tuft = Color.Lerp(area.Ground, area.Grass, .6f);
        for (int r = 0; r < height; r++)
            for (int x = 0; x < width; x++)
            {
                int y = height - 1 - r;
                var t = new Vector2Int(x, y);
                char ch = map[r][x];
                Vector3 pos = new Vector3(x * Tile, 0, y * Tile);
                grid[x, y] = ch;
                // Soft checkerboard of lighter grass so the ground reads as tiles instead of one flat sheet.
                if ((x + y) % 2 == 0 && "#w~R".IndexOf(ch) < 0)
                    batch.Add(Shape.Cube, pos + new Vector3(0, .005f, 0), new Vector3(Tile, .02f, Tile), checker);
                switch (ch)
                {
                    case '#': Wall(batch, area, pos, t); break;
                    case 'T':
                        if (index == 3) Tree(batch, pos, MooData.Hex("2f5a46"), MooData.Hex("3e7058"), true);
                        else Tree(batch, pos, MooData.Hex("3f8f3a"), MooData.Hex("58a84a"), false);
                        break;
                    case 'o':
                        batch.Add(Shape.Ball, pos + new Vector3(0, .5f, 0), new Vector3(1.9f, 1.3f, 1.7f), MooData.Hex("9c968a"));
                        batch.Add(Shape.Ball, pos + new Vector3(.5f, .9f, .2f), new Vector3(.9f, .8f, .9f), MooData.Hex("b5afa3"));
                        break;
                    case 'w':
                        batch.Add(Shape.Cube, pos + new Vector3(0, -.03f, 0), new Vector3(Tile, .1f, Tile), MooData.Hex("5aa9e6"));
                        if (Random.value < .35f)
                            batch.Add(Shape.Cylinder, pos + new Vector3(Random.Range(-.5f, .5f), .04f, Random.Range(-.5f, .5f)),
                                new Vector3(.6f, .02f, .6f), MooData.Hex("6fbf4a"));
                        break;
                    case '~':
                        batch.Add(Shape.Cube, pos + new Vector3(0, .01f, 0), new Vector3(Tile, .06f, Tile), MooData.Hex("7a5534"));
                        batch.Add(Shape.Ball, pos + new Vector3(Random.Range(-.6f, .6f), .05f, Random.Range(-.6f, .6f)),
                            new Vector3(.3f, .12f, .3f), MooData.Hex("8f6a45"));
                        break;
                    case 'R':
                        barnMin = Vector2Int.Min(barnMin, t);
                        barnMax = Vector2Int.Max(barnMax, t);
                        break;
                    case 'S':
                        batch.Add(Shape.Cylinder, pos + new Vector3(0, .55f, 0), new Vector3(.14f, 1.1f, .14f), MooData.Hex("8a5a35"));
                        batch.Add(Shape.Cube, pos + new Vector3(0, 1.1f, 0), new Vector3(1.3f, .7f, .12f), MooData.Hex("e0b77a"));
                        for (int k = 0; k < 3; k++)
                            batch.Add(Shape.Cube, pos + new Vector3(-.1f + (k % 2) * .1f, 1.28f - k * .17f, -.07f), new Vector3(.9f - k * .15f, .05f, .02f), MooData.Hex("8a5a35"));
                        signs[t] = signIndex < area.Signs.Length ? area.Signs[signIndex] : "Moo.";
                        signIndex++;
                        break;
                    case 'N':
                        AddNpc(t, "bessie", "Bessie", MooArt.Cow("Bessie", world, MooData.Hex("ff8fc8"), save.finished, true));
                        break;
                    case 'q':
                        AddNpc(t, "confused", "Cow", MooArt.Cow("Confused cow", world, Confetti[Random.Range(0, 4)], save.finished));
                        break;
                    case 'C':
                        if ((save.pieces & (1 << (index - 1))) != 0) grid[x, y] = '.';
                        else
                        {
                            AddNpc(t, "rescue", area.Cow, MooArt.Cow(area.Cow, world, Confetti[index], false));
                            labels.Add(new Label { pos = World(Center(t), 2.6f), text = area.Cow + "!", color = MooData.Hex("ffd34d") });
                        }
                        break;
                    case 'c':
                        grid[x, y] = '.';
                        SpawnCritter(area.Critter, Center(t));
                        break;
                    case 'G':
                        grid[x, y] = '.';
                        goatTile = t;
                        break;
                    case 'B':
                        tileModels[t] = Bale(pos);
                        break;
                    case 'D':
                        var door = MooArt.Root("Log gate", world);
                        door.localPosition = pos;
                        for (int k = 0; k < 3; k++)
                            MooArt.Part(door, Shape.Cylinder, new Vector3(0, .35f + k * .55f, 0), new Vector3(.55f, Tile / 2, .55f), MooData.Hex("7a5030"), new Vector3(0, 0, 90));
                        MooArt.Part(door, Shape.Ball, new Vector3(0, .95f, -.35f), new Vector3(.4f, .45f, .2f), MooArt.Gold);
                        tileModels[t] = door;
                        break;
                    case 'k':
                        tileModels[t] = Acorn(pos);
                        break;
                    case 'm':
                        tileModels[t] = Milk(world, pos);
                        break;
                    case '$':
                        if (save.collected.Contains(CoinKey(t))) grid[x, y] = '.';
                        else tileModels[t] = Coin(world, pos);
                        break;
                    case 'P':
                        grid[x, y] = '.';
                        start = t;
                        break;
                    case 'E':
                        batch.Add(Shape.Cylinder, pos + new Vector3(0, .03f, 0), new Vector3(1.7f, .04f, 1.7f), MooData.Hex("ffe066"), default, true);
                        labels.Add(new Label { pos = World(Center(t), 1.4f), text = "To the barn", color = Color.white });
                        break;
                    case '1': case '2': case '3': case '4':
                        Gate(batch, pos, t, ch - '0');
                        break;
                    default:
                        if (Random.value < .22f) Flowers(batch, pos, 1.6f, 2);
                        if (Random.value < .35f)
                            for (int k = 0; k < 3; k++)
                                batch.Add(Shape.Cone, pos + new Vector3(Random.Range(-.8f, .8f), .14f, Random.Range(-.8f, .8f)),
                                    new Vector3(.1f, .28f, .1f), tuft, new Vector3(Random.Range(-15f, 15f), 0, Random.Range(-15f, 15f)));
                        break;
                }
            }
        if (barnMax.x >= 0)
        {
            Vector2 lo = Center(barnMin), hi = Center(barnMax);
            Barn(batch, new Vector3((lo.x + hi.x) / 2, 0, (lo.y + hi.y) / 2), hi.x - lo.x + Tile, hi.y - lo.y + Tile);
        }
        if (index == 0)
            for (int i = 0; i < 3; i++)
                if ((save.pieces & (1 << i)) != 0)
                    AddNpc(MooData.HubCowSpots[i], "rescued" + i, MooData.Cows[i],
                        MooArt.Cow(MooData.Cows[i], world, Confetti[i + 1], save.finished));
        if (index == 3)
            for (int i = 0; i < 30; i++)
            {
                Transform fly = MooArt.Part(world, Shape.Ball, new Vector3(Random.Range(0, width * Tile), Random.Range(.8f, 2.6f),
                    Random.Range(0, height * Tile)), Vector3.one * .16f, MooData.Hex("fff38a"), default, true);
                bobs.Add(new Bob { t = fly, home = fly.localPosition, phase = Random.value * 6, height = .35f });
            }
        batch.Finish(world);
        Random.state = randomState;

        hero = MooArt.Girl(Hero, world);
        hero.Crown.gameObject.SetActive(save.crown);
        AddShadow(hero.Root, .85f);
        playerPos = Center(start);
        facing = Vector2.down;
        if (index == 0 && fromGate > 0)
        {
            Vector2Int gate = Find((char)('0' + fromGate));
            foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                if (!Solid(gate + d, true) && At(gate + d) == '.')
                {
                    playerPos = Center(gate + d);
                    facing = new Vector2(d.x, d.y);
                    break;
                }
        }

        if (index == 4 && goatTile.x >= 0)
        {
            if (save.finished)
            {
                grid[goatTile.x, goatTile.y] = 'n';
                AddNpc(goatTile, "goat", "Grumbleweed", MooArt.Goat(world));
            }
            else
            {
                boss = new Boss { pos = Center(goatTile), model = MooArt.Goat(world) };
                boss.model.localPosition = World(boss.pos);
                AddShadow(boss.model, 2.4f);
            }
        }

        ApplyLighting(area);
        SnapCamera();
        bannerTime = 3.2f;
        state.message = area.Name + ": " + area.Subtitle;
        state.status = "playing";
        MooMusic.Track[] songs = { MooMusic.Track.Barnyard, MooMusic.Track.Meadow, MooMusic.Track.Marsh, MooMusic.Track.Woods,
            save.finished ? MooMusic.Track.Sunset : MooMusic.Track.Boss };
        PlayMusic(songs[index]);
        if (boss != null) Say(MooData.BossIntro, () => { boss.active = true; boss.timer = 1.2f; });
    }

    private string CoinKey(Vector2Int t) => areaIndex + ":" + t.x + ":" + t.y + ";";

    private void AddNpc(Vector2Int t, string kind, string name, Transform model)
    {
        grid[t.x, t.y] = 'n';
        model.localPosition = World(Center(t));
        model.localEulerAngles = new Vector3(0, 180 + Random.Range(-40f, 40f), 0);
        npcs[t] = new Npc { kind = kind, name = name, tile = t, model = model };
        AddShadow(model, kind == "goat" ? 2.4f : 1.7f);
    }

    private void AddShadow(Transform follow, float size, float floor = .07f)
    {
        if (world == null || follow == null) return;
        blobs.Add(new Blob { t = MooArt.BlobShadow(world), follow = follow, size = size, floor = floor });
    }

    public void Shake(float amount) => shake = Mathf.Max(shake, amount);

    private void Wall(MooArt.Batch batch, MooArea area, Vector3 pos, Vector2Int t)
    {
        switch (areaIndex)
        {
            case 0:
                batch.Add(Shape.Cube, pos + new Vector3(0, .6f, 0), new Vector3(.3f, 1.2f, .3f), area.Wall);
                foreach (Vector2Int d in new[] { Vector2Int.right, Vector2Int.up })
                {
                    char n = MapChar(t + d);
                    if (n != '#' && !(n >= '1' && n <= '4')) continue;
                    var offset = new Vector3(d.x, 0, d.y) * (Tile / 2);
                    var size = d.x != 0 ? new Vector3(Tile, .14f, .1f) : new Vector3(.1f, .14f, Tile);
                    batch.Add(Shape.Cube, pos + offset + Vector3.up * .5f, size, area.Wall);
                    batch.Add(Shape.Cube, pos + offset + Vector3.up * .9f, size, area.Wall);
                }
                Flowers(batch, pos, 1.6f, 1);
                break;
            case 1:
                batch.Add(Shape.Cube, pos + new Vector3(0, .65f, 0), new Vector3(Tile, 1.3f, Tile), area.Wall);
                batch.Add(Shape.Ball, pos + new Vector3(Random.Range(-.4f, .4f), 1.3f, Random.Range(-.4f, .4f)), new Vector3(1.4f, .7f, 1.4f), MooData.Hex("5fa347"));
                if (Random.value < .3f) batch.Add(Shape.Ball, pos + new Vector3(.3f, 1.45f, -.6f), Vector3.one * .2f, MooData.Hex("ff8fc8"));
                break;
            case 2:
                batch.Add(Shape.Cube, pos + new Vector3(0, .4f, 0), new Vector3(Tile, .8f, Tile), MooData.Hex("6f7f5a"));
                for (int k = 0; k < 4; k++)
                {
                    float h = Random.Range(1.2f, 2f);
                    batch.Add(Shape.Cylinder, pos + new Vector3(Random.Range(-.8f, .8f), .8f + h / 2, Random.Range(-.8f, .8f)), new Vector3(.08f, h, .08f), MooData.Hex("8fa85a"));
                }
                batch.Add(Shape.Cylinder, pos + new Vector3(.3f, 2.1f, .2f), new Vector3(.16f, .4f, .16f), MooData.Hex("7a5030"));
                break;
            case 3:
                Tree(batch, pos, MooData.Hex("2f5a46"), MooData.Hex("3e7058"), true);
                break;
            default:
                batch.Add(Shape.Cube, pos + new Vector3(0, .7f, 0), new Vector3(Tile, 1.4f, Tile), area.Wall);
                batch.Add(Shape.Ball, pos + new Vector3(Random.Range(-.4f, .4f), 1.4f, Random.Range(-.4f, .4f)), new Vector3(1.5f, .8f, 1.5f), MooData.Hex("a49e92"));
                break;
        }
    }

    private char MapChar(Vector2Int t)
    {
        string[] map = MooData.Areas[areaIndex].Map;
        if (t.x < 0 || t.y < 0 || t.x >= width || t.y >= height) return ' ';
        return map[height - 1 - t.y][t.x];
    }

    private static void Tree(MooArt.Batch batch, Vector3 pos, Color leaves, Color top, bool berries)
    {
        batch.Add(Shape.Cylinder, pos + new Vector3(0, .55f, 0), new Vector3(.45f, 1.1f, .45f), MooData.Hex("8a5a35"));
        batch.Add(Shape.Cone, pos + new Vector3(0, 1.55f, 0), new Vector3(2f, 1.5f, 2f), leaves);
        batch.Add(Shape.Cone, pos + new Vector3(0, 2.3f, 0), new Vector3(1.4f, 1.2f, 1.4f), top);
        if (!berries) return;
        for (int k = 0; k < 3; k++)
            batch.Add(Shape.Ball, pos + new Vector3(Mathf.Cos(k * 2.1f) * .7f, 1.3f + k * .15f, Mathf.Sin(k * 2.1f) * .7f), Vector3.one * .2f, MooData.Hex("e889d6"));
    }

    private static void Flowers(MooArt.Batch batch, Vector3 center, float spread, int count)
    {
        Color[] colors = { Color.white, MooData.Hex("ffe066"), MooData.Hex("ff9ccf"), MooData.Hex("b5e07a") };
        for (int i = 0; i < count; i++)
        {
            var p = center + new Vector3(Random.Range(-spread, spread), .06f, Random.Range(-spread, spread) * .6f);
            batch.Add(Shape.Ball, p, new Vector3(.16f, .1f, .16f), colors[Random.Range(0, colors.Length)]);
        }
    }

    // The title stage leaves out the hayloft window, which otherwise peeks out from behind the "MOO QUEST" title.
    private static void Barn(MooArt.Batch batch, Vector3 c, float w, float d, bool loft = true)
    {
        Color red = MooData.Hex("c8453b"), roof = MooData.Hex("8f2f2a"), trim = Color.white;
        batch.Add(Shape.Cube, c + new Vector3(0, 1.6f, 0), new Vector3(w - .3f, 3.2f, d - .3f), red);
        float slope = 35, length = (d / 2) / Mathf.Cos(slope * Mathf.Deg2Rad) + .4f;
        float rise = (d / 4) * Mathf.Tan(slope * Mathf.Deg2Rad);
        batch.Add(Shape.Cube, c + new Vector3(0, 3.2f + rise, -d / 4), new Vector3(w + .3f, .25f, length), roof, new Vector3(-slope, 0, 0));
        batch.Add(Shape.Cube, c + new Vector3(0, 3.2f + rise, d / 4), new Vector3(w + .3f, .25f, length), roof, new Vector3(slope, 0, 0));
        batch.Add(Shape.Cube, c + new Vector3(0, 3.2f + rise * .9f, 0), new Vector3(w - .3f, rise * 1.8f, d * .6f), red);
        float front = c.z - d / 2 + .1f;
        batch.Add(Shape.Cube, new Vector3(c.x, 1.2f, front - .05f), new Vector3(2.4f, 2.4f, .1f), trim);
        batch.Add(Shape.Cube, new Vector3(c.x, 1.2f, front - .1f), new Vector3(2.1f, 2.1f, .1f), red);
        batch.Add(Shape.Cube, new Vector3(c.x, 1.2f, front - .15f), new Vector3(2.9f, .15f, .05f), trim, new Vector3(0, 0, 45));
        batch.Add(Shape.Cube, new Vector3(c.x, 1.2f, front - .15f), new Vector3(2.9f, .15f, .05f), trim, new Vector3(0, 0, -45));
        if (!loft) return;
        batch.Add(Shape.Cube, new Vector3(c.x, 3.3f, front - .05f), new Vector3(1f, .9f, .1f), trim);
        batch.Add(Shape.Cube, new Vector3(c.x, 3.3f, front - .1f), new Vector3(.8f, .7f, .1f), MooData.Hex("e8b64f"));
    }

    private void Gate(MooArt.Batch batch, Vector3 pos, Vector2Int t, int gate)
    {
        bool open = GateOpen(gate);
        Color wood = MooData.Hex("8a5a35");
        bool horizontal = t.y == 0 || t.y == height - 1;
        Vector3 side = horizontal ? Vector3.right : Vector3.forward;
        batch.Add(Shape.Cube, pos + side * .95f + Vector3.up * 1.1f, new Vector3(.3f, 2.2f, .3f), wood);
        batch.Add(Shape.Cube, pos - side * .95f + Vector3.up * 1.1f, new Vector3(.3f, 2.2f, .3f), wood);
        batch.Add(Shape.Cube, pos + Vector3.up * 2.25f, horizontal ? new Vector3(2.4f, .3f, .35f) : new Vector3(.35f, .3f, 2.4f), wood);
        if (open)
            batch.Add(Shape.Cylinder, pos + new Vector3(0, .03f, 0), new Vector3(1.6f, .04f, 1.6f), MooData.Hex("ffe066"), default, true);
        else
            for (int k = 0; k < 3; k++)
                batch.Add(Shape.Cube, pos + Vector3.up * (.4f + k * .5f), horizontal ? new Vector3(1.8f, .22f, .15f) : new Vector3(.15f, .22f, 1.8f), MooData.Hex("b07a48"));
        labels.Add(new Label
        {
            pos = World(Center(t), 2.9f),
            text = MooData.GateNames[gate] + (open ? "" : " (locked)"),
            color = open ? Color.white : MooData.Hex("ffd0c0"),
        });
    }

    private Transform Bale(Vector3 pos)
    {
        var bale = MooArt.Root("Hay bale", world);
        bale.localPosition = pos;
        MooArt.Part(bale, Shape.Cube, new Vector3(0, .6f, 0), new Vector3(1.75f, 1.2f, 1.75f), MooArt.Hay);
        MooArt.Part(bale, Shape.Cube, new Vector3(-.45f, .6f, 0), new Vector3(.1f, 1.22f, 1.77f), MooData.Hex("b57a32"));
        MooArt.Part(bale, Shape.Cube, new Vector3(.45f, .6f, 0), new Vector3(.1f, 1.22f, 1.77f), MooData.Hex("b57a32"));
        return bale;
    }

    private Transform Acorn(Vector3 pos)
    {
        var acorn = MooArt.Root("Golden acorn", world);
        acorn.localPosition = pos + Vector3.up * .6f;
        MooArt.Part(acorn, Shape.Ball, Vector3.zero, new Vector3(.45f, .55f, .45f), MooArt.Gold);
        MooArt.Part(acorn, Shape.Ball, new Vector3(0, .22f, 0), new Vector3(.52f, .25f, .52f), MooData.Hex("a8742f"));
        MooArt.Part(acorn, Shape.Cylinder, new Vector3(0, .38f, 0), new Vector3(.06f, .14f, .06f), MooData.Hex("7a5030"));
        bobs.Add(new Bob { t = acorn, home = acorn.localPosition, spin = 90, height = .15f });
        AddShadow(acorn, .5f);
        return acorn;
    }

    private Transform Milk(Transform parent, Vector3 pos)
    {
        var milk = MooArt.Root("Milk", parent);
        milk.localPosition = pos + Vector3.up * .5f;
        MooArt.Part(milk, Shape.Cylinder, Vector3.zero, new Vector3(.38f, .55f, .38f), Color.white);
        MooArt.Part(milk, Shape.Cylinder, new Vector3(0, .36f, 0), new Vector3(.22f, .18f, .22f), Color.white);
        MooArt.Part(milk, Shape.Cylinder, new Vector3(0, .5f, 0), new Vector3(.24f, .1f, .24f), MooData.Hex("5aa9e6"));
        MooArt.Part(milk, Shape.Cube, new Vector3(0, 0, -.18f), new Vector3(.2f, .18f, .02f), MooData.Hex("ff8fc8"));
        bobs.Add(new Bob { t = milk, home = milk.localPosition, spin = 60, height = .12f, phase = pos.x });
        AddShadow(milk, .45f);
        return milk;
    }

    private Transform Coin(Transform parent, Vector3 pos)
    {
        var coin = MooArt.Root("Moo-nie", parent);
        coin.localPosition = pos + Vector3.up * .6f;
        MooArt.Part(coin, Shape.Cylinder, Vector3.zero, new Vector3(.6f, .08f, .6f), MooArt.Gold, new Vector3(90, 0, 0));
        MooArt.Part(coin, Shape.Ball, new Vector3(0, 0, 0), new Vector3(.3f, .3f, .12f), Color.white);
        MooArt.Part(coin, Shape.Ball, new Vector3(.05f, .04f, 0), new Vector3(.1f, .1f, .14f), MooArt.Black);
        bobs.Add(new Bob { t = coin, home = coin.localPosition, spin = 150, height = .12f, phase = pos.z });
        AddShadow(coin, .5f);
        return coin;
    }

    private void SpawnCritter(int kind, Vector2 pos)
    {
        if (kind < 0) return;
        var c = new Critter
        {
            kind = kind, pos = pos, home = pos, hp = MooData.CritterHp[kind], phase = Random.value * 5,
            wanderTimer = Random.Range(0, 2f), model = MooArt.Critter(kind, world),
        };
        c.model.localPosition = World(pos);
        AddShadow(c.model, kind == 1 ? .95f : .85f);
        critters.Add(c);
    }

    // ---------- Gameplay ----------

    private void PlayStep(float dt)
    {
        MooCharacter me = Hero;
        invuln -= dt; attackCooldown -= dt; specialCooldown -= dt; swing -= dt; spin -= dt; bumpCooldown -= dt; bannerTime -= dt;
        Vector2 input = gigglingOut ? Vector2.zero : MoveInput();
        walking = input.sqrMagnitude > .04f;
        if (dashTime > 0)
        {
            dashTime -= dt;
            MoveCircle(ref playerPos, facing * 20 * dt, PlayerRadius, true);
            foreach (Critter c in critters)
                if (!c.giggling && !dashHits.Contains(c) && (c.pos - playerPos).sqrMagnitude < 1.4f * 1.4f)
                {
                    dashHits.Add(c);
                    HurtCritter(c, 2, facing);
                }
            if (boss != null && boss.active && !dashHitBoss && (boss.pos - playerPos).magnitude < 2.1f)
            {
                dashHitBoss = true;
                HurtBoss(2);
            }
        }
        else
        {
            if (walking) facing = input.normalized;
            bool mud = At(TileAt(playerPos)) == '~';
            Vector2 want = input * me.Speed * (mud ? .85f : 1);
            playerVel = mud ? Vector2.MoveTowards(playerVel, want, 7 * dt) : want;
            MoveCircle(ref playerPos, (playerVel + knock) * dt, PlayerRadius, true);
            knock = Vector2.MoveTowards(knock, Vector2.zero, 40 * dt);
            Probe(input, dt);
        }
        dustTimer -= dt;
        if ((walking || dashTime > 0) && !gigglingOut && dustTimer <= 0)
        {
            dustTimer = dashTime > 0 ? .04f : .2f;
            Puff(playerPos - facing * .3f);
        }

        if (!gigglingOut)
        {
            if (actionPressed && (SomethingToTickle() || !TryTalk())) Tickle();
            if (state.status != "playing") return;
            if (specialPressed) Special();
            Pickups();
            char under = At(TileAt(playerPos));
            if (areaIndex == 0 && under >= '1' && under <= '4' && GateOpen(under - '0'))
            {
                int gate = under - '0';
                FadeThen(() => EnterArea(gate, -1));
            }
            else if (under == 'E')
            {
                int from = areaIndex;
                FadeThen(() => EnterArea(0, from));
            }
        }
        UpdateCritters(dt);
        UpdateProjectiles(dt);
        UpdateBoss(dt);
        if (giggles <= 0 && !gigglingOut) GiggleOut();
    }

    private void Probe(Vector2 input, float dt)
    {
        if (input.sqrMagnitude < .04f)
        {
            pushTimer = 0;
            return;
        }
        var d = Mathf.Abs(input.x) > Mathf.Abs(input.y)
            ? new Vector2Int((int)Mathf.Sign(input.x), 0) : new Vector2Int(0, (int)Mathf.Sign(input.y));
        Vector2Int t = TileAt(playerPos + new Vector2(d.x, d.y) * (PlayerRadius + .3f));
        char ch = At(t);
        if (ch == 'B')
        {
            pushTimer += dt;
            if (pushTimer > .25f)
            {
                pushTimer = 0;
                PushBale(t, d);
            }
        }
        else pushTimer = 0;
        if (ch == 'D')
        {
            if (hasKey) OpenDoor(t);
            else Bump("This log gate is locked. Maybe a golden acorn opens it?");
        }
        if (ch >= '1' && ch <= '4' && !GateOpen(ch - '0')) Bump(LockedText(ch - '0'));
    }

    public static string LockedText(int gate) =>
        gate == 2 ? "Locked! Help Buttercup in Clover Meadow first." :
        gate == 3 ? "Locked! Help Moo-donna in Mudpuddle Marsh first." :
        "Locked! Bring all three cowbell pieces first.";

    private void Bump(string text)
    {
        if (bumpCooldown > 0) return;
        bumpCooldown = 2.5f;
        Float(text, playerPos, Color.white);
        state.message = text;
    }

    public bool PushBale(Vector2Int from, Vector2Int dir)
    {
        Vector2Int to = from + dir;
        char ch = At(to);
        if (ch != '.' && ch != '~' && ch != 'w')
        {
            Bump("Hnnngh! It won't budge that way.");
            return false;
        }
        foreach (Critter c in critters)
            if (TileAt(c.pos) == to) return false;
        grid[from.x, from.y] = baleUnder.TryGetValue(from, out char under) ? under : '.';
        baleUnder.Remove(from);
        tileModels.TryGetValue(from, out Transform model);
        tileModels.Remove(from);
        Vector3 target = World(Center(to));
        if (ch == 'w')
        {
            grid[to.x, to.y] = '=';
            Float("A hay bridge! Hay-mazing!", Center(to), MooData.Hex("ffe066"), true);
            state.message = "You built a hay bridge!";
            Play(sparkleClip);
        }
        else
        {
            grid[to.x, to.y] = 'B';
            baleUnder[to] = ch;
            tileModels[to] = model;
            Play(bonkClip, .6f);
        }
        if (model != null) slides.Add(new Slide { t = model, target = target, flatten = ch == 'w' });
        return true;
    }

    private void OpenDoor(Vector2Int t)
    {
        grid[t.x, t.y] = '.';
        if (tileModels.TryGetValue(t, out Transform door)) MooArt.Kill(door.gameObject);
        tileModels.Remove(t);
        hasKey = false;
        Burst(Center(t), 20);
        Shake(.3f);
        Float("Click-clack! The log gate swings open!", Center(t), MooData.Hex("ffe066"), true);
        state.message = "The log gate opened!";
        Play(sparkleClip);
    }

    // The nearest cow, goat, or sign in front of the hero, if any.
    public bool TalkTarget(out Npc npc, out Vector2Int? sign)
    {
        Vector2 probe = playerPos + facing * 1.1f;
        float best = 1.8f;
        npc = null;
        sign = null;
        foreach (Npc n in npcs.Values)
        {
            float d = (Center(n.tile) - probe).magnitude;
            if (d < best) { best = d; npc = n; sign = null; }
        }
        foreach (Vector2Int s in signs.Keys)
        {
            float d = (Center(s) - probe).magnitude;
            if (d < best) { best = d; sign = s; npc = null; }
        }
        return npc != null || sign.HasValue;
    }

    // Tickling wins over chatting when a critter (or the goat) is right in front of the hero.
    public bool SomethingToTickle()
    {
        Vector2 center = playerPos + facing * 1.0f;
        foreach (Critter c in critters)
            if (!c.giggling && (c.pos - center).magnitude < 1.35f) return true;
        return boss != null && boss.active && (boss.pos - center).magnitude < 2.1f;
    }

    private bool TryTalk()
    {
        TalkTarget(out Npc npc, out Vector2Int? sign);
        if (sign.HasValue)
        {
            Say(new[] { "Sign|" + signs[sign.Value] }, null);
            return true;
        }
        if (npc == null) return false;
        Vector2 toMe = playerPos - Center(npc.tile);
        npc.model.localRotation = Quaternion.LookRotation(new Vector3(toMe.x, 0, toMe.y));
        npc.talks++;
        switch (npc.kind)
        {
            case "bessie":
                Play(mooClip);
                Say(new[] { "Bessie|" + Hint() }, null);
                break;
            case "confused":
                if (save.finished) { Play(mooClip); Say(new[] { "Cow|" + Pick(MooData.HappyCowLines) }, null); }
                else { Play(quackClip); Say(new[] { "Confused Cow|" + MooData.ConfusedLines[npc.talks % MooData.ConfusedLines.Length] }, null); }
                break;
            case "rescue":
                Rescue();
                break;
            case "goat":
                Play(bleatClip);
                Say(new[] { "Grumbleweed|Party at the barn later? I'll bring the bell AND the snacks! Hee hee!" }, null);
                break;
            default:
                int cow = npc.kind[npc.kind.Length - 1] - '0';
                Play(mooClip);
                Say(new[] { npc.name + "|" + (save.finished ? Pick(MooData.HappyCowLines) : MooData.RescuedLines[cow]) }, null);
                break;
        }
        return true;
    }

    public void Rescue()
    {
        int index = areaIndex;
        MooArea area = MooData.Areas[index];
        Play(mooClip);
        Say(area.Rescue, () =>
        {
            save.pieces |= 1 << (index - 1);
            giggles = Hero.MaxGiggles;
            Save();
            Play(sparkleClip);
            FadeThen(() =>
            {
                EnterArea(0, -1);
                Say(new[] { "Bessie|Hooray, " + area.Cow + " is home! " + Hint() }, null);
            });
        });
    }

    public void Tickle()
    {
        if (attackCooldown > 0) return;
        attackCooldown = .35f;
        swing = .22f;
        Play(swishClip, .7f);
        Vector2 center = playerPos + facing * 1.0f;
        foreach (Critter c in critters.ToArray())
            if (!c.giggling && (c.pos - center).magnitude < 1.35f)
                HurtCritter(c, Hero.Power, facing);
        if (boss != null && boss.active && (boss.pos - center).magnitude < 2.1f) HurtBoss(Hero.Power);
    }

    public void Special()
    {
        if (specialCooldown > 0)
        {
            Bump(Hero.Special + " is recharging...");
            return;
        }
        MooCharacter me = Hero;
        specialCooldown = me.Cooldown;
        Float(me.Special + "!", playerPos, MooData.Hex("ffe066"), true);
        state.message = me.Name + " used " + me.Special + "!";
        switch (save.character)
        {
            case 0:
                spin = .5f;
                Shake(.25f);
                Ring(playerPos, 3.2f, MooData.Hex("ffe066"));
                Play(sparkleClip);
                foreach (Critter c in critters.ToArray())
                    if (!c.giggling && (c.pos - playerPos).magnitude < 3.2f) HurtCritter(c, 2, c.pos - playerPos);
                if (boss != null && boss.active && (boss.pos - playerPos).magnitude < 3.8f) HurtBoss(2);
                break;
            case 1:
                dashTime = .28f;
                invuln = Mathf.Max(invuln, .4f);
                dashHits.Clear();
                dashHitBoss = false;
                Play(swishClip);
                break;
            case 2:
                var hay = MooArt.Root("Rolling hay", world);
                MooArt.Part(hay, Shape.Cylinder, Vector3.zero, new Vector3(1f, .9f, 1f), MooArt.Hay, new Vector3(0, 0, 90));
                MooArt.Part(hay, Shape.Cylinder, Vector3.zero, new Vector3(.6f, .92f, .6f), MooData.Hex("b57a32"), new Vector3(0, 0, 90));
                projectiles.Add(new Projectile { pos = playerPos + facing * .8f, vel = facing * 11, life = 1.2f, friendly = true, model = hay });
                Play(bonkClip);
                break;
            default:
                Ring(playerPos, 5.5f, MooData.Hex("ff9ccf"));
                Play(lullabyClip);
                foreach (Critter c in critters)
                    if (!c.giggling && (c.pos - playerPos).magnitude < 5.5f) { c.sleep = 4.5f; c.snore = 0; }
                if (boss != null && boss.active && (boss.pos - playerPos).magnitude < 6.5f) DizzyBoss(2.6f);
                Float("La la la~", playerPos + Vector2.up * .5f, MooData.Hex("ff9ccf"));
                break;
        }
    }

    public void HurtCritter(Critter c, int amount, Vector2 dir)
    {
        if (c.giggling) return;
        if (c.sleep > 0) { amount++; c.sleep = 0; }
        c.hp -= amount;
        c.hurt = .25f;
        c.stun = .35f;
        c.knock = (dir.sqrMagnitude > .001f ? dir.normalized : Vector2.up) * 7;
        if (c.hp > 0)
        {
            Float("Tee hee!", c.pos, MooData.Hex("ffd0e6"));
            return;
        }
        c.giggling = true;
        c.happy = 1.1f;
        Shake(.15f);
        Float(Pick(MooData.CritterGiggles), c.pos, MooData.Hex("ffe066"));
        Play(highGiggle, .8f);
        float roll = Random.value;
        if (roll < .35f) AddDrop(c.pos, 1);
        else if (roll < .55f) AddDrop(c.pos, 0);
    }

    private void AddDrop(Vector2 pos, int kind)
    {
        Vector3 at = World(pos);
        drops.Add(new Drop { pos = pos, kind = kind, model = kind == 0 ? Milk(world, at) : Coin(world, at) });
    }

    public void HitPlayer(Vector2 from)
    {
        if (invuln > 0 || dashTime > 0 || gigglingOut) return;
        giggles = Mathf.Max(0, giggles - 1);
        invuln = 1.3f;
        Vector2 away = playerPos - from;
        knock = (away.sqrMagnitude < .001f ? -facing : away.normalized) * 10;
        Float(Pick(MooData.GotTickled), playerPos, MooData.Hex("ff9ccf"));
        Shake(.45f);
        Play(giggleClip);
        state.message = "Tickled! " + giggles + " giggles left.";
    }

    private void GiggleOut()
    {
        gigglingOut = true;
        Float("Giggled out! Hee hee hee!", playerPos, MooData.Hex("ff9ccf"), true);
        Play(giggleClip);
        FadeThen(() =>
        {
            giggles = Hero.MaxGiggles;
            EnterArea(0, -1);
            Say(new[] { "Bessie|" + Pick(MooData.RespawnLines) }, null);
        });
    }

    private void Pickups()
    {
        Vector2Int t = TileAt(playerPos);
        char ch = At(t);
        if (ch == 'm' && giggles < Hero.MaxGiggles) DrinkMilk();
        else if (ch == '$')
        {
            save.collected += CoinKey(t);
            GetCoin();
        }
        else if (ch == 'k')
        {
            hasKey = true;
            Float("You found a golden acorn!", playerPos, MooArt.Gold, true);
            state.message = "You found a golden acorn! It opens log gates.";
            Play(sparkleClip);
        }
        else return;
        grid[t.x, t.y] = '.';
        if (tileModels.TryGetValue(t, out Transform model)) MooArt.Kill(model.gameObject);
        tileModels.Remove(t);
    }

    private void DrinkMilk()
    {
        giggles = Mathf.Min(Hero.MaxGiggles, giggles + 2);
        Float("Gulp! +2 giggles", playerPos, Color.white);
        state.message = "Yum, milk! Giggles refilled.";
        Play(dingClip);
    }

    private void GetCoin()
    {
        save.moonies++;
        Float("+1 Moo-nie!", playerPos, MooArt.Gold);
        state.message = "Moo-nies: " + save.moonies;
        Play(dingClip);
        // Heroes who finish first can still earn the crown by collecting the rest afterwards.
        if (save.finished && !save.crown && save.moonies >= MooData.HatGoal)
        {
            save.crown = true;
            if (hero != null) hero.Crown.gameObject.SetActive(true);
            Float("You earned the sparkly Moo-nie Crown!", playerPos + Vector2.up, MooArt.Gold, true);
            state.message = "You earned the sparkly Moo-nie Crown!";
            Burst(playerPos, 20);
            Play(sparkleClip);
        }
        Save();
    }

    private void UpdateDrops()
    {
        for (int i = drops.Count - 1; i >= 0; i--)
        {
            Drop d = drops[i];
            if ((d.pos - playerPos).magnitude > 1.1f) continue;
            if (d.kind == 0)
            {
                if (giggles >= Hero.MaxGiggles) continue;
                DrinkMilk();
            }
            else GetCoin();
            MooArt.Kill(d.model.gameObject);
            drops.RemoveAt(i);
        }
    }

    private void UpdateCritters(float dt)
    {
        UpdateDrops();
        for (int i = critters.Count - 1; i >= 0; i--)
        {
            Critter c = critters[i];
            c.phase += dt;
            c.hurt -= dt;
            if (c.giggling)
            {
                c.happy -= dt;
                Vector2 away = c.pos - playerPos;
                if (away.sqrMagnitude < .01f) away = Vector2.up;
                MoveCircle(ref c.pos, away.normalized * 5 * dt, CritterRadius, false);
                if (c.happy <= 0)
                {
                    Burst(c.pos, 14);
                    MooArt.Kill(c.model.gameObject);
                    critters.RemoveAt(i);
                }
                continue;
            }
            if (c.sleep > 0)
            {
                c.sleep -= dt;
                c.snore -= dt;
                if (c.snore <= 0)
                {
                    c.snore = 1.2f;
                    Float("Zzz-moo...", c.pos, MooData.Hex("c9d8ff"));
                }
                continue;
            }
            Vector2 want;
            if (c.stun > 0)
            {
                c.stun -= dt;
                want = c.knock;
                c.knock = Vector2.MoveTowards(c.knock, Vector2.zero, 20 * dt);
            }
            else
            {
                Vector2 to = playerPos - c.pos;
                float distance = to.magnitude, speed = MooData.CritterSpeed[c.kind];
                if (distance < 6.5f && !gigglingOut) want = to / Mathf.Max(distance, .01f) * speed;
                else
                {
                    c.wanderTimer -= dt;
                    if (c.wanderTimer <= 0)
                    {
                        c.wanderTimer = Random.Range(1f, 2.5f);
                        c.wander = Random.value < .3f ? Vector2.zero : Random.insideUnitCircle.normalized;
                        if ((c.home - c.pos).magnitude > 4) c.wander = (c.home - c.pos).normalized;
                    }
                    want = c.wander * speed * .5f;
                }
                if (c.kind == 1) want *= Mathf.Sin(c.phase * 5) > 0 ? 1.6f : .2f;
            }
            if (want.sqrMagnitude > .01f) c.face = want.normalized;
            MoveCircle(ref c.pos, want * dt, CritterRadius, false);
            if ((c.pos - playerPos).magnitude < PlayerRadius + CritterRadius + .05f && invuln <= 0 && dashTime <= 0)
            {
                HitPlayer(c.pos);
                c.stun = .6f;
                c.knock = (c.pos - playerPos).normalized * 6;
            }
        }
    }

    private void UpdateProjectiles(float dt)
    {
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            Projectile p = projectiles[i];
            p.life -= dt;
            p.pos += p.vel * dt;
            bool pop = p.life <= 0 || Solid(TileAt(p.pos), false);
            if (!pop && p.friendly)
            {
                foreach (Critter c in critters.ToArray())
                    if (!c.giggling && (c.pos - p.pos).magnitude < .95f)
                    {
                        HurtCritter(c, 3, p.vel);
                        pop = true;
                        break;
                    }
                if (!pop && boss != null && boss.active && (boss.pos - p.pos).magnitude < 1.6f)
                {
                    HurtBoss(3);
                    pop = true;
                }
            }
            else if (!pop && (p.pos - playerPos).magnitude < .8f && invuln <= 0)
            {
                HitPlayer(p.pos);
                pop = true;
            }
            if (!pop) continue;
            if (p.friendly) Burst(p.pos, 8);
            MooArt.Kill(p.model.gameObject);
            projectiles.RemoveAt(i);
        }
    }

    public int BossPhase => boss == null ? 0 : boss.hp > 13 ? 1 : boss.hp > 6 ? 2 : 3;

    private void UpdateBoss(float dt)
    {
        if (boss == null || !boss.active) return;
        boss.hurt -= dt;
        boss.timer -= dt;
        int phase = BossPhase;
        Vector2 to = playerPos - boss.pos;
        switch (boss.mode)
        {
            case "idle":
                if (to.magnitude > 2.6f) MoveCircle(ref boss.pos, to.normalized * 1.8f * dt, BossRadius, false);
                if (to.sqrMagnitude > .01f) boss.dir = to.normalized;
                if (boss.timer <= 0)
                {
                    boss.mode = "windup";
                    boss.timer = phase == 3 ? .6f : .9f;
                    Float("!", boss.pos, MooData.Hex("ff6b5a"), true);
                    Play(bleatClip, .7f);
                }
                break;
            case "windup":
                if (to.sqrMagnitude > .01f) boss.dir = to.normalized;
                if (boss.timer <= 0)
                {
                    boss.mode = "charge";
                    boss.timer = 1.8f;
                }
                break;
            case "charge":
                if (MoveCircle(ref boss.pos, boss.dir * (phase == 3 ? 12.5f : 10.5f) * dt, BossRadius, false))
                {
                    DizzyBoss(2.4f);
                    Float("BONK! Dizzy!", boss.pos, MooData.Hex("ffe066"), true);
                    Burst(boss.pos, 16);
                    Shake(.8f);
                    Play(bonkClip);
                }
                else if (boss.timer <= 0)
                {
                    boss.mode = "recover";
                    boss.timer = .6f;
                }
                break;
            default:
                if (boss.timer <= 0) AfterCharge(phase);
                break;
        }
        if (boss.mode != "dizzy" && to.magnitude < (boss.mode == "charge" ? 1.7f : 1.4f)) HitPlayer(boss.pos);
    }

    private void AfterCharge(int phase)
    {
        if (phase >= 2) KickCans(phase == 2 ? 3 : 5);
        if (phase == 3)
        {
            int awake = 0;
            foreach (Critter c in critters) if (!c.giggling) awake++;
            if (awake < 2)
            {
                Vector2 spot = boss.pos + Random.insideUnitCircle.normalized * 3;
                if (!Overlaps(spot, CritterRadius, false))
                {
                    SpawnCritter(0, spot);
                    Float("Get 'em, bunnies!", boss.pos, Color.white);
                }
            }
        }
        boss.mode = "idle";
        boss.timer = phase == 1 ? 1.8f : 1.3f;
    }

    private void KickCans(int count)
    {
        Vector2 aim = (playerPos - boss.pos).normalized;
        for (int i = 0; i < count; i++)
        {
            float angle = (i - (count - 1) / 2f) * 22 * Mathf.Deg2Rad;
            var dir = new Vector2(aim.x * Mathf.Cos(angle) - aim.y * Mathf.Sin(angle), aim.x * Mathf.Sin(angle) + aim.y * Mathf.Cos(angle));
            var can = MooArt.Root("Tin can", world);
            MooArt.Part(can, Shape.Cylinder, Vector3.zero, new Vector3(.4f, .5f, .4f), MooData.Hex("c8ccd4"), new Vector3(0, 0, 90));
            MooArt.Part(can, Shape.Cylinder, Vector3.zero, new Vector3(.42f, .25f, .42f), MooData.Hex("ff6b5a"), new Vector3(0, 0, 90));
            projectiles.Add(new Projectile { pos = boss.pos + dir, vel = dir * 6.5f, life = 2.5f, model = can });
        }
        Float("Have some tin cans! Bleh!", boss.pos, Color.white);
        Play(bonkClip, .5f);
    }

    public void DizzyBoss(float seconds)
    {
        if (boss == null || !boss.active) return;
        boss.timer = Mathf.Max(boss.mode == "dizzy" ? boss.timer : 0, seconds);
        boss.mode = "dizzy";
    }

    public void HurtBoss(int amount)
    {
        if (boss == null || !boss.active || boss.hurt > 0) return;
        if (boss.mode == "dizzy") amount *= 2;
        int before = BossPhase;
        boss.hp -= amount;
        boss.hurt = .3f;
        Shake(.25f);
        Play(lowGiggle);
        if (boss.hp <= 0)
        {
            BossDefeated();
            return;
        }
        Float(Pick(MooData.BossHurt), boss.pos, MooData.Hex("ffd0e6"));
        if (BossPhase != before)
            Float("Grumbleweed is getting grumpier... and gigglier!", boss.pos + Vector2.up, MooData.Hex("ff9ccf"), true);
        state.message = "Grumbleweed's grumpiness: " + boss.hp + " / " + BossHp;
    }

    private void BossDefeated()
    {
        boss.hp = 0;
        boss.active = false;
        boss.mode = "happy";
        foreach (Projectile p in projectiles) MooArt.Kill(p.model.gameObject);
        projectiles.Clear();
        foreach (Critter c in critters) { c.giggling = true; c.happy = 1; }
        Burst(boss.pos, 30);
        Shake(1f);
        Say(MooData.BossEnding, Victory);
    }

    public void Victory()
    {
        save.finished = true;
        save.pieces = 7;
        if (save.moonies >= MooData.HatGoal) save.crown = true;
        Save();
        state.status = "victory";
        state.message = "You did it! The Golden Cowbell is whole and the herd can moo again!";
        partyTime = 0;
        PlayMusic(MooMusic.Track.Party);
        Play(bigMoo);
        if (hero != null) hero.Crown.gameObject.SetActive(save.crown);
        if (boss != null && boss.model != null) dancers.Add(boss.model);
        string[] names = { "Bessie", "Buttercup", "Moo-donna", "Sir Moos-a-Lot" };
        for (int i = 0; i < names.Length; i++)
        {
            float angle = (i + .5f) * Mathf.PI * 2 / names.Length;
            Transform cow = MooArt.Cow(names[i], world, Confetti[i], true, i == 0);
            cow.localPosition = World(playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3.5f);
            dancers.Add(cow);
            AddShadow(cow, 1.7f);
        }
    }

    private void BackToFarm()
    {
        FadeThen(() =>
        {
            giggles = Hero.MaxGiggles;
            EnterArea(0, -1);
            Say(new[] { "Bessie|" + MooData.Hints[4] }, null);
        });
    }

    // ---------- Effects ----------

    public void Float(string text, Vector2 at, Color color, bool big = false)
    {
        floats.Add(new FloatText { pos = World(at, 2.4f), text = text, color = color, life = big ? 2.2f : 1.4f, big = big });
        if (floats.Count > 12) floats.RemoveAt(0);
    }

    private void Ring(Vector2 at, float radius, Color color)
    {
        Transform ring = MooArt.Part(world, Shape.Cylinder, World(at, .1f), new Vector3(0, .04f, 0), color, default, true);
        effects.Add(new Effect { t = ring, life = .45f, size = radius * 2 });
    }

    private void Puff(Vector2 at)
    {
        if (world == null || areaIndex < 0) return;
        Color dust = At(TileAt(at)) == '~' ? MooData.Hex("a07a52") : Color.Lerp(MooData.Areas[areaIndex].Ground, Color.white, .55f);
        Transform bit = MooArt.Part(world, Shape.Ball, World(at + Random.insideUnitCircle * .2f, .15f), Vector3.one * Random.Range(.16f, .26f), dust);
        bits.Add(new Bit { t = bit, vel = new Vector3(Random.Range(-.4f, .4f), Random.Range(1.2f, 2f), Random.Range(-.4f, .4f)), life = .3f, spin = false });
    }

    private void Burst(Vector2 at, int count)
    {
        if (world == null) return;
        for (int i = 0; i < count; i++)
        {
            Transform bit = MooArt.Part(world, Shape.Cube, World(at, .8f), Vector3.one * .14f, Confetti[i % Confetti.Length], default, true);
            Vector2 spread = Random.insideUnitCircle * 4;
            bits.Add(new Bit { t = bit, vel = new Vector3(spread.x, Random.Range(4f, 7f), spread.y), life = 1.1f });
        }
    }
}
