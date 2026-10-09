using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

// Moo Quest: a cozy, cow-themed, top-down action RPG. Everything (models, sounds, UI) is generated in code.
public sealed partial class MooQuest : MonoBehaviour
{
    [Serializable]
    public sealed class State
    {
        public string status = "loading", character = "", area = "", message = "Loading the farm...";
        public int giggles, maxGiggles, moonies, pieces;
        public bool hasSave, muted, crown;
    }

    [Serializable]
    public sealed class SaveData
    {
        public int character = -1, pieces, moonies;
        public string collected = "";
        public bool finished, crown;
    }

    public static string SaveKey = "MooQuestSave";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void MooState(string json);
#endif

    public readonly State state = new State();
    public SaveData save = new SaveData();
    public int selected;
    public bool muted;

    public bool actionPressed, specialPressed, pausePressed, leftPressed, rightPressed;
    private Vector2 stick, keyMove;
    private string resumeStatus = "playing", lastJson = "";

    private readonly List<string[]> lines = new List<string[]>();
    private int lineIndex;
    private float reveal;
    private Action dialogueDone;

    public float fade;
    private int fadeDirection;
    private Action fadeAction;

    private AudioSource sfx, music;
    private AudioClip mooClip, bigMoo, giggleClip, highGiggle, lowGiggle, swishClip, dingClip, bonkClip, quackClip,
        sparkleClip, lullabyClip, bleatClip;

    public MooCharacter Hero => MooData.Characters[Mathf.Clamp(save.character, 0, MooData.Characters.Length - 1)];
    public bool HasSave => save.character >= 0 && save.character < MooData.Characters.Length;
    public string Status => state.status;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot() => new GameObject("MooQuest").AddComponent<MooQuest>();

    private void Start() => Init();

    public void Init()
    {
        Application.targetFrameRate = 60;
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
#endif
        cam = new GameObject("Camera").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.fieldOfView = 40;
        cam.nearClipPlane = .3f;
        cam.farClipPlane = 200;
        cam.gameObject.AddComponent<AudioListener>();
        sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(50, -30, 0);
        sun.intensity = 1.05f;
        sun.shadows = LightShadows.None;
        RenderSettings.ambientMode = AmbientMode.Trilight;

        sfx = gameObject.AddComponent<AudioSource>();
        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.volume = .55f;
        mooClip = MooAudio.Moo();
        bigMoo = MooAudio.Moo(.8f);
        bleatClip = MooAudio.Moo(2.4f);
        giggleClip = MooAudio.Giggle();
        highGiggle = MooAudio.Giggle(1.35f);
        lowGiggle = MooAudio.Giggle(.6f);
        swishClip = MooAudio.Swish();
        dingClip = MooAudio.Ding();
        bonkClip = MooAudio.Bonk();
        quackClip = MooAudio.Quack();
        sparkleClip = MooAudio.Sparkle();
        lullabyClip = MooAudio.Lullaby();
        MooMusic.Get(MooMusic.Track.Barnyard); // Other songs are composed the first time they're needed.

        LoadSave();
        ShowTitle();
    }

    // ---------- Commands from the web page (Unity SendMessage) ----------

