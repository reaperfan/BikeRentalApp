import { Navigate, Route, Routes } from 'react-router-dom'
import ProtectedRoute from './auth/ProtectedRoute'
import AppHeader from './components/AppHeader'
import Home from './pages/Home'
import Login from './pages/Login'
import Profile from './pages/Profile'
import Register from './pages/Register'
import Szerviz from './pages/Szerviz'
import BikeCatalog from './pages/BikeCatalog'
import BikeRental from './pages/BikeRental'
import Reviews from './pages/Reviews'

export default function App() {
  return (
    <>
      <AppHeader />
      <Routes>
        <Route path="/ertekelesek" element={<Reviews />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/" element={<ProtectedRoute><Home /></ProtectedRoute>} />
        <Route path="/profile" element={<ProtectedRoute><Profile /></ProtectedRoute>} />
        <Route path="/szerviz" element={<ProtectedRoute><Szerviz /></ProtectedRoute>} />
        <Route path="/catalog" element={<BikeCatalog />} />
        <Route path="/rental/:bikeId" element={<BikeRental />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </>
  )
}
