import { useEffect, useRef, useState } from 'react'
import './PongGame.css'

const WIDTH = 720
const HEIGHT = 440
const PADDLE_W = 13
const PADDLE_H = 88
const PADDLE_MARGIN = 26
const BALL_R = 9
const WIN_SCORE = 10
const STEP_MS = 1000 / 60
const PLAYER_KEY_SPEED = 7
const CPU_MAX_SPEED = 5.2
const BALL_START_SPEED = 5
const BALL_MAX_SPEED = 12.5
const SERVE_DELAY = 48

const COLOR = {
  court: '#46311f',
  line: 'rgba(251, 243, 228, 0.22)',
  player: '#c9714f',
  cpu: '#8a9a5b',
  ball: '#e3aa45',
  ballGlow: 'rgba(227, 170, 69, 0.55)',
}

const clamp = (v, lo, hi) => (v < lo ? lo : v > hi ? hi : v)

function createGame() {
  return {
    ball: { x: WIDTH / 2, y: HEIGHT / 2, vx: 0, vy: 0, speed: BALL_START_SPEED },
    playerY: HEIGHT / 2 - PADDLE_H / 2,
    cpuY: HEIGHT / 2 - PADDLE_H / 2,
    score: { player: 0, cpu: 0 },
    serveTimer: SERVE_DELAY,
    serveDir: Math.random() < 0.5 ? -1 : 1,
    keys: { up: false, down: false },
  }
}

function serve(game) {
  const { ball } = game
  ball.x = WIDTH / 2
  ball.y = HEIGHT / 2
  ball.speed = BALL_START_SPEED
  const angle = (Math.random() - 0.5) * (Math.PI / 3)
  ball.vx = game.serveDir * ball.speed * Math.cos(angle)
  ball.vy = ball.speed * Math.sin(angle)
}

function bounce(game, paddleY, edgeX, dir) {
  const { ball } = game
  ball.x = edgeX
  const rel = clamp((ball.y - (paddleY + PADDLE_H / 2)) / (PADDLE_H / 2), -1, 1)
  ball.speed = Math.min(ball.speed + 0.45, BALL_MAX_SPEED)
  const angle = rel * (Math.PI / 3.2)
  ball.vx = dir * ball.speed * Math.cos(angle)
  ball.vy = ball.speed * Math.sin(angle)
}

// Advances the simulation one fixed step. Returns the scorer, or null.
function step(game) {
  const { ball } = game

  if (game.serveTimer > 0) {
    game.serveTimer -= 1
    if (game.serveTimer === 0) serve(game)
  }

  if (game.keys.up) game.playerY -= PLAYER_KEY_SPEED
  if (game.keys.down) game.playerY += PLAYER_KEY_SPEED
  game.playerY = clamp(game.playerY, 0, HEIGHT - PADDLE_H)

  const cpuCenter = game.cpuY + PADDLE_H / 2
  const cpuTarget = ball.vx > 0 ? ball.y : HEIGHT / 2
  const cpuDiff = cpuTarget - cpuCenter
  if (Math.abs(cpuDiff) > 6) {
    game.cpuY += clamp(cpuDiff, -CPU_MAX_SPEED, CPU_MAX_SPEED)
    game.cpuY = clamp(game.cpuY, 0, HEIGHT - PADDLE_H)
  }

  ball.x += ball.vx
  ball.y += ball.vy

  if (ball.y - BALL_R < 0) {
    ball.y = BALL_R
    ball.vy = Math.abs(ball.vy)
  } else if (ball.y + BALL_R > HEIGHT) {
    ball.y = HEIGHT - BALL_R
    ball.vy = -Math.abs(ball.vy)
  }

  const playerEdge = PADDLE_MARGIN + PADDLE_W
  if (
    ball.vx < 0 &&
    ball.x - BALL_R <= playerEdge &&
    ball.x + BALL_R >= PADDLE_MARGIN &&
    ball.y + BALL_R >= game.playerY &&
    ball.y - BALL_R <= game.playerY + PADDLE_H
  ) {
    bounce(game, game.playerY, playerEdge + BALL_R, 1)
  }

  const cpuEdge = WIDTH - PADDLE_MARGIN - PADDLE_W
  if (
    ball.vx > 0 &&
    ball.x + BALL_R >= cpuEdge &&
    ball.x - BALL_R <= WIDTH - PADDLE_MARGIN &&
    ball.y + BALL_R >= game.cpuY &&
    ball.y - BALL_R <= game.cpuY + PADDLE_H
  ) {
    bounce(game, game.cpuY, cpuEdge - BALL_R, -1)
  }

  if (ball.x + BALL_R < 0) return 'cpu'
  if (ball.x - BALL_R > WIDTH) return 'player'
  return null
}