    public void NewGame(string unused)
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        save = new SaveData();
        fadeDirection = 0;
        fade = 0;
        fadeAction = null;
        ShowSelect();
    }

    public void SetPaused(string value)
    {
        if (value == "1" && (state.status == "playing" || state.status == "dialogue"))
        {
            resumeStatus = state.status;
            state.status = "paused";
            stick = keyMove = Vector2.zero;
        }
        else if (value == "0" && state.status == "paused") state.status = resumeStatus;
        Publish();
    }

    public void SetMuted(string value)
    {
        muted = value == "1";
        AudioListener.volume = muted ? 0 : 1;
        Publish();
    }

    public void SetStick(string value)
    {
        string[] parts = (value ?? "").Split(',');
        if (parts.Length != 2 ||
            !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
            !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
            float.IsNaN(x) || float.IsNaN(y) || float.IsInfinity(x) || float.IsInfinity(y))
        {
            stick = Vector2.zero;
            return;
        }
        stick = Vector2.ClampMagnitude(new Vector2(x, y), 1);
    }

    public void Press(string button)
    {
        switch (button)
        {
            case "action": actionPressed = true; break;
            case "special": specialPressed = true; break;
            case "pause": pausePressed = true; break;
            case "left": leftPressed = true; break;
            case "right": rightPressed = true; break;
        }
    }

    // ---------- Flow ----------

    private void ShowTitle()
    {
        BuildStage();
        selected = HasSave ? save.character : 0;
        state.status = "title";
        state.message = "Welcome to Moo Quest! The Great Cowbell Caper awaits.";
        PlayMusic(MooMusic.Track.Barnyard);
        Publish();
    }

    public void ShowSelect()
    {
        if (stageGirls == null) BuildStage();
        state.status = "select";
        state.message = "Choose your hero: Kaite, Laura, Grace, or Audrey.";
        PlayMusic(MooMusic.Track.Barnyard);
        Publish();
    }

    public void Choose(int index)
    {
        save = new SaveData { character = Mathf.Clamp(index, 0, MooData.Characters.Length - 1) };
        Save();
        Play(bigMoo);
        FadeThen(() =>
        {
            giggles = Hero.MaxGiggles;
            EnterArea(0, -1);
            Say(MooData.Intro, null);
        });
    }

    public void Continue()
    {
        FadeThen(() =>
        {
            giggles = Hero.MaxGiggles;
            EnterArea(0, -1);
            Say(new[] { "Bessie|Welcome back, {name}! " + Hint() }, null);
        });
    }

    public string Hint()
    {
        if (save.finished) return MooData.Hints[4];
        if ((save.pieces & 1) == 0) return MooData.Hints[0];
        if ((save.pieces & 2) == 0) return MooData.Hints[1];
        if ((save.pieces & 4) == 0) return MooData.Hints[2];
        return MooData.Hints[3];
    }

    public bool GateOpen(int gate)
    {
        int p = save.pieces;
        return gate == 1 || save.finished || (gate == 2 && (p & 1) != 0) || (gate == 3 && (p & 2) != 0) || (gate == 4 && p == 7);
    }

    public void FadeThen(Action action)
    {
        if (fadeDirection != 0) return;
        fadeDirection = 1;
        fadeAction = action;
    }

    // ---------- Dialogue ----------

    public void Say(string[] script, Action done)
    {
        lines.Clear();
        foreach (string line in script)
        {
            string text = line.Replace("{name}", HasSave ? Hero.Name : "Friend");
            int bar = text.IndexOf('|');
            lines.Add(bar < 0 ? new[] { "", text } : new[] { text.Substring(0, bar), text.Substring(bar + 1) });
        }
        lineIndex = 0;
        reveal = 0;
        dialogueDone = done;
        state.status = "dialogue";
        AnnounceLine();
    }

    private void AnnounceLine()
    {
        string[] line = lines[lineIndex];
        state.message = line[0].Length > 0 ? line[0] + ": " + line[1] : line[1];
    }

    private void Dialogue(float dt)
    {
        reveal += dt * 60;
        if (!actionPressed) return;
        if (reveal < lines[lineIndex][1].Length)
        {
            reveal = lines[lineIndex][1].Length;
            return;
        }
        lineIndex++;
        reveal = 0;
        if (lineIndex < lines.Count)
        {
            AnnounceLine();
            return;
        }
        state.status = "playing";
        Action done = dialogueDone;
        dialogueDone = null;
        done?.Invoke();
    }

    public string[] CurrentLine => state.status == "dialogue" || (state.status == "paused" && resumeStatus == "dialogue")
        ? lines[Mathf.Min(lineIndex, lines.Count - 1)] : null;

    // ---------- Main loop ----------

    private void Update()
    {
        ReadKeyboard();
        float dt = Mathf.Min(Time.deltaTime, .05f);
        Tick(dt);
        Animate(dt);
    }

    private void ReadKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.E)) actionPressed = true;
        if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftShift) ||
            Input.GetKeyDown(KeyCode.RightShift)) specialPressed = true;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) pausePressed = true;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) leftPressed = true;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) rightPressed = true;
        float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
        float y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
        keyMove = new Vector2(x, y);
    }

    public void SetKeys(Vector2 move) => keyMove = move;

    public Vector2 MoveInput() => Vector2.ClampMagnitude(keyMove.sqrMagnitude > 0 ? keyMove : stick, 1);

    public void Tick(float dt)
    {
        if (pausePressed)
        {
            pausePressed = false;
            SetPaused(state.status == "paused" ? "0" : "1");
        }
        if (state.status == "paused")
        {
            ClearPresses();
            Publish();
            return;
        }
        if (fadeDirection != 0)
        {
            fade += dt * 3.2f * fadeDirection;
            if (fadeDirection > 0 && fade >= 1)
            {
                fade = 1;
                fadeDirection = -1;
                Action action = fadeAction;
                fadeAction = null;
                action?.Invoke();
            }
            else if (fadeDirection < 0 && fade <= 0)
            {
                fade = 0;
                fadeDirection = 0;
            }
            ClearPresses();
            Publish();
            return;
        }
        switch (state.status)
        {
            case "title":
                if (actionPressed)
                {
                    if (HasSave) Continue(); else ShowSelect();
                }
                break;
            case "select":
                if (leftPressed) { selected = (selected + MooData.Characters.Length - 1) % MooData.Characters.Length; Play(swishClip); }
                if (rightPressed) { selected = (selected + 1) % MooData.Characters.Length; Play(swishClip); }
                if (actionPressed) Choose(selected);
                break;
            case "dialogue":
                Dialogue(dt);
                break;
            case "playing":
                PlayStep(dt);
                break;
            case "victory":
                partyTime += dt;
                if (actionPressed && partyTime > 2) BackToFarm();
                break;
        }
        ClearPresses();
        Publish();
    }

    private void ClearPresses() => actionPressed = specialPressed = pausePressed = leftPressed = rightPressed = false;

    // ---------- Saving ----------

    public void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
        PlayerPrefs.Save();
    }

    public void LoadSave()
    {
        save = new SaveData();
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            save = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        }
        catch (Exception)
        {
            save = new SaveData();
        }
        if (!HasSave) save = new SaveData();
        save.pieces &= 7;
        save.moonies = Mathf.Max(0, save.moonies);
        if (save.collected == null) save.collected = "";
    }

    // ---------- Sound + state ----------

    private void Play(AudioClip clip, float volume = 1)
    {
        if (!muted && sfx != null && clip != null && Application.isPlaying) sfx.PlayOneShot(clip, volume);
    }

    private void PlayMusic(MooMusic.Track track)
    {
        if (music == null || !Application.isPlaying) return;
        AudioClip clip = MooMusic.Get(track);
        if (music.clip == clip) return;
        music.clip = clip;
        music.Play();
    }

    private void Publish()
    {
        state.hasSave = HasSave;
        state.character = HasSave ? Hero.Name : "";
        state.area = areaIndex >= 0 && state.status != "title" && state.status != "select" ? MooData.Areas[areaIndex].Name : "";
        state.giggles = giggles;
        state.maxGiggles = HasSave ? Hero.MaxGiggles : 0;
        state.moonies = save.moonies;
        state.pieces = save.pieces;
        state.muted = muted;
        state.crown = save.crown;
        string json = JsonUtility.ToJson(state);
        if (json == lastJson) return;
        lastJson = json;
#if UNITY_WEBGL && !UNITY_EDITOR
        MooState(json);
#endif
    }

    private static string Pick(string[] options) => options[Random.Range(0, options.Length)];
}
