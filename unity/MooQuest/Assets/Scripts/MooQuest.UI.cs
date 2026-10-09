using UnityEngine;

public sealed partial class MooQuest
{
    private static readonly Vector3 CameraOffset = new Vector3(0, 11.5f, -7.5f);
    private float time;
    private Vector3 camVelocity, camBase;
    private GUIStyle titleStyle, bigStyle, textStyle, smallStyle, nameStyle, buttonStyle, centerStyle, floatStyle, labelStyle;
    private Texture2D heart, emptyHeart, bell, emptyBell, coin, acornIcon, round;
    private float ui = 1;

    // ---------- Animation ----------

    private void SnapCamera()
    {
        if (cam == null) return;
        camBase = cam.transform.position = World(playerPos) + CameraOffset;
        cam.transform.LookAt(World(playerPos, .8f));
        camVelocity = Vector3.zero;
    }

    private void Animate(float dt)
    {
        if (state.status == "paused") dt = 0;
        time += dt;
        if (stageGirls != null) AnimateStage(dt);
        if (hero != null) AnimateHero();
        foreach (Critter c in critters) AnimateCritter(c);
        if (boss != null && boss.model != null && state.status != "victory") AnimateBoss();
        foreach (Npc n in npcs.Values)
            if (n.model != null && n.model.childCount > 0)
            {
                float breathe = 1 + Mathf.Sin(time * 2 + n.tile.x) * .025f;
                n.model.GetChild(0).localScale = new Vector3(1, breathe, 1);
            }
        for (int i = bobs.Count - 1; i >= 0; i--)
        {
            Bob b = bobs[i];
            if (b.t == null) { bobs.RemoveAt(i); continue; }
            b.t.localPosition = b.home + Vector3.up * Mathf.Sin(time * 2.5f + b.phase) * b.height;
            if (b.spin != 0) b.t.Rotate(0, b.spin * dt, 0, Space.World);
        }
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            Effect e = effects[i];
            e.time += dt;
            float k = e.time / e.life;
            if (k >= 1 || e.t == null) { if (e.t != null) MooArt.Kill(e.t.gameObject); effects.RemoveAt(i); continue; }
            float size = Mathf.Lerp(.5f, e.size, Mathf.Sqrt(k));
            e.t.localScale = new Vector3(size, .04f, size);
        }
        for (int i = bits.Count - 1; i >= 0; i--)
        {
            Bit b = bits[i];
            b.life -= dt;
            if (b.life <= 0 || b.t == null) { if (b.t != null) MooArt.Kill(b.t.gameObject); bits.RemoveAt(i); continue; }
            b.vel += Vector3.down * (b.spin ? 14 : 3) * dt;
            b.t.localPosition += b.vel * dt;
            if (b.spin) b.t.Rotate(400 * dt, 300 * dt, 0);
            else b.t.localScale *= 1 + dt * 2.5f;
        }
        for (int i = slides.Count - 1; i >= 0; i--)
        {
            Slide s = slides[i];
            if (s.t == null) { slides.RemoveAt(i); continue; }
            Vector3 target = s.target + (s.flatten ? Vector3.down * .85f : Vector3.zero);
            s.t.localPosition = Vector3.MoveTowards(s.t.localPosition, target, 8 * Mathf.Max(dt, .02f));
            if ((s.t.localPosition - target).sqrMagnitude < .0001f) slides.RemoveAt(i);
        }
        foreach (Projectile p in projectiles)
            if (p.model != null)
            {
                p.model.localPosition = World(p.pos, .5f);
                p.model.rotation = Quaternion.LookRotation(new Vector3(p.vel.x, 0, p.vel.y)) * Quaternion.Euler(time * 720, 0, 0);
            }
        for (int i = floats.Count - 1; i >= 0; i--)
        {
            floats[i].time += dt;
            if (floats[i].time > floats[i].life) floats.RemoveAt(i);
        }
        if (state.status == "victory") AnimateParty();
        // Blob shadows stay on the floor and shrink a little as their owner hops up.
        for (int i = blobs.Count - 1; i >= 0; i--)
        {
            Blob b = blobs[i];
            if (b.follow == null || b.t == null) { if (b.t != null) MooArt.Kill(b.t.gameObject); blobs.RemoveAt(i); continue; }
            Vector3 p = b.follow.position;
            float s = b.size * (1 - Mathf.Clamp01((p.y - b.floor) / 2.5f) * .45f);
            b.t.position = new Vector3(p.x, b.floor, p.z);
            b.t.localScale = new Vector3(s, .01f, s);
            b.t.gameObject.SetActive(b.follow.gameObject.activeInHierarchy);
        }
        if (areaIndex >= 0 && cam != null)
        {
            Vector3 focus = World(playerPos);
            Vector3 offset = state.status == "victory"
                ? Quaternion.Euler(0, partyTime * 12, 0) * new Vector3(0, 9, -9)
                : CameraOffset;
            camBase = Vector3.SmoothDamp(camBase, focus + offset, ref camVelocity, .25f, 100, Mathf.Max(dt, .001f));
            cam.transform.position = camBase;
            cam.transform.LookAt(focus + Vector3.up * .8f);
            if (shake > 0)
            {
                cam.transform.position += Random.insideUnitSphere * shake * .3f;
                shake = Mathf.MoveTowards(shake, 0, dt * 2.5f);
            }
        }
    }

    private void AnimateStage(float dt)
    {
        for (int i = 0; i < stageGirls.Length; i++)
        {
            MooArt.Rig g = stageGirls[i];
            bool chosen = state.status == "select" && i == selected;
            float hop = chosen ? Mathf.Abs(Mathf.Sin(time * 6)) * .3f : Mathf.Sin(time * 2 + i) * .03f;
            g.Root.localPosition = new Vector3(-3.6f + i * 2.4f, .4f + hop, 0);
            float turn = chosen ? 180 + Mathf.Sin(time * 3) * 20 : 180 + Mathf.Sin(time + i * 2) * 10;
            g.Root.localRotation = Quaternion.Slerp(g.Root.localRotation, Quaternion.Euler(0, turn, 0), 10 * Mathf.Max(dt, .02f));
            float wave = chosen ? -140 + Mathf.Sin(time * 10) * 25 : Mathf.Sin(time * 2 + i) * 6;
            g.RightArm.localRotation = Quaternion.Euler(0, 0, wave);
            g.LeftArm.localRotation = Quaternion.Euler(0, 0, chosen ? 20 : -Mathf.Sin(time * 2 + i) * 6);
            g.Root.localScale = Vector3.one * (chosen ? 1.12f : 1);
        }
        if (spotlight != null)
        {
            spotlight.gameObject.SetActive(state.status == "select");
            spotlight.localPosition = Vector3.Lerp(spotlight.localPosition, new Vector3(-3.6f + selected * 2.4f, .42f, 0), 12 * Mathf.Max(dt, .02f));
        }
    }

    private void AnimateHero()
    {
        Transform root = hero.Root;
        root.localPosition = World(playerPos);
        float angle = Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg;
        if (spin > 0) angle += (1 - spin / .5f) * 720;
        root.localRotation = Quaternion.Euler(0, angle, 0);
        bool moving = walking && state.status == "playing";
        float stride = moving ? Mathf.Sin(time * 14) * 35 : 0;
        hero.LeftLeg.localRotation = Quaternion.Euler(stride, 0, 0);
        hero.RightLeg.localRotation = Quaternion.Euler(-stride, 0, 0);
        hero.LeftArm.localRotation = Quaternion.Euler(-stride * .7f, 0, 0);
        bool tickling = swing > 0;
        hero.Feather.gameObject.SetActive(tickling || spin > 0);
        hero.RightArm.localRotation = tickling
            ? Quaternion.Euler(-80 + Mathf.Sin(swing * 60) * 30, 0, 0)
            : Quaternion.Euler(stride * .7f, 0, 0);
        float bob = moving ? Mathf.Abs(Mathf.Sin(time * 14)) * .08f : Mathf.Sin(time * 2) * .02f;
        hero.Body.localPosition = new Vector3(0, bob, 0);
        hero.Body.localScale = dashTime > 0 ? new Vector3(.85f, .9f, 1.3f) : Vector3.one;
        bool blink = invuln > 0 && dashTime <= 0 && Mathf.Repeat(time * 12, 1) < .5f;
        hero.Body.gameObject.SetActive(!blink || state.status != "playing");
        if (gigglingOut) root.localRotation = Quaternion.Euler(0, angle + time * 900, Mathf.Sin(time * 20) * 15);
    }

    private void AnimateCritter(Critter c)
    {
        if (c.model == null) return;
        float hop = c.kind == 1 ? Mathf.Max(0, Mathf.Sin(c.phase * 5)) * .45f : Mathf.Abs(Mathf.Sin(c.phase * 9)) * .15f;
        if (c.sleep > 0) hop = 0;
        c.model.localPosition = World(c.pos, hop);
        float angle = Mathf.Atan2(c.face.x, c.face.y) * Mathf.Rad2Deg;
        if (c.giggling)
        {
            Vector2 away = c.pos - playerPos;
            angle = Mathf.Atan2(away.x, away.y) * Mathf.Rad2Deg;
            c.model.localRotation = Quaternion.Euler(0, angle, Mathf.Sin(time * 40) * 25);
        }
        else if (c.sleep > 0) c.model.localRotation = Quaternion.Euler(0, angle, 70);
        else c.model.localRotation = Quaternion.Euler(0, angle, 0);
        c.model.localScale = c.hurt > 0 ? new Vector3(1.25f, .75f, 1.25f) : Vector3.one;
    }

    private void AnimateBoss()
    {
        Transform m = boss.model;
        float angle = Mathf.Atan2(boss.dir.x, boss.dir.y) * Mathf.Rad2Deg;
        Vector3 pos = World(boss.pos);
        Quaternion rot = Quaternion.Euler(0, angle, 0);
        switch (boss.mode)
        {
            case "windup":
                pos += new Vector3(Mathf.Sin(time * 60) * .08f, 0, 0);
                rot *= Quaternion.Euler(-12, 0, 0);
                break;
            case "charge":
                rot *= Quaternion.Euler(15, 0, 0);
                pos.y += Mathf.Abs(Mathf.Sin(time * 20)) * .2f;
                break;
            case "dizzy":
                rot = Quaternion.Euler(0, angle + time * 300, Mathf.Sin(time * 8) * 12);
                break;
            case "happy":
                rot = Quaternion.Euler(0, angle + time * 200, 0);
                pos.y += Mathf.Abs(Mathf.Sin(time * 8)) * .5f;
                break;
        }
        m.localPosition = pos;
        m.localRotation = rot;
        m.localScale = Vector3.one * (boss.hurt > 0 ? 1.5f : 1.35f);
    }

    private void AnimateParty()
    {
        if (hero != null)
        {
            hero.Root.localPosition = World(playerPos, Mathf.Abs(Mathf.Sin(partyTime * 6)) * .5f);
            hero.Root.localRotation = Quaternion.Euler(0, partyTime * 180, 0);
            hero.RightArm.localRotation = hero.LeftArm.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(partyTime * 10) * 40 + 150);
            hero.Body.gameObject.SetActive(true);
        }
        for (int i = 0; i < dancers.Count; i++)
        {
            if (dancers[i] == null) continue;
            float angle = partyTime * .8f + i * Mathf.PI * 2 / dancers.Count;
            Vector2 at = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3.6f;
            dancers[i].localPosition = World(at, Mathf.Abs(Mathf.Sin(partyTime * 6 + i)) * .6f);
            dancers[i].localRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg + Mathf.Sin(partyTime * 5 + i) * 30, Mathf.Sin(partyTime * 6 + i) * 10);
        }
        if (Mathf.Repeat(partyTime, .5f) < Time.deltaTime * 1.01f && world != null)
            Burst(playerPos + Random.insideUnitCircle * 4, 10);
    }

    // ---------- UI ----------

    private void SetupGui()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
        bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        textStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true, alignment = TextAnchor.UpperLeft };
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, alignment = TextAnchor.UpperLeft };
        nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        centerStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        floatStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
        Texture2D up = Solid(MooData.Hex("ff8fc8")), hover = Solid(MooData.Hex("ffa8d6")), down = Solid(MooData.Hex("e86fae"));
        buttonStyle.normal.background = up; buttonStyle.hover.background = hover; buttonStyle.active.background = down;
        buttonStyle.focused.background = up;
        buttonStyle.normal.textColor = buttonStyle.hover.textColor = buttonStyle.active.textColor = buttonStyle.focused.textColor = Color.white;
        Color ink = MooData.Hex("3b2a20");
        heart = Icon(32, (x, y) => HeartShape(x * 1.12f, y * 1.12f), MooData.Hex("ff6fa8"), ink);
        emptyHeart = Icon(32, (x, y) => HeartShape(x * 1.12f, y * 1.12f), new Color(1, 1, 1, .2f), new Color(1, 1, 1, .45f));
        round = Icon(32, (x, y) => x * x + y * y < 1, Color.white);
        coin = Icon(32, (x, y) => x * x + y * y < .8f && !(x * x + y * y < .4f && x * x + y * y > .25f), MooArt.Gold, ink);
        bell = Icon(32, BellShape, MooArt.Gold, ink);
        emptyBell = Icon(32, BellShape, new Color(1, 1, 1, .15f), new Color(1, 1, 1, .45f));
        acornIcon = Icon(32, (x, y) => (x * x + (y + .15f) * (y + .15f) * 1.4f < .45f) || (y > .25f && y < .55f && Mathf.Abs(x) < .72f), MooArt.Gold, ink);
    }

    // Domed top, flared skirt, rim, and clapper, so it reads as a bell rather than a pine tree.
    private static bool BellShape(float x, float y) =>
        (y > .2f && x * x + (y - .2f) * (y - .2f) < .2f) ||
        (y <= .2f && y > -.5f && Mathf.Abs(x) < .45f + (.2f - y) * .3f) ||
        (y <= -.5f && y > -.64f && Mathf.Abs(x) < .78f) ||
        (x * x + (y + .76f) * (y + .76f) < .02f) ||
        (y > .6f && y < .8f && Mathf.Abs(x) < .09f);

    private static bool HeartShape(float x, float y)
    {
        y = -y * 1.1f + .25f;
        float a = x * x + y * y - .6f;
        return a * a * a - x * x * y * y * y < 0;
    }

    private static Texture2D Solid(Color color)
    {
        var t = new Texture2D(1, 1) { hideFlags = HideFlags.DontSave };
        t.SetPixel(0, 0, color);
        t.Apply();
        return t;
    }

    // Rasterizes a shape into a tiny icon, optionally with a dark outline so it pops on any background.
    private static Texture2D Icon(int size, System.Func<float, float, bool> inside, Color color, Color? outline = null)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear };
        var clear = new Color(color.r, color.g, color.b, 0);
        float o = 2.2f / size;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = (x + .5f) / size * 2 - 1, fy = (y + .5f) / size * 2 - 1;
                Color pixel = clear;
                if (inside(fx, fy)) pixel = color;
                else if (outline.HasValue)
                    for (int k = 0; k < 8 && pixel.a == 0; k++)
                    {
                        float a = k * Mathf.PI / 4;
                        if (inside(fx + Mathf.Cos(a) * o * 2, fy + Mathf.Sin(a) * o * 2)) pixel = outline.Value;
                    }
                t.SetPixel(x, y, pixel);
            }
        t.Apply();
        return t;
    }

    private void Box(Rect r, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void Panel(Rect r, Color color)
    {
        Box(new Rect(r.x + 4, r.y + 5, r.width, r.height), new Color(0, 0, 0, .18f));
        Box(r, color);
        Box(new Rect(r.x, r.y, r.width, 4), new Color(1, 1, 1, .5f));
    }

    private static void Shadowed(Rect r, string text, GUIStyle style, Color color)
    {
        style.normal.textColor = new Color(0, 0, 0, .45f * color.a);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        style.normal.textColor = color;
        GUI.Label(r, text, style);
    }

    private bool Button(Rect r, string text)
    {
        Box(new Rect(r.x + 3, r.y + 4, r.width, r.height), new Color(0, 0, 0, .2f));
        return GUI.Button(r, text, buttonStyle);
    }

    private Vector2 ToGui(Vector3 world, out bool visible)
    {
        Vector3 p = cam.WorldToScreenPoint(world);
        visible = p.z > 0;
        return new Vector2(p.x / ui, (Screen.height - p.y) / ui);
    }

    private void OnGUI()
    {
        if (cam == null) return;
        SetupGui();
        ui = Mathf.Max(.5f, Screen.height / 540f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(ui, ui, 1));
        float w = Screen.width / ui, h = 540;

        switch (state.status)
        {
            case "title": TitleScreen(w, h); break;
            case "select": SelectScreen(w, h); break;
            default:
                if (areaIndex >= 0)
                {
                    WorldLabels();
                    if (state.status != "victory") Hud(w, h);
                }
                break;
        }
        if (CurrentLine != null) DialogueBox(w, h);
        if (state.status == "victory") VictoryScreen(w, h);
        if (state.status == "paused") PauseScreen(w, h);
        if (fade > 0) Box(new Rect(0, 0, w, h), new Color(.16f, .1f, .08f, Mathf.Clamp01(fade)));
    }

    private void TitleScreen(float w, float h)
    {
        float wob = Mathf.Sin(time * 2) * 4;
        Shadowed(new Rect(0, 30 + wob, w, 90), "MOO QUEST", titleStyle, Color.white);
        Shadowed(new Rect(0, 108, w, 40), "The Great Cowbell Caper", bigStyle, MooData.Hex("ffe066"));
        float bw = 260, y = h - 150;
        if (HasSave)
        {
            if (Button(new Rect(w / 2 - bw - 10, y, bw, 52), "Continue as " + Hero.Name)) Continue();
            if (Button(new Rect(w / 2 + 10, y, bw, 52), "New game")) NewGame("");
        }
        else if (Button(new Rect(w / 2 - bw / 2, y, bw, 52), "Start the adventure!")) ShowSelect();
        Box(new Rect(0, h - 86, w, 70), new Color(.22f, .15f, .12f, .35f));
        if (Mathf.Repeat(time, 1.2f) < .8f)
            Shadowed(new Rect(0, h - 80, w, 30), "Press SPACE to start", centerStyle, Color.white);
        Shadowed(new Rect(0, h - 48, w, 30), "Tickle critters, rescue cows, and outsmart a very grumpy goat.", smallStyleCentered(), new Color(1, 1, 1, .9f));
    }

    private GUIStyle smallCentered;
    private GUIStyle smallStyleCentered() => smallCentered ?? (smallCentered = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter });

    private void SelectScreen(float w, float h)
    {
        Shadowed(new Rect(0, 14, w, 44), "Who's going on the Moo Quest?", bigStyle, Color.white);
        for (int i = 0; i < stageGirls.Length; i++)
        {
            Vector2 head = ToGui(stageGirls[i].Root.position + Vector3.up * 2.2f, out _);
            Vector2 feet = ToGui(stageGirls[i].Root.position, out _);
            var hit = new Rect(head.x - 55, head.y, 110, feet.y - head.y + 10);
            if (GUI.Button(hit, GUIContent.none, GUIStyle.none))
            {
                if (selected == i) Choose(i);
                else { selected = i; Play(swishClip); }
            }
            Shadowed(new Rect(head.x - 70, head.y - 30, 140, 30), MooData.Characters[i].Name, nameStyle,
                i == selected ? MooData.Hex("ffe066") : Color.white);
        }
        MooCharacter c = MooData.Characters[selected];
        float pw = Mathf.Min(640, w - 40), px = (w - pw) / 2, py = h - 200;
        Panel(new Rect(px, py, pw, 186), new Color(.22f, .15f, .12f, .88f));
        Shadowed(new Rect(px + 18, py + 10, 300, 32), c.Name, new GUIStyle(nameStyle) { alignment = TextAnchor.MiddleLeft, fontSize = 26 }, c.Hair == MooData.Hex("5b3a23") ? MooData.Hex("e0b48a") : c.Hair);
        Shadowed(new Rect(px + 18, py + 40, pw - 36, 22), c.Looks + "  ·  " + c.Tagline, smallStyle, Color.white);
        Stat(px + 18, py + 74, "Speed", c.Speed / 7f);
        Stat(px + 18, py + 98, "Tickle power", c.Power / 2f);
        Stat(px + 18, py + 122, "Giggles", c.MaxGiggles / 6f);
        Shadowed(new Rect(px + 300, py + 70, pw - 318, 26), "Special: " + c.Special, new GUIStyle(smallStyle) { fontStyle = FontStyle.Bold }, MooData.Hex("ffe066"));
        Shadowed(new Rect(px + 300, py + 94, pw - 318, 50), c.SpecialInfo, smallStyle, Color.white);
        if (Button(new Rect(px + 18, py + 146, 44, 34), "<")) { selected = (selected + 3) % 4; Play(swishClip); }
        if (Button(new Rect(px + 68, py + 146, 44, 34), ">")) { selected = (selected + 1) % 4; Play(swishClip); }
        if (Button(new Rect(px + pw - 238, py + 140, 220, 40), "Let's moo-ve!")) Choose(selected);
    }

    private void Stat(float x, float y, string label, float amount)
    {
        Shadowed(new Rect(x, y, 120, 22), label, smallStyle, Color.white);
        Box(new Rect(x + 120, y + 5, 140, 12), new Color(1, 1, 1, .2f));
        Box(new Rect(x + 120, y + 5, 140 * Mathf.Clamp01(amount), 12), MooData.Hex("ff8fc8"));
    }

    private void Hud(float w, float h)
    {
        MooCharacter me = Hero;
        Panel(new Rect(10, 10, 22 + me.MaxGiggles * 30, 64), new Color(.22f, .15f, .12f, .75f));
        Shadowed(new Rect(20, 12, 200, 22), me.Name + "'s giggles", smallStyle, Color.white);
        for (int i = 0; i < me.MaxGiggles; i++)
        {
            float pulse = i == giggles - 1 && giggles <= 2 ? 1 + Mathf.Abs(Mathf.Sin(time * 6)) * .15f : 1;
            float s = 26 * pulse;
            GUI.DrawTexture(new Rect(20 + i * 30 + 13 - s / 2, 36 + 13 - s / 2, s, s), i < giggles ? heart : emptyHeart);
        }
        float rx = w - 200;
        Panel(new Rect(rx, 10, 190, 64), new Color(.22f, .15f, .12f, .75f));
        GUI.DrawTexture(new Rect(rx + 10, 16, 24, 24), coin);
        Shadowed(new Rect(rx + 40, 16, 150, 24), save.moonies + " Moo-nies", smallStyle, Color.white);
        for (int i = 0; i < 3; i++)
            GUI.DrawTexture(new Rect(rx + 10 + i * 28, 44, 24, 24), (save.pieces & (1 << i)) != 0 ? bell : emptyBell);
        if (hasKey) GUI.DrawTexture(new Rect(rx + 150, 44, 24, 24), acornIcon);

        float sw = 220, sx = 14, sy = h - 46;
        Panel(new Rect(sx, sy, sw, 34), new Color(.22f, .15f, .12f, .75f));
        float ready = 1 - Mathf.Clamp01(specialCooldown / me.Cooldown);
        Box(new Rect(sx + 6, sy + 22, (sw - 12) * ready, 6), ready >= 1 ? MooData.Hex("ffe066") : MooData.Hex("ff8fc8"));
        Shadowed(new Rect(sx + 8, sy + 1, sw - 16, 22), (ready >= 1 ? "K: " : "...") + me.Special, smallStyle, Color.white);

        if (bannerTime > 0)
        {
            float a = Mathf.Clamp01(bannerTime) * Mathf.Clamp01((3.2f - bannerTime) * 3);
            MooArea area = MooData.Areas[areaIndex];
            float bw = Mathf.Min(560, w - 20);
            Box(new Rect((w - bw) / 2, 86, bw, 76), new Color(.22f, .15f, .12f, .55f * a));
            Shadowed(new Rect(0, 90, w, 44), area.Name, bigStyle, new Color(1, 1, 1, a));
            Shadowed(new Rect(0, 130, w, 30), area.Subtitle, centerStyle, new Color(1, .9f, .5f, a));
        }
        if (boss != null && boss.active)
        {
            // Sits between the side panels on wide screens and drops below them on narrow ones.
            bool fits = w - 440 >= 200;
            float bw = fits ? Mathf.Min(420, w - 440) : Mathf.Min(420, w - 20), bx = (w - bw) / 2, by = fits ? 12 : 82;
            Panel(new Rect(bx, by, bw, 44), new Color(.22f, .15f, .12f, .8f));
            Shadowed(new Rect(bx, by, bw, 22), "Grumbleweed's grumpiness", smallStyleCentered(), Color.white);
            Box(new Rect(bx + 10, by + 24, bw - 20, 12), new Color(1, 1, 1, .2f));
            Box(new Rect(bx + 10, by + 24, (bw - 20) * Mathf.Clamp01(boss.hp / (float)BossHp), 12),
                boss.mode == "dizzy" && Mathf.Repeat(time * 6, 1) < .5f ? MooData.Hex("ffe066") : MooData.Hex("b07ad8"));
        }
    }

    private void WorldLabels()
    {
        foreach (Label l in labels)
        {
            if ((l.pos - World(playerPos)).magnitude > 18) continue;
            Vector2 p = ToGui(l.pos, out bool visible);
            if (!visible) continue;
            Shadowed(new Rect(p.x - 100, p.y - 12, 200, 24), l.text, labelStyle, l.color);
        }
        foreach (FloatText f in floats)
        {
            Vector2 p = ToGui(f.pos + Vector3.up * f.time * 1.2f, out bool visible);
            if (!visible) continue;
            floatStyle.fontSize = f.big ? 24 : 18;
            var c = f.color;
            c.a = Mathf.Clamp01((f.life - f.time) * 2);
            Shadowed(new Rect(p.x - 250, p.y - 15, 500, 30), f.text, floatStyle, c);
        }
        if (state.status == "playing" && !gigglingOut && !SomethingToTickle() && TalkTarget(out Npc npc, out Vector2Int? sign))
        {
            Vector2 at = sign.HasValue ? Center(sign.Value) : Center(npc.tile);
            Vector2 p = ToGui(World(at, sign.HasValue ? 2.1f : 2.3f), out bool visible);
            if (visible)
            {
                string text = sign.HasValue ? "Read" : npc.kind == "rescue" ? "Help!" : "Talk";
                float bob = Mathf.Sin(time * 5) * 3;
                var r = new Rect(p.x - 34, p.y - 30 + bob, 68, 26);
                Panel(r, new Color(1f, .97f, .9f, .95f));
                labelStyle.normal.textColor = MooData.Hex("b05a8a");
                GUI.Label(r, text, labelStyle);
            }
        }
        if (boss != null && boss.active && boss.mode == "dizzy")
        {
            Vector2 p = ToGui(World(boss.pos, 3.6f), out bool visible);
            if (visible) Shadowed(new Rect(p.x - 100, p.y - 15, 200, 30), "@ @  dizzy!  @ @", floatStyle, MooData.Hex("ffe066"));
        }
    }

    private void DialogueBox(float w, float h)
    {
        string[] line = CurrentLine;
        float bw = Mathf.Min(760, w - 30), bx = (w - bw) / 2, by = h - 150;
        var rect = new Rect(bx, by, bw, 136);
        Panel(rect, new Color(1f, .97f, .9f, .97f));
        Box(new Rect(bx, by, 8, 136), MooData.Hex("ff8fc8"));
        if (line[0].Length > 0)
        {
            var tag = new Rect(bx + 20, by - 20, Mathf.Max(120, line[0].Length * 12 + 30), 34);
            Panel(tag, MooData.Hex("ff8fc8"));
            Shadowed(tag, line[0], nameStyle, Color.white);
        }
        int shown = Mathf.Min(line[1].Length, Mathf.FloorToInt(reveal));
        textStyle.normal.textColor = MooData.Hex("3b2a20");
        GUI.Label(new Rect(bx + 24, by + 22, bw - 48, 96), line[1].Substring(0, shown), textStyle);
        if (shown >= line[1].Length && Mathf.Repeat(time, 1) < .7f)
        {
            smallStyle.normal.textColor = MooData.Hex("b05a8a");
            GUI.Label(new Rect(bx + bw - 190, by + 108, 180, 24), "SPACE or tap to continue", smallStyle);
        }
        if (state.status == "dialogue" && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            actionPressed = true;
            Event.current.Use();
        }
    }

    private void PauseScreen(float w, float h)
    {
        Box(new Rect(0, 0, w, h), new Color(0, 0, 0, .45f));
        Shadowed(new Rect(0, h / 2 - 90, w, 70), "Paused", titleStyle, Color.white);
        Shadowed(new Rect(0, h / 2 - 20, w, 30), "The cows are waiting patiently. (They're very good at it.)", centerStyle, Color.white);
        Shadowed(new Rect(20, h / 2 + 14, w - 40, 50),
            "Move: arrows / WASD    Tickle & talk: SPACE    Special: K or SHIFT    Pause: ESC",
            smallStyleCentered(), new Color(1, .9f, .6f));
        if (Button(new Rect(w / 2 - 110, h / 2 + 70, 220, 48), "Keep playing")) SetPaused("0");
    }

    private void VictoryScreen(float w, float h)
    {
        float a = Mathf.Clamp01(partyTime - 1);
        if (a <= 0) return;
        Color old = GUI.color;
        GUI.color = new Color(1, 1, 1, a);
        Shadowed(new Rect(0, 24, w, 80), "MOO-TIFUL!", titleStyle, MooData.Hex("ffe066"));
        float pw = Mathf.Min(560, w - 40), px = (w - pw) / 2;
        Panel(new Rect(px, 112, pw, 250), new Color(.22f, .15f, .12f, .85f));
        string crown = save.crown
            ? "You found " + save.moonies + " Moo-nies and earned the sparkly Moo-nie Crown!"
            : "You found " + save.moonies + " Moo-nies. Find " + MooData.HatGoal + " for a secret crown!";
        string credits =
            Hero.Name + " saved Moo-ville!\n" +
            "Buttercup, Moo-donna, and Sir Moos-a-Lot are home, and Grumbleweed has a brand-new herd of friends.\n\n" + crown +
            "\n\nStarring Kaite, Laura, Grace, and Audrey.";
        Shadowed(new Rect(px + 20, 124, pw - 40, 230), credits, new GUIStyle(centerStyle) { alignment = TextAnchor.UpperCenter }, Color.white);
        if (partyTime > 2)
        {
            if (Button(new Rect(w / 2 - 250, 380, 240, 50), "Back to the farm")) BackToFarm();
            if (Button(new Rect(w / 2 + 10, 380, 240, 50), "Play again")) NewGame("");
        }
        GUI.color = old;
    }
}