function draw(ctx, game) {
  ctx.fillStyle = COLOR.court
  ctx.fillRect(0, 0, WIDTH, HEIGHT)

  ctx.strokeStyle = COLOR.line
  ctx.lineWidth = 4
  ctx.setLineDash([14, 18])
  ctx.beginPath()
  ctx.moveTo(WIDTH / 2, 10)
  ctx.lineTo(WIDTH / 2, HEIGHT - 10)
  ctx.stroke()
  ctx.setLineDash([])

  ctx.fillStyle = COLOR.player
  ctx.beginPath()
  ctx.roundRect(PADDLE_MARGIN, game.playerY, PADDLE_W, PADDLE_H, 6)
  ctx.fill()

  ctx.fillStyle = COLOR.cpu
  ctx.beginPath()
  ctx.roundRect(WIDTH - PADDLE_MARGIN - PADDLE_W, game.cpuY, PADDLE_W, PADDLE_H, 6)
  ctx.fill()

  ctx.save()
  ctx.shadowColor = COLOR.ballGlow
  ctx.shadowBlur = 18
  ctx.fillStyle = COLOR.ball
  ctx.beginPath()
  ctx.arc(game.ball.x, game.ball.y, BALL_R, 0, Math.PI * 2)
  ctx.fill()
  ctx.restore()
}

