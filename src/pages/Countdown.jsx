import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import './Countdown.css'

function getCountdownState() {
  const now = new Date()
  const isChristmasDay = now.getMonth() === 11 && now.getDate() === 25

  let target = new Date(now.getFullYear(), 11, 25, 0, 0, 0, 0)
  if (now >= target) {
    target = new Date(now.getFullYear() + 1, 11, 25, 0, 0, 0, 0)
  }

  const diff = Math.max(0, target - now)
  return {
    isChristmasDay,
    year: target.getFullYear(),
    days: Math.floor(diff / 86400000),
    hours: Math.floor((diff % 86400000) / 3600000),
    minutes: Math.floor((diff % 3600000) / 60000),
    seconds: Math.floor((diff % 60000) / 1000),
  }
}

function pad(value) {
  return String(value).padStart(2, '0')
}

export default function Countdown() {
  const [state, setState] = useState(getCountdownState)

  useEffect(() => {
    const timer = setInterval(() => setState(getCountdownState()), 1000)
    return () => clearInterval(timer)
  }, [])

  if (state.isChristmasDay) {
    return (
      <div className="page countdown">
        <section className="xmas-day">
          <p className="xmas-emoji" aria-hidden="true">🎄🎅🎁</p>
          <h1 className="xmas-day-title">Merry Christmas!</h1>
          <p className="xmas-day-text">
            It’s finally here. From our family to yours — wishing you a warm,
            cozy, and joyful Christmas day.
          </p>
          <Link to="/gallery" className="btn">
            Look back at our memories
          </Link>
        </section>
      </div>
    )
  }

  const units = [
    { label: 'Days', value: state.days },
    { label: 'Hours', value: pad(state.hours) },
    { label: 'Minutes', value: pad(state.minutes) },
    { label: 'Seconds', value: pad(state.seconds) },
  ]

  return (
    <div className="page countdown">
      <header className="countdown-head">
        <p className="countdown-kicker">🎄 Counting down to 🎄</p>
        <h1 className="countdown-title">Christmas {state.year}</h1>
        <p className="countdown-intro">
          The stockings, the cocoa, the whole crew under one roof — here’s how
          long until the big day.
        </p>
      </header>

      <section
        className="countdown-grid"
        aria-label={`${state.days} days, ${state.hours} hours, ${state.minutes} minutes, and ${state.seconds} seconds until Christmas`}
      >
        {units.map((unit) => (
          <div key={unit.label} className="countdown-unit">
            <span className="countdown-value">{unit.value}</span>
            <span className="countdown-label">{unit.label}</span>
          </div>
        ))}
      </section>

      <p className="countdown-note">
        ❄️ Until then — hot cocoa, warm blankets, and a little patience. ❄️
      </p>
    </div>
  )
}
