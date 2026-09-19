import { useAuth } from '../context/AuthContext';

export default function ProfilePage() {
  const { user, logout } = useAuth();

  return (
    <div>
      <h1>Profile</h1>
      <div className="card" style={{ maxWidth: 400 }}>
        <p><strong>Name:</strong> {user.fullName}</p>
        <p><strong>Email:</strong> {user.email}</p>
        <p><strong>Role:</strong> {user.role}</p>
        <p><strong>Team:</strong> {user.teamName ?? 'Unassigned'}</p>
        <button className="btn btn-secondary" onClick={logout}>
          Log out
        </button>
      </div>
    </div>
  );
}
