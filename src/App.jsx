import { Routes, Route } from 'react-router-dom'
import NavBar from './components/NavBar.jsx'
import Footer from './components/Footer.jsx'
import Home from './pages/Home.jsx'
import Gallery from './pages/Gallery.jsx'
import Cows from './pages/Cows.jsx'
import PongGame from './pages/PongGame.jsx'
import Countdown from './pages/Countdown.jsx'
import NotFound from './pages/NotFound.jsx'
import './styles/App.css'

export default function App() {
  return (
    <div className="app-shell">
      <NavBar />
      <main className="app-main">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/gallery" element={<Gallery />} />
          <Route path="/cows" element={<Cows />} />
          <Route path="/pong" element={<PongGame />} />
          <Route path="/christmas" element={<Countdown />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
      <Footer />
    </div>
  )
}
