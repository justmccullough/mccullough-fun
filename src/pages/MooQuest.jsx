import { useEffect, useRef, useState } from 'react'
import './MooQuest.css'

const HEROES = [
  { name: 'Kaite', looks: 'Blonde hair · blue eyes', hair: '#f2d16b', eyes: '#2f7fd8', shirt: '#7fc8f8',
    special: 'Sunshine Spin', note: 'Twirls so fast everyone nearby gets tickled.' },
  { name: 'Laura', looks: 'Brunette · brown eyes', hair: '#5b3a23', eyes: '#6b3e1f', shirt: '#f49ac2',
    special: 'Chocolate-Milk Dash', note: 'The fastest boots on the farm. Zoom!' },
  { name: 'Grace', looks: 'Auburn hair · brown eyes', hair: '#b8452a', eyes: '#6b3e1f', shirt: '#8fd694',
    special: 'Hay-Bale Toss', note: 'Small but mighty. Bowls critters over with hay.' },
  { name: 'Audrey', looks: 'Light brown hair · hazel eyes', hair: '#a47c56', eyes: '#8a7f3c', shirt: '#c9a7f0',
    special: 'Moo-sic Lullaby', note: 'A song so cozy that critters nap and goats get dizzy.' },
]
const INITIAL = { status: 'loading', character: '', area: '', message: 'Loading the farm…', giggles: 0, maxGiggles: 0,
  moonies: 0, pieces: 0, hasSave: false, muted: false, crown: false }
const countPieces = (mask) => [1, 2, 4].filter((bit) => mask & bit).length

