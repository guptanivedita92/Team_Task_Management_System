import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function MainLayout() {
  const { user, logout } = useAuth();

  return (
    <div className="app-shell">
      <header className="navbar">
        <div className="navbar-brand">Team Task Management</div>
        <nav className="navbar-links">
          <NavLink to="/dashboard">Dashboard</NavLink>
          <NavLink to="/tasks">Tasks</NavLink>
          <NavLink to="/teams">Teams</NavLink>
          {user?.role === 'Admin' && <NavLink to="/users">Users</NavLink>}
          <NavLink to="/notifications">Notifications</NavLink>
          <NavLink to="/profile">{user?.fullName ?? 'Profile'}</NavLink>
          <button className="btn btn-secondary" onClick={logout}>
            Log out
          </button>
        </nav>
      </header>
      <main className="page-container">
        <Outlet />
      </main>
    </div>
  );
}
