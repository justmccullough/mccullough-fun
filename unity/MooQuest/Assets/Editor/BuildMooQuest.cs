using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildMooQuest
{
    public static void Build()
    {
        Validate();
        IncludeShaders("Legacy Shaders/Diffuse", "Unlit/Color");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/Farm.unity");
        PlayerSettings.companyName = "McCullough";
        PlayerSettings.productName = "Moo Quest";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.initialMemorySize = 32;
        PlayerSettings.WebGL.maximumMemorySize = 256;
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../public/unity/mooquest"));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Farm.unity" },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Unity Web build failed: " + report.summary.result);
        using (var sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(File.ReadAllBytes(Path.Combine(output, "Build/mooquest.wasm")));
            string version = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            File.WriteAllText(Path.Combine(output, "version.json"), "{\"version\":\"" + version + "\"}");
        }
        Debug.Log("Moo Quest built successfully. All gameplay checks passed.");
    }

    private static void IncludeShaders(params string[] names)
    {
        var settings = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
        SerializedProperty list = settings.FindProperty("m_AlwaysIncludedShaders");
        foreach (string name in names)
        {
            Shader shader = Shader.Find(name);
            if (shader == null) throw new Exception("Missing shader " + name);
            bool found = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) found = true;
            if (found) continue;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
        }
        settings.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    public static void Validate()
    {
        CheckData();
        string oldKey = MooQuest.SaveKey;
        MooQuest.SaveKey = "MooQuestEditorChecks";
        PlayerPrefs.DeleteKey(MooQuest.SaveKey);
        try
        {
            CheckGame();
        }
        finally
        {
            PlayerPrefs.DeleteKey(MooQuest.SaveKey);
            MooQuest.SaveKey = oldKey;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Debug.Log("Moo Quest gameplay checks passed: characters, maps, select, gates, walls, tickles, specials, puzzles, rescue, saving, boss, pause.");
    }

    private static void CheckData()
    {
        MooCharacter[] c = MooData.Characters;
        Require(c.Length == 4 && c[0].Name == "Kaite" && c[1].Name == "Laura" && c[2].Name == "Grace" && c[3].Name == "Audrey", "Four heroes");
        foreach (MooCharacter other in c)
        {
            if (other != c[1]) Require(c[1].Speed > other.Speed, "Laura is fastest");
            if (other != c[2]) Require(c[2].Power > other.Power, "Grace is strongest");
            if (other != c[3]) Require(c[3].MaxGiggles > other.MaxGiggles, "Audrey has the most giggles");
        }
        int coins = 0;
        for (int a = 0; a < MooData.Areas.Length; a++)
        {
            MooArea area = MooData.Areas[a];
            int signs = 0;
            foreach (string row in area.Map)
            {
                Require(row.Length == area.Map[0].Length, area.Name + " map is rectangular");
                foreach (char ch in row)
                {
                    if (ch == '$') coins++;
                    if (ch == 'S') signs++;
                    Require("#Tow~RSNqCcGBDkm$PE1234.".IndexOf(ch) >= 0, area.Name + " uses known tiles ('" + ch + "')");
                }
            }
            Require(signs <= area.Signs.Length, area.Name + " has text for every sign");
            string all = string.Concat(area.Map);
            Require(Count(all, 'P') == 1, area.Name + " has one start");
            if (a == 0) Require(all.Contains("1") && all.Contains("2") && all.Contains("3") && all.Contains("4") && all.Contains("N"), "Barnyard gates and Bessie");
            else Require(Count(all, 'E') == 1, area.Name + " has an exit");
            if (a >= 1 && a <= 3) Require(Count(all, 'C') == 1 && area.Rescue != null && area.Critter >= 0, area.Name + " has a cow to rescue");
            if (a == 4) Require(Count(all, 'G') == 1, "Hilltop has Grumbleweed");
        }
        Require(coins >= MooData.HatGoal, "Enough Moo-nies for the crown");
        string[] hub = MooData.Areas[0].Map;
        foreach (Vector2Int spot in MooData.HubCowSpots)
            Require(hub[hub.Length - 1 - spot.y][spot.x] == '.', "Rescued cows have room in the barnyard");
    }

    private static void CheckGame()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var game = new GameObject("Game checks").AddComponent<MooQuest>();
        game.Init();
        game.SetMuted("1");
        Require(game.Status == "title" && !game.HasSave, "Title screen on first visit");
        game.actionPressed = true;
        game.Tick(.02f);
        Require(game.Status == "select", "Space opens character select");
        game.rightPressed = true;
        game.Tick(.02f);
        Require(game.selected == 1, "Arrow keys change hero");
        game.actionPressed = true;
        game.Tick(.02f);
        Settle(game);
        Require(game.Hero.Name == "Laura" && game.areaIndex == 0 && game.Status == "playing", "Choosing Laura starts the intro then the barnyard");
        Require(game.giggles == game.Hero.MaxGiggles && game.HasSave, "Fresh giggles and an auto-save");

        // Gates and walls.
        Vector2Int gate2 = game.Find('2'), gate1 = game.Find('1');
        Require(game.Solid(gate2, true) && !game.Solid(gate1, true), "Only the meadow gate starts open");
        Vector2 p = MooQuest.Center(new Vector2Int(1, 2));
        game.MoveCircle(ref p, new Vector2(-5, 0), MooQuest.PlayerRadius, true);
        Require(p.x >= 1.49f, "Fences stop the hero");
        game.playerPos = MooQuest.Center(gate1);
        game.Tick(.02f);
        Settle(game);
        Require(game.areaIndex == 1 && game.Status == "playing", "Walking through an open gate enters Clover Meadow");

        // Tickling and getting tickled.
        MooQuest.Critter bun = game.critters[0];
        Isolate(game, bun);
        bun.pos = game.playerPos + game.facing;
        for (int i = 0; i < 4 && !bun.giggling; i++)
        {
            game.attackCooldown = 0;
            game.Tickle();
        }
        Require(bun.giggling, "Tickles make critters giggle away");
        game.Tick(1.2f);
        Require(!game.critters.Contains(bun), "Giggling critters run off");
        game.EnterArea(1, -1);
        game.giggles = game.Hero.MaxGiggles;
        MooQuest.Critter frog = game.critters[0];
        Isolate(game, frog);
        frog.pos = game.playerPos + new Vector2(.6f, 0);
        game.invuln = 0;
        game.Tick(.02f);
        Require(game.giggles == game.Hero.MaxGiggles - 1 && game.invuln > 0, "Critters tickle the hero");
        game.giggles = 0;
        game.Tick(.02f);
        Settle(game);
        Require(game.areaIndex == 0 && game.giggles == game.Hero.MaxGiggles, "Giggling out returns to the barn with full giggles");

        // Specials.
        for (int hero = 0; hero < 4; hero++)
        {
            game.save.character = hero;
            game.EnterArea(1, -1);
            MooQuest.Critter target = game.critters[0];
            Isolate(game, target);
            game.facing = Vector2.down;
            target.pos = game.playerPos + Vector2.down * 2.2f;
            game.specialCooldown = 0;
            game.Special();
            for (int i = 0; i < 20; i++) game.Tick(.03f);
            bool worked = hero == 3 ? target.sleep > 0 : target.giggling || target.hp < MooData.CritterHp[0];
            Require(worked, MooData.Characters[hero].Special + " works");
            Require(game.specialCooldown > 0, "Specials recharge");
        }
        game.save.character = 1;

        // Marsh hay-bridge puzzle.
        game.save.pieces = 1;
        game.EnterArea(2, -1);
        Vector2Int start = MooQuest.TileAt(game.playerPos), cow = game.Find('C'), bale = game.Find('B');
        Require(!game.Reachable(start, cow), "Marsh water blocks the way at first");
        for (int i = 0; i < 6 && game.At(bale) == 'B'; i++)
        {
            Require(game.PushBale(bale, Vector2Int.down), "Bales can be pushed");
            bale += Vector2Int.down;
        }
        Require(game.At(bale) == '=' && game.Reachable(start, cow), "Pushing a bale into water builds a bridge to Moo-donna");

        // Woods log gate.
        game.save.pieces = 3;
        game.EnterArea(3, -1);
        start = MooQuest.TileAt(game.playerPos);
        cow = game.Find('C');
        Vector2Int door = game.Find('D');
        Require(!game.Reachable(start, cow) && game.Reachable(start, game.Find('k')), "The acorn is reachable but Sir Moos-a-Lot is gated");
        Require(game.Solid(door, true), "Log gates are solid");
        game.playerPos = MooQuest.Center(game.Find('k'));
        game.Tick(.02f);
        Require(game.hasKey, "Golden acorn pickup");
        game.playerPos = MooQuest.Center(door + Vector2Int.up);
        game.SetKeys(Vector2.down);
        for (int i = 0; i < 10; i++) game.Tick(.03f);
        game.SetKeys(Vector2.zero);
        Require(game.At(door) == '.' && game.Reachable(start, cow) && !game.hasKey, "The acorn opens the log gate");

        // Rescue + saving.
        game.Rescue();
        Settle(game);
        Require((game.save.pieces & 4) != 0 && game.areaIndex == 0, "Rescuing a cow returns a bell piece");
        game.save.moonies = 5;
        game.Save();
        game.save = new MooQuest.SaveData();
        game.LoadSave();
        Require(game.save.pieces == 7 && game.save.moonies == 5 && game.save.character == 1, "Save and load round trip");
        Require(!game.Solid(game.Find('4'), true), "All three pieces open the hilltop");

        // Moo-nie pickup is remembered.
        Vector2Int coinTile = game.Find('$');
        game.playerPos = MooQuest.Center(coinTile);
        game.Tick(.02f);
        Require(game.save.moonies == 6, "Moo-nie pickup");
        game.EnterArea(0, -1);
        Require(game.At(coinTile) == '.', "Collected Moo-nies stay collected");

        // Pause.
        game.SetPaused("1");
        Vector2 before = game.playerPos;
        game.SetKeys(Vector2.right);
        game.Tick(.1f);
        Require(game.playerPos == before && game.Status == "paused", "Pause freezes the farm");
        game.SetPaused("0");
        game.SetKeys(Vector2.zero);
        Require(game.Status == "playing", "Unpause resumes");

        // Boss.
        game.EnterArea(4, -1);
        Require(game.boss != null && game.Status == "dialogue" && !game.boss.active, "Grumbleweed introduces himself");
        Settle(game);
        Require(game.boss.active && game.Status == "playing", "Boss fight begins");
        game.DizzyBoss(2);
        Require(game.boss.mode == "dizzy", "Bonks make Grumbleweed dizzy");
        for (int i = 0; i < 30 && game.boss.hp > 0; i++)
        {
            game.boss.hurt = 0;
            game.HurtBoss(1);
        }
        Require(game.boss.hp <= 0 && game.Status == "dialogue", "Grumbleweed giggles and makes up");
        Settle(game);
        Require(game.Status == "victory" && game.save.finished && game.save.pieces == 7, "Victory dance party");
        game.partyTime = 3;
        game.actionPressed = true;
        game.Tick(.02f);
        Settle(game);
        Require(game.areaIndex == 0 && game.Status == "playing", "Back to the farm after the party");
        game.EnterArea(4, -1);
        Require(game.boss == null && game.Status == "playing", "Grumbleweed is friendly after the ending");

        game.NewGame("");
        Require(game.Status == "select" && !game.HasSave, "New game clears the save");
        UnityEngine.Object.DestroyImmediate(game.cam.gameObject);
        UnityEngine.Object.DestroyImmediate(game.gameObject);
    }

    // Completes fades and clicks through any dialogue.
    private static void Settle(MooQuest game)
    {
        for (int i = 0; i < 400; i++)
        {
            if (game.fade > 0 || game.Status == "dialogue")
            {
                game.actionPressed = game.Status == "dialogue";
                game.Tick(game.fade > 0 ? .5f : .02f);
            }
            else
            {
                game.Tick(.02f);
                if (game.fade <= 0 && game.Status != "dialogue") return;
            }
        }
        throw new Exception("Game check failed: the game never settled (status " + game.Status + ")");
    }

    private static void Isolate(MooQuest game, MooQuest.Critter keep)
    {
        foreach (MooQuest.Critter c in game.critters)
            if (c != keep) c.pos = new Vector2(-50, -50);
        keep.sleep = keep.stun = 0;
    }

    private static int Count(string text, char ch)
    {
        int n = 0;
        foreach (char c in text) if (c == ch) n++;
        return n;
    }

    private static void Require(bool condition, string check)
    {
        if (!condition) throw new Exception("Game check failed: " + check);
    }
}
