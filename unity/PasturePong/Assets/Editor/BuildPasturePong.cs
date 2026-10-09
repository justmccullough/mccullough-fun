using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildPasturePong
{
    public static void Build()
    {
        Validate();
        for (int i = 1; i < 3; i++)
            if (PasturePong.CpuSpeed[i] <= PasturePong.CpuSpeed[i - 1] ||
                PasturePong.CpuReaction[i] >= PasturePong.CpuReaction[i - 1] ||
                PasturePong.CpuError[i] >= PasturePong.CpuError[i - 1])
                throw new Exception("Difficulty must increase speed, reaction, and accuracy.");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/Pasture.unity");
        PlayerSettings.companyName = "McCullough";
        PlayerSettings.productName = "Pasture Pong";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.initialMemorySize = 32;
        PlayerSettings.WebGL.maximumMemorySize = 256;
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../public/unity/pong"));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Pasture.unity" },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Unity Web build failed: " + report.summary.result);
        using (var sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(File.ReadAllBytes(Path.Combine(output, "Build/pong.wasm")));
            string version = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            File.WriteAllText(Path.Combine(output, "version.json"), "{\"version\":\"" + version + "\"}");
        }
        Debug.Log("Pasture Pong built successfully. Difficulty profile checks passed.");
    }

    public static void Validate()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var game = new GameObject("Game checks").AddComponent<PasturePong>();
        Call(game, "Start");
        var state = Get<PasturePong.State>(game, "state");
        for (int level = 0; level < 3; level++)
        {
            game.SetDifficulty(level.ToString());
            Require(state.difficulty == level && state.status == "playing", "Difficulty selection");
            Set(game, "serveDelay", 0f);
            Set(game, "reaction", 0f);
            Set(game, "ball", new Vector2(3, 3));
            Set(game, "velocity", new Vector2(6, 0));
            Call(game, "FixedUpdate");
            Require(Mathf.Abs(Get<float>(game, "cpuY") - PasturePong.CpuSpeed[level] * Time.fixedDeltaTime) < .001f,
                "CPU movement speed for level " + level);
        }
        game.Restart("");
        game.SetDirection("1");
        for (int i = 0; i < 100; i++) Call(game, "FixedUpdate");
        Require(Get<float>(game, "playerY") <= PasturePong.HalfHeight - PasturePong.PaddleHalf, "Paddle bounds");
        game.SetPaused("1");
        Vector2 pausedBall = Get<Vector2>(game, "ball");
        Call(game, "FixedUpdate");
        Require(Get<Vector2>(game, "ball") == pausedBall && state.status == "paused", "Pause freezes simulation");
        game.Restart("");
        Set(game, "serveDelay", 0f);
        Set(game, "ball", new Vector2(-6.75f, 0));
        Set(game, "velocity", new Vector2(-7, 0));
        Call(game, "FixedUpdate");
        Require(Get<Vector2>(game, "velocity").x > 0 && state.rally == 1, "Player paddle collision");
        Set(game, "ball", new Vector2(0, 4.4f));
        Set(game, "velocity", new Vector2(2, 4));
        Call(game, "FixedUpdate");
        Require(Get<Vector2>(game, "velocity").y < 0, "Top fence collision");
        game.Restart("");
        game.SetMuted("1");
        for (int i = 0; i < PasturePong.WinningScore; i++) Call(game, "Score", true);
        Require(state.player == 7 && state.status == "gameover", "Player victory at seven");
        game.Restart("");
        for (int i = 0; i < PasturePong.WinningScore; i++) Call(game, "Score", false);
        Require(state.cpu == 7 && state.status == "gameover", "CPU victory at seven");
        game.Restart("");
        Require(state.player == 0 && state.cpu == 0 && state.rally == 0 && state.status == "playing", "Restart clears match");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Debug.Log("Pasture Pong gameplay checks passed: CPU levels, bounds, pause, bounces, both winners, restart.");
    }

    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Get<T>(PasturePong game, string name) => (T)typeof(PasturePong).GetField(name, Private).GetValue(game);
    private static void Set(PasturePong game, string name, object value) => typeof(PasturePong).GetField(name, Private).SetValue(game, value);
    private static void Call(PasturePong game, string name, params object[] values) => typeof(PasturePong).GetMethod(name, Private).Invoke(game, values);
    private static void Require(bool condition, string check)
    {
        if (!condition) throw new Exception("Game check failed: " + check);
    }
}
