# mccullough.fun

A cozy family site built with React and Vite.

## Development

Run `npm ci`, then `npm run dev`. Run `npm run build` for the production site.

## Pasture Pong

Visit `/pong` to play cow-themed, single-player Pong against Sir Moos-a-Lot.
First to seven wins. Easy, Medium, and Hard change CPU movement speed,
reaction time, and aiming error. Changing difficulty starts a new match.
Use mouse movement, touch dragging, arrow keys, or W/S; focus the pasture
for keyboard controls. Space/Escape or the Pause button pauses the game.
Leaving the tab pauses automatically. Restart and sound controls are also available.

The game is a real Unity Web (WebGL 2 / WebAssembly) build, not a JavaScript
Pong implementation. Its source lives in `unity/PasturePong`; the React page
owns the accessible controls, scoreboard, loading progress, and error UI.
The Unity player runs in an isolated iframe so navigating away and returning
creates a fresh runtime. Build-versioned download URLs keep cached assets in sync.
All cow art and the little scoring moo are generated locally in Unity.

### Rebuilding Unity

Install **Unity 6000.6.5f1** with **Web Build Support** and activate a Unity
license. On Windows, run:

```powershell
npm run build:unity
npm run build
```

For an editor outside the default Unity Hub location:

```powershell
.\scripts\build-unity.ps1 -EditorPath 'D:\Unity\Editor\Unity.exe'
```

On other platforms, run the editor in batch mode with the project path,
`-buildTarget WebGL -executeMethod BuildPasturePong.Build -quit`.
The build method creates the scene, checks CPU speeds, difficulty profiles,
paddle bounds, pause, collisions, both win conditions, and restart, then
exports uncompressed runtime assets to `public/unity/pong`.
Check in rebuilt Web assets alongside source changes: the existing site
deployment only needs `npm run build`, not a Unity editor or license.
The hosting configuration serves `.wasm` as `application/wasm` and excludes
Unity assets from SPA fallback so missing downloads fail explicitly.

## Moo Quest

Visit `/mooquest` for a cozy, real-time farm adventure. Grumbleweed the goat
has stolen the Golden Cowbell and the herd can't moo! Pick a hero:

- **Kaite** (blonde, blue eyes) — Sunshine Spin tickles everyone nearby.
- **Laura** (brunette, brown eyes) — Chocolate-Milk Dash zooms through critters.
- **Grace** (auburn, brown eyes) — Hay-Bale Toss bowls over troublemakers.
- **Audrey** (light brown, hazel eyes) — Moo-sic Lullaby puts critters to sleep.

Explore Moo-ville Barnyard, Clover Meadow, Mudpuddle Marsh, and Moonberry Woods to
rescue three lost cows and their cowbell pieces, then out-giggle Grumbleweed on
the hilltop. Combat is gentle tickling: critters giggle and run home, and the
hero just gets "giggled out" and wakes up safely back at the barn. Progress
auto-saves in the browser.

Controls: arrow keys/WASD move, Space/Enter tickles and talks, K/Shift uses the
special move, Escape/P pauses. Touch devices get an on-screen joystick and buttons.

Source lives in `unity/MooQuest`. The heroes, cows, and bunnies are CC0 models
from Kenney's [Mini Characters](https://kenney.nl/assets/mini-characters) and
[Cube Pets](https://kenney.nl/assets/cube-pets) packs (`Assets/Resources/Kenney`),
with cow-ear headbands and hair colors added in code. Everything else, including
the frogs, raccoons, Grumbleweed, music, and sounds, is generated in code. Rebuild with `npm run build:unity:mooquest`, or
`.\scripts\build-unity.ps1 -Game mooquest`; other platforms can use
`-executeMethod BuildMooQuest.Build`. The build validates the maps and plays
through the whole adventure (every hero's special, all three rescues, and the
boss) before exporting to `public/unity/mooquest`.
