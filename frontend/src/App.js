import './App.css';
import { Routes, Route} from 'react-router-dom'
import Login from './components/Login/Login'
import Home from './components/Home/Home'


const routes = {
  '/': () => <Login />,
  '/login': () => <Login />
}
function App() {
  return (<Routes>
    <Route path="/" element={<Login />} />
    <Route path="/login" element={<Login />} />
    <Route path="/home" element={<Home />} />
  </Routes>
  );
}
export default App;
