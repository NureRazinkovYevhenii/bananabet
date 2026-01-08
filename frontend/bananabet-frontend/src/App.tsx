import { Route, Routes } from 'react-router-dom';
import './App.css';
import useWallet from './useWallet';
import HeaderBar from './components/HeaderBar';
import HomePage from './pages/HomePage';
import MatchesPage from './pages/MatchesPage';
import MatchDetailsPage from './pages/MatchDetailsPage';
import ProfilePage from './pages/ProfilePage';

function App() {
  const wallet = useWallet();

  return (
    <>
      <HeaderBar wallet={wallet} />
      <main style={{ padding: 24 }}>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/matches" element={<MatchesPage />} />
          <Route path="/matches/:id" element={<MatchDetailsPage wallet={wallet} />} />
          <Route path="/profile" element={<ProfilePage wallet={wallet} />} />
        </Routes>
      </main>
    </>
  );
}

export default App;
