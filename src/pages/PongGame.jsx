import { useEffect, useRef, useState } from 'react'
import './PongGame.css'

const LEVELS = [
  { name: 'Easy', note: 'Just grazing. A slow, daydreamy opponent.' },
  { name: 'Medium', note: 'A little competitive. Still very moo-ch your friend.' },
  { name: 'Hard', note: 'Fast hooves. Sharp reflexes. Absolutely no chill.' },
]
const INITIAL = { status: 'idle', player: 0, cpu: 0, difficulty: 1, rally: 0, message: 'A friendly little pasture rivalry.' }
export default function PongGame() {
  const frameRef = useRef(null)
  const [game, setGame] = useState(INITIAL)
  const [progress, setProgress] = useState(0)
  const [ready, setReady] = useState(false)
  const [error, setError] = useState('')
  const [attempt, setAttempt] = useState(0)
  const [muted, setMuted] = useState(false)

  function send(method, value = '') {
    frameRef.current?.contentWindow?.postMessage({ type: 'pong-command', method, value: String(value) }, window.location.origin)
  }

  useEffect(() => {
    setReady(false)
    setMuted(false)
    setError('')
    setProgress(0)
    setGame(INITIAL)
    const timeout = window.setTimeout(() => {
      setError('The pasture is taking too long to open. Check your connection and try again.')
    }, 90000)
    const message = (event) => {
      if (event.origin !== window.location.origin || event.source !== frameRef.current?.contentWindow) return
      switch (event.data.type) {
        case 'pong-state': setGame(event.data.detail); break
        case 'pong-progress': setProgress(event.data.detail); break
        case 'pong-ready': window.clearTimeout(timeout); setReady(true); break
        case 'pong-error': window.clearTimeout(timeout); setError(event.data.detail); break
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

  function start() {
    send('Restart')
  }

  function chooseLevel(index) {
    send('SetDifficulty', index)
  }

  return (
    <div className="page pong">
      <header className="pong-head">
        <p className="pong-kicker">🐄 A little friendly farm-petition</p>
        <h1 className="pong-title">Pasture Pong<span aria-hidden="true">.</span></h1>
        <p className="pong-intro">One pasture. Two cows. Unreasonably high steaks.<br />
          Take on Sir Moos-a-Lot in a cozy, single-player game of Pong.</p>
      </header>

      <section className="pong-card" aria-label="Pasture Pong game">
        <div className="pong-toolbar">
          <div className="pong-levels" role="group" aria-label="CPU difficulty">
            {LEVELS.map((level, index) => (
              <button key={level.name} type="button" aria-pressed={game.difficulty === index}
                disabled={!ready || !!error} onClick={() => chooseLevel(index)}>{level.name}</button>
            ))}
          </div>
          <span className="pong-rules">First to 7 · no actual beef</span>
        </div>
        <p className="pong-level-note">{LEVELS[game.difficulty].note} Changing difficulty starts a fresh match.</p>

        <div className="pong-scoreboard" aria-label={`Score: you ${game.player}, CPU ${game.cpu}`}>
          <div className="pong-team pong-team-player"><span className="pong-avatar" aria-hidden="true">🐮</span>
            <div><span className="pong-score-name">You</span><span className="pong-team-note">The hometown heifer</span></div>
          </div>
          <div className="pong-score"><span>{game.player}</span><span className="pong-score-sep">:</span><span>{game.cpu}</span></div>
          <div className="pong-team pong-team-cpu"><div><span className="pong-score-name">Sir Moos-a-Lot</span>
            <span className="pong-team-note">CPU · professional grazer</span></div><span className="pong-avatar" aria-hidden="true">🐄</span></div>
        </div>

        <div className="pong-stage">
          <iframe key={attempt} ref={frameRef} className="pong-canvas"
            src={`${import.meta.env.BASE_URL}unity/pong-host.html`}
            title="Unity Pasture Pong field" aria-describedby="pong-controls"
            onError={() => setError('The Unity player could not load. Please try again.')} />
          {(error || !ready || game.status !== 'playing') && (
            <div className="pong-overlay">
              <span className="pong-overlay-emoji" aria-hidden="true">{error ? '🌧️' : game.status === 'gameover' && game.player >= 7 ? '🏆' : '🐮'}</span>
              <h2>{error ? 'A little trouble in the pasture' : !ready ? 'Opening the pasture…' :
                game.status === 'paused' ? 'Taking a grass break' : game.status === 'gameover' ?
                  game.player >= 7 ? 'Udderly victorious!' : 'Sir Moos-a-Lot wins!' : 'Ready to raise the steaks?'}</h2>
              <p>{error || (!ready ? 'Loading our little Unity game. The cows are getting their hooves on.' :
                game.status === 'paused' ? 'Your herd will be right here.' : game.status === 'gameover' ?
                  game.message : 'You’re the cow with the terracotta scarf. Keep the hay bale in play!')}</p>
              {error ? <button className="btn" onClick={() => setAttempt((value) => value + 1)}>Try again</button> :
                !ready ? <><progress value={progress} max={1} aria-label="Loading Unity game" /><span>{Math.round(progress * 100)}%</span></> :
                  <button className="btn" onClick={game.status === 'paused' ? () => {
                    send('SetPaused', '0')
                  } : start}>{game.status === 'paused' ? 'Back to the pasture' : game.status === 'gameover' ? 'One moo-re round' : 'Let’s play'}</button>}
            </div>
          )}
        </div>

        <div className="pong-bottom-bar">
          <p className="pong-commentary" role="status">{game.message}</p>
          <div className="pong-actions">
            <button type="button" disabled={!ready || !!error || !['playing', 'paused'].includes(game.status)}
              onClick={() => {
                send('SetPaused', game.status === 'paused' ? '0' : '1')
              }}>{game.status === 'paused' ? 'Resume' : 'Pause'}</button>
            <button type="button" disabled={!ready || !!error} onClick={start}>Restart</button>
            <button type="button" disabled={!ready || !!error} aria-pressed={muted} onClick={() => {
              send('SetMuted', muted ? '0' : '1'); setMuted(!muted)
            }}>{muted ? 'Sound off' : 'Sound on'}</button>
          </div>
        </div>
      </section>
      <div id="pong-controls" className="pong-tips">
        <p><strong>How to herd</strong> Move your mouse or drag on the pasture. Keyboard? Click the field, then use ↑ / ↓ or W / S. Space pauses.</p>
        <p><strong>A little cow wisdom</strong> Catch the hay at the edge of your cow for a sharper shot. Losing is just an excuse for one moo-re round.</p>
      </div>
      <p className="pong-footnote">Made with Unity · fueled by hay · approved by absolutely no cows</p>
    </div>
  )
}