export default function MooQuest() {
  const frameRef = useRef(null)
  const stageRef = useRef(null)
  const [full, setFull] = useState(false)
  const [game, setGame] = useState(INITIAL)
  const [progress, setProgress] = useState(0)
  const [ready, setReady] = useState(false)
  const [error, setError] = useState('')
  const [attempt, setAttempt] = useState(0)

  function send(method, value = '') {
    frameRef.current?.contentWindow?.postMessage({ type: 'moo-command', method, value: String(value) }, window.location.origin)
  }

  useEffect(() => {
    setReady(false)
    setError('')
    setProgress(0)
    setGame(INITIAL)
    const timeout = window.setTimeout(() => {
      setError('The farm is taking too long to open. Check your connection and try again.')
    }, 90000)
    const message = (event) => {
      if (event.origin !== window.location.origin || event.source !== frameRef.current?.contentWindow) return
      switch (event.data.type) {
        case 'moo-state': setGame(event.data.detail); break
        case 'moo-progress': setProgress(event.data.detail); break
        case 'moo-ready': window.clearTimeout(timeout); setReady(true); break
        case 'moo-error': window.clearTimeout(timeout); setError(event.data.detail); break
      }
    }
    const pause = () => {
      // Focusing the Unity iframe also blurs its parent; that is not a tab change.
      if (document.activeElement !== frameRef.current) send('SetPaused', '1')
    }
    const visibility = () => { if (document.hidden) send('SetPaused', '1') }
    window.addEventListener('message', message)
    window.addEventListener('blur', pause)
    document.addEventListener('visibilitychange', visibility)
    return () => {
      window.clearTimeout(timeout)
      window.removeEventListener('message', message)
      window.removeEventListener('blur', pause)
      document.removeEventListener('visibilitychange', visibility)
    }
  }, [attempt])

  // Phones need the whole screen to make the game readable. Use real fullscreen where the browser allows it
  // (and try to lock landscape); otherwise just fill the window, which also works on iPhone.
  async function enterFullscreen() {
    const stage = stageRef.current
    setFull(true)
    if (stage?.requestFullscreen && document.fullscreenEnabled) {
      try {
        await stage.requestFullscreen({ navigationUI: 'hide' })
        await screen.orientation?.lock?.('landscape')
      } catch { /* Filling the window is fine too. */ }
    }
    send('Focus')
  }

  function exitFullscreen() {
    if (document.fullscreenElement) document.exitFullscreen().catch(() => {})
    setFull(false)
    send('Focus')
  }

  useEffect(() => {
    const changed = () => { if (!document.fullscreenElement) setFull(false) }
    document.addEventListener('fullscreenchange', changed)
    return () => document.removeEventListener('fullscreenchange', changed)
  }, [])

  useEffect(() => {
    document.body.classList.toggle('moo-is-full', full)
    return () => document.body.classList.remove('moo-is-full')
  }, [full])

  const live = ready && !error
  const inAdventure = ['playing', 'dialogue', 'paused'].includes(game.status)
  const paused = game.status === 'paused'

  function startOver() {
    if (game.hasSave && !window.confirm('Start a brand-new adventure? Your saved progress will be forgotten.')) return
    send('NewGame')
  }

  return (
    <div className="page moo">
      <header className="moo-head">
        <p className="moo-kicker">🐄 A tiny farm adventure</p>
        <h1 className="moo-title">Moo Quest<span aria-hidden="true">!</span></h1>
        <p className="moo-intro">Grumbleweed the goat has stolen the Golden Cowbell, and now the herd can’t moo!<br />
          Pick a hero, tickle some troublemakers, and bring the music back to the farm.</p>
      </header>

      <section className="moo-card" aria-label="Moo Quest game">
        <div className="moo-hud" aria-label="Adventure progress">
          <span><strong>{game.character || 'No hero yet'}</strong>{game.area ? ` · ${game.area}` : ''}</span>
          <span aria-label={`Giggles ${game.giggles} of ${game.maxGiggles}`}>💖 {game.giggles}/{game.maxGiggles}</span>
          <span aria-label={`${game.moonies} moonies`}>🪙 {game.moonies}</span>
          <span aria-label={`${countPieces(game.pieces)} of 3 cowbell pieces`}>🔔 {countPieces(game.pieces)}/3</span>
          {game.crown && <span>👑 Hero of the Herd</span>}
        </div>

        <div ref={stageRef} className={full ? 'moo-stage is-full' : 'moo-stage'}>
          <iframe key={attempt} ref={frameRef} className="moo-canvas"
            src={`${import.meta.env.BASE_URL}unity/mooquest-host.html`}
            title="Unity Moo Quest game" aria-describedby="moo-controls"
            onError={() => setError('The Unity player could not load. Please try again.')} />
          {full && <button type="button" className="moo-exit-full" onClick={exitFullscreen}>✕ Exit fullscreen</button>}
          {(error || !ready) && (
            <div className="moo-overlay">
              <span className="moo-overlay-emoji" aria-hidden="true">{error ? '🌧️' : '🐮'}</span>
              <h2>{error ? 'A little trouble on the farm' : 'Opening the barn doors…'}</h2>
              <p>{error || 'The cows are fluffing their bandanas. Almost there!'}</p>
              {error ? <button className="btn" onClick={() => setAttempt((value) => value + 1)}>Try again</button> :
                <><progress value={progress} max={1} aria-label="Loading Unity game" /><span>{Math.round(progress * 100)}%</span></>}
            </div>
          )}
        </div>

        <div className="moo-bottom-bar">
          <p className="moo-commentary" role="status">{game.message}</p>
          <div className="moo-actions">
            <button type="button" disabled={!live || !inAdventure}
              onClick={() => send('SetPaused', paused ? '0' : '1')}>{paused ? 'Resume' : 'Pause'}</button>
            <button type="button" disabled={!live} aria-pressed={game.muted}
              onClick={() => send('SetMuted', game.muted ? '0' : '1')}>{game.muted ? 'Sound off' : 'Sound on'}</button>
            <button type="button" disabled={!live} onClick={startOver}>Start over</button>
            <button type="button" className="moo-full-button" disabled={!live} onClick={enterFullscreen}>⛶ Fullscreen</button>
          </div>
        </div>
      </section>

      <section className="moo-heroes" aria-label="Meet the heroes">
        {HEROES.map((hero) => (
          <article key={hero.name} className="moo-hero" style={{ '--shirt': hero.shirt }}>
            <div className="moo-face" aria-hidden="true" style={{ '--hair': hero.hair, '--eyes': hero.eyes }}>
              <span className="moo-eye" /><span className="moo-eye" />
            </div>
            <h3>{hero.name}</h3>
            <p className="moo-looks">{hero.looks}</p>
            <p><strong>{hero.special}</strong> {hero.note}</p>
          </article>
        ))}
      </section>

      <div id="moo-controls" className="moo-tips">
        <p><strong>How to play</strong> Click the game, then move with the arrow keys or WASD. Space tickles critters and chats with cows.
          K or Shift uses your special move. Esc pauses. On a phone or tablet, tap Fullscreen, turn your phone sideways, and use the on-screen joystick and buttons.</p>
        <p><strong>Farmer tips</strong> Nobody ever gets hurt here — critters just giggle until they run home. Drink milk to refill your
          giggles, push hay bales to build bridges, and your adventure saves itself automatically.</p>
      </div>
      <p className="moo-footnote">Made with Unity · powered by giggles · zero goats were harmed</p>
    </div>
  )
}
