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
