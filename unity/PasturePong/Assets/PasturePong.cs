using System;
using System.Runtime.InteropServices;
using UnityEngine;

public sealed class PasturePong : MonoBehaviour
{
    public const float HalfWidth = 8f, HalfHeight = 4.5f, PaddleHalf = .85f, BallRadius = .19f;
    public const int WinningScore = 7;
    public static readonly float[] CpuSpeed = { 3.0f, 4.8f, 6.6f };
    public static readonly float[] CpuReaction = { .32f, .18f, .08f };
    public static readonly float[] CpuError = { 1.1f, .55f, .16f };

    [Serializable]
    public sealed class State
    {
        public string status = "idle";
        public int player, cpu, difficulty = 1, rally;
        public string message = "A friendly little pasture rivalry.";
    }

    [DllImport("__Internal")] private static extern void PongState(string json);
    private readonly State state = new State();
    private Sprite disc, square;
    private Transform playerCow, cpuCow, hay;
    private readonly Transform[] trail = new Transform[9];
    private readonly Vector3[] trailPositions = new Vector3[9];
    private Vector2 ball, velocity;
    private float playerY, cpuY, targetY, direction, reaction, cpuTarget, error, serveDelay;
    private float speed, trailTimer;
    private bool pointerControl, muted;
    private AudioSource audioSource;
    private AudioClip moo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot() => new GameObject("PasturePong").AddComponent<PasturePong>();

    private void Start()
    {
        Application.targetFrameRate = 60;
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
#endif
        Camera camera = new GameObject("Pasture camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.9f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.backgroundColor = Hex("e8ddbd");
        square = MakeSprite(false);
        disc = MakeSprite(true);
        Shape("Field", Vector2.zero, new Vector2(16, 9), Hex("7e8c5a"), 0);
        for (int i = 0; i < 8; i++)
            Shape("Mown grass", new Vector2(-7 + i * 2, 0), new Vector2(1, 9), Hex("849160"), 1);
        for (int i = -7; i <= 7; i++)
        {
            Shape("Fence top", new Vector2(i, 4.36f), new Vector2(.08f, .25f), Hex("f1e2c6"), 2);
            Shape("Fence bottom", new Vector2(i, -4.36f), new Vector2(.08f, .25f), Hex("f1e2c6"), 2);
        }
        for (int i = -4; i <= 4; i++)
            Shape("Center stitch", new Vector2(0, i), new Vector2(.04f, .45f), Hex("c0c59b"), 2);
        for (int i = 0; i < 24; i++)
            Shape("Daisy", new Vector2(Mathf.Sin(i * 7.31f) * 5.9f, Mathf.Cos(i * 3.17f) * 3.7f),
                Vector2.one * .09f, Hex("f1e2c6"), 2, true);
        playerCow = Cow("Your cow", -7.1f, Hex("c9714f"));
        cpuCow = Cow("Sir Moos-a-Lot", 7.1f, Hex("8a5a6a"));
        for (int i = 0; i < trail.Length; i++)
            trail[i] = Shape("Hay dust", Vector2.zero, Vector2.one * (.23f - i * .018f),
                new Color(1, .83f, .41f, .35f - i * .03f), 3, true);
        hay = Shape("Hay bale", Vector2.zero, Vector2.one * .38f, Hex("e8b64f"), 5);
        Shape("Bale twine", Vector2.zero, new Vector2(.16f, 1), Hex("b57a32"), 6, false, hay);
        audioSource = gameObject.AddComponent<AudioSource>();
        float[] samples = new float[11025];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / 22050f, envelope = Mathf.Sin(Mathf.PI * i / samples.Length);
            samples[i] = Mathf.Sin(t * (110 - t * 65) * Mathf.PI * 2) * envelope * .18f;
        }
        moo = AudioClip.Create("Tiny victory moo", samples.Length, 1, 22050, false);
        moo.SetData(samples, 0);
        ResetRound();
        Publish();
    }