export default function PongGame() {
  const canvasRef = useRef(null)
  const gameRef = useRef(null)
  if (gameRef.current === null) gameRef.current = createGame()

  const [status, setStatus] = useState('idle')
  const [score, setScore] = useState({ player: 0, cpu: 0 })
  const [winner, setWinner] = useState(null)

  // Draw a static frame whenever the game is not actively running.
  useEffect(() => {
    if (status === 'playing') return
    draw(canvasRef.current.getContext('2d'), gameRef.current)
  }, [status])

  // Game loop.
  useEffect(() => {
    if (status !== 'playing') return
    const canvas = canvasRef.current
    const ctx = canvas.getContext('2d')
    const game = gameRef.current
    let raf = 0
    let last = performance.now()
    let acc = 0

    function frame(now) {
      acc += Math.min(now - last, 100)
      last = now

      let scored = null
      while (acc >= STEP_MS) {
        scored = step(game)
        acc -= STEP_MS
        if (scored) break
      }

      if (scored) {
        game.score[scored] += 1
        const next = { ...game.score }
        setScore(next)
        if (next[scored] >= WIN_SCORE) {
          setWinner(scored)
          setStatus('gameover')
          draw(ctx, game)
          return
        }
        game.serveDir = scored === 'player' ? 1 : -1
        game.ball.x = WIDTH / 2
        game.ball.y = HEIGHT / 2
        game.ball.vx = 0
        game.ball.vy = 0
        game.serveTimer = SERVE_DELAY
      }

      draw(ctx, game)
      raf = requestAnimationFrame(frame)
    }

    function pointerY(clientY) {
      const rect = canvas.getBoundingClientRect()
      return (clientY - rect.top) * (HEIGHT / rect.height)
    }
    function onMouseMove(e) {
      game.playerY = clamp(pointerY(e.clientY) - PADDLE_H / 2, 0, HEIGHT - PADDLE_H)
    }
    function onTouchMove(e) {
      const touch = e.touches[0]
      if (!touch) return
      game.playerY = clamp(
        pointerY(touch.clientY) - PADDLE_H / 2,
        0,
        HEIGHT - PADDLE_H,
      )
      e.preventDefault()
    }
    function onKeyDown(e) {
      if (e.key === 'ArrowUp' || e.key === 'w' || e.key === 'W') {
        game.keys.up = true
        e.preventDefault()
      } else if (e.key === 'ArrowDown' || e.key === 's' || e.key === 'S') {
        game.keys.down = true
        e.preventDefault()
      }
    }
    function onKeyUp(e) {
      if (e.key === 'ArrowUp' || e.key === 'w' || e.key === 'W') game.keys.up = false
      if (e.key === 'ArrowDown' || e.key === 's' || e.key === 'S') game.keys.down = false
    }

    canvas.addEventListener('mousemove', onMouseMove)
    canvas.addEventListener('touchmove', onTouchMove, { passive: false })
    window.addEventListener('keydown', onKeyDown)
    window.addEventListener('keyup', onKeyUp)
    raf = requestAnimationFrame(frame)

    return () => {
      cancelAnimationFrame(raf)
      canvas.removeEventListener('mousemove', onMouseMove)
      canvas.removeEventListener('touchmove', onTouchMove)
      window.removeEventListener('keydown', onKeyDown)
      window.removeEventListener('keyup', onKeyUp)
    }
  }, [status])

  function startGame() {
    gameRef.current = createGame()
    setScore({ player: 0, cpu: 0 })
    setWinner(null)
    setStatus('playing')
  }

  return (
    <div className="page pong">
      <header className="pong-head">
        <p className="pong-kicker">🏓 Family game night 🏓</p>
        <h1 className="pong-title">Family Pong</h1>
        <p className="pong-intro">
          You’re the warm terracotta paddle on the left. First to {WIN_SCORE}{' '}
          points wins — then the match resets for a rematch.
        </p>
      </header>

      <div className="pong-scoreboard">
        <div className="pong-score pong-score-player">
          <span className="pong-score-name">You</span>
          <span className="pong-score-value">{score.player}</span>
        </div>
        <span className="pong-score-sep">vs</span>
        <div className="pong-score pong-score-cpu">
          <span className="pong-score-name">CPU</span>
          <span className="pong-score-value">{score.cpu}</span>
        </div>
      </div>

      <div className="pong-stage">
        <canvas
          ref={canvasRef}
          width={WIDTH}
          height={HEIGHT}
          className="pong-canvas"
        />
        {status !== 'playing' && (
          <div className="pong-overlay">
            {status === 'idle' ? (
              <>
                <p className="pong-overlay-emoji" aria-hidden="true">🏓</p>
                <h2 className="pong-overlay-title">Ready to play?</h2>
                <p className="pong-overlay-text">
                  Move your paddle with the mouse, or the ↑ / ↓ (or W / S) keys.
                </p>
                <button type="button" className="btn" onClick={startGame}>
                  Start game
                </button>
              </>
            ) : (
              <>
                <p className="pong-overlay-emoji" aria-hidden="true">
                  {winner === 'player' ? '🎉' : '🤖'}
                </p>
                <h2 className="pong-overlay-title">
                  {winner === 'player' ? 'You win!' : 'The CPU wins!'}
                </h2>
                <p className="pong-overlay-text">
                  Final score — You {score.player}, CPU {score.cpu}.
                </p>
                <button type="button" className="btn" onClick={startGame}>
                  Play again
                </button>
              </>
            )}
          </div>
        )}
      </div>

      <p className="pong-hint">
        Tip: clip the ball with the edge of your paddle for a sharper angle.
      </p>
    </div>
  )
}
