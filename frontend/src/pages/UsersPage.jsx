import { useEffect, useState } from 'react';
import { getUsers, updateUserRole, assignUserTeam } from '../services/userService';
import { getTeams } from '../services/teamService';

const ROLES = ['Admin', 'Manager', 'User'];

export default function UsersPage() {
  const [users, setUsers] = useState([]);
  const [teams, setTeams] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    load();
  }, []);

  async function load() {
    setLoading(true);
    setError('');
    try {
      const [usersResult, teamsResult] = await Promise.all([getUsers(), getTeams()]);
      setUsers(usersResult);
      setTeams(teamsResult);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function handleRoleChange(userId, role) {
    setError('');
    try {
      const updated = await updateUserRole(userId, role);
      setUsers((list) => list.map((u) => (u.id === userId ? updated : u)));
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleTeamChange(userId, teamId) {
    setError('');
    try {
      const updated = await assignUserTeam(userId, teamId ? Number(teamId) : null);
      setUsers((list) => list.map((u) => (u.id === userId ? updated : u)));
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div>
      <h1>Users</h1>
      {error && <div className="error-banner">{error}</div>}
      {loading ? (
        <p>Loading users…</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Role</th>
              <th>Team</th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id}>
                <td>{u.fullName}</td>
                <td>{u.email}</td>
                <td>
                  <select value={u.role} onChange={(e) => handleRoleChange(u.id, e.target.value)}>
                    {ROLES.map((r) => (
                      <option key={r} value={r}>{r}</option>
                    ))}
                  </select>
                </td>
                <td>
                  <select
                    value={u.teamId ?? ''}
                    onChange={(e) => handleTeamChange(u.id, e.target.value)}
                  >
                    <option value="">No team</option>
                    {teams.map((t) => (
                      <option key={t.id} value={t.id}>{t.name}</option>
                    ))}
                  </select>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