    private Sprite MakeSprite(bool round)
    {
        Texture2D texture = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                texture.SetPixel(x, y, !round || Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) < 31
                    ? Color.white : Color.clear);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.one * .5f, 64);
    }

    private Transform Shape(string name, Vector2 position, Vector2 size, Color color, int layer,
        bool round = false, Transform parent = null)
    {
        GameObject item = new GameObject(name);
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = size;
        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = round ? disc : square;
        renderer.color = color;
        renderer.sortingOrder = layer;
        return item.transform;
    }

    private Transform Cow(string name, float x, Color scarf)
    {
        Transform root = new GameObject(name).transform;
        root.position = new Vector3(x, 0, 0);
        Shape("Body", Vector2.zero, new Vector2(.47f, 1.7f), Hex("fffaf0"), 5, false, root);
        Shape("Spot", new Vector2(-.08f, .18f), new Vector2(.23f, .35f), Hex("4a3526"), 6, true, root);
        Shape("Spot", new Vector2(.1f, -.4f), new Vector2(.2f, .25f), Hex("4a3526"), 6, true, root);
        Shape("Scarf", new Vector2(0, .48f), new Vector2(.5f, .13f), scarf, 7, false, root);
        Shape("Head", new Vector2(0, .7f), new Vector2(.66f, .55f), Hex("fffaf0"), 7, true, root);
        for (int side = -1; side <= 1; side += 2)
        {
            Shape("Ear", new Vector2(side * .34f, .8f), new Vector2(.23f, .14f), Hex("dca28b"), 6, true, root);
            Shape("Horn", new Vector2(side * .2f, 1.0f), new Vector2(.09f, .2f), Hex("e8b64f"), 6, false, root);
            Shape("Eye", new Vector2(side * .13f, .76f), Vector2.one * .065f, Hex("4a3526"), 8, true, root);
        }
        Shape("Snout", new Vector2(0, .58f), new Vector2(.4f, .2f), Hex("dca28b"), 8, true, root);
        return root;
    }

    public void SetDifficulty(string value)
    {
        if (!int.TryParse(value, out int level) || level < 0 || level > 2)
            throw new ArgumentException("Difficulty must be 0, 1, or 2.");
        state.difficulty = level;
        Restart("");
    }

    public void Restart(string unused)
    {
        state.player = state.cpu = state.rally = 0;
        state.status = "playing";
        state.message = "The steaks are low. The rivalry is real.";
        playerY = cpuY = targetY = direction = 0;
        pointerControl = false;
        ResetRound();
        Publish();
    }

    public void SetPaused(string value)
    {
        if (state.status != "playing" && state.status != "paused") return;
        state.status = value == "1" ? "paused" : "playing";
        direction = 0;
        Publish();
    }

    public void SetDirection(string value)
    {
        if (!float.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float next) || float.IsNaN(next) || float.IsInfinity(next))
            throw new ArgumentException("Direction must be finite.");
        direction = Mathf.Clamp(next, -1, 1);
        pointerControl = false;
    }

    public void SetPointer(string value)
    {
        if (!float.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float next) || float.IsNaN(next) || float.IsInfinity(next))
            throw new ArgumentException("Pointer must be finite.");
        // Camera includes a small border around the nine-unit playing field.
        targetY = Mathf.Clamp((.5f - next) * 9.8f, -HalfHeight + PaddleHalf, HalfHeight - PaddleHalf);
        pointerControl = true;
    }

    public void SetMuted(string value) => muted = value == "1";

    private void ResetRound()
    {
        ball = velocity = Vector2.zero;
        speed = 6.3f;
        serveDelay = 1.1f;
        reaction = 0;
        error = UnityEngine.Random.Range(-CpuError[state.difficulty], CpuError[state.difficulty]);
        for (int i = 0; i < trail.Length; i++) trailPositions[i] = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (state.status != "playing") return;
        float dt = Time.fixedDeltaTime;
        playerY = pointerControl ? Mathf.MoveTowards(playerY, targetY, 12 * dt)
            : Mathf.Clamp(playerY + direction * 8 * dt, -HalfHeight + PaddleHalf, HalfHeight - PaddleHalf);
        reaction -= dt;
        if (reaction <= 0)
        {
            cpuTarget = velocity.x > 0 ? ball.y + error : 0;
            reaction = CpuReaction[state.difficulty];
        }
        cpuY = Mathf.MoveTowards(cpuY, Mathf.Clamp(cpuTarget, -HalfHeight + PaddleHalf, HalfHeight - PaddleHalf),
            CpuSpeed[state.difficulty] * dt);
        if (serveDelay > 0)
        {
            serveDelay -= dt;
            if (serveDelay <= 0)
            {
                float angle = UnityEngine.Random.Range(-.45f, .45f);
                velocity = new Vector2((UnityEngine.Random.value < .5f ? -1 : 1) * Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            }
            return;
        }
        ball += velocity * dt;
        if (Mathf.Abs(ball.y) > HalfHeight - BallRadius)
        {
            ball.y = Mathf.Sign(ball.y) * (HalfHeight - BallRadius);
            velocity.y = -Mathf.Sign(ball.y) * Mathf.Abs(velocity.y);
        }
        if (velocity.x < 0 && ball.x - BallRadius <= -6.865f && ball.x > -7.5f &&
            Mathf.Abs(ball.y - playerY) <= PaddleHalf + BallRadius)
            Bounce(playerY, 1);
        else if (velocity.x > 0 && ball.x + BallRadius >= 6.865f && ball.x < 7.5f &&
            Mathf.Abs(ball.y - cpuY) <= PaddleHalf + BallRadius)
            Bounce(cpuY, -1);
        if (Mathf.Abs(ball.x) > HalfWidth + BallRadius) Score(ball.x > 0);
    }

    private void Bounce(float y, int heading)
    {
        ball.x = heading * -6.67f;
        speed = Mathf.Min(speed + .45f, 13f);
        float angle = Mathf.Clamp((ball.y - y) / PaddleHalf, -.95f, .95f);
        velocity = new Vector2(heading * Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
        error = UnityEngine.Random.Range(-CpuError[state.difficulty], CpuError[state.difficulty]);
        state.rally++;
        state.message = state.rally >= 8 ? "Udderly ridiculous rally!" : "Keep that hay rolling.";
        Publish();
    }

    private void Score(bool playerPoint)
    {
        if (playerPoint) state.player++; else state.cpu++;
        state.rally = 0;
        state.message = playerPoint ? "Legendairy shot. No bull." : "He milked that one. You've got this.";
        if (!muted) audioSource.PlayOneShot(moo);
        if (state.player >= WinningScore || state.cpu >= WinningScore)
        {
            state.status = "gameover";
            state.message = state.player >= WinningScore ? "You're the cream of the crop!" : "Out-mooed, but never out-loved.";
        }
        ResetRound();
        Publish();
    }

    private void Update()
    {
        playerCow.position = new Vector3(-7.1f, playerY, 0);
        cpuCow.position = new Vector3(7.1f, cpuY, 0);
        hay.position = ball;
        if (state.status == "playing") hay.Rotate(0, 0, -velocity.x * Time.deltaTime * 13);
        trailTimer += Time.deltaTime;
        if (trailTimer > .025f && state.status == "playing")
        {
            trailTimer = 0;
            for (int i = trail.Length - 1; i > 0; i--) trailPositions[i] = trailPositions[i - 1];
            trailPositions[0] = ball;
        }
        for (int i = 0; i < trail.Length; i++) trail[i].position = trailPositions[i];
    }

    private void Publish()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        PongState(JsonUtility.ToJson(state));
#endif
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
}
