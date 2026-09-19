import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { getTeam, addTeamMember, removeTeamMember, deleteTeam } from '../services/teamService';
import { getUsers } from '../services/userService';

export default function TeamDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();

  const [team, setTeam] = useState(null);
  const [allUsers, setAllUsers] = useState([]);
  const [selectedUserId, setSelectedUserId] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  const canManage =
    user.role === 'Admin' || (user.role === 'Manager' && user.teamId === Number(id));

  useEffect(() => {
    load();
    if (canManage) {
      getUsers().then(setAllUsers).catch(() => {});
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  async function load() {
    setLoading(true);
    setError('');
    try {
      setTeam(await getTeam(id));
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function handleAddMember(e) {
    e.preventDefault();
    if (!selectedUserId) return;
    setError('');
    try {
      const updated = await addTeamMember(id, Number(selectedUserId));
      setTeam(updated);
      setSelectedUserId('');
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleRemoveMember(userId) {
    setError('');
    try {
      const updated = await removeTeamMember(id, userId);
      setTeam(updated);
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleDeleteTeam() {
    if (!confirm('Delete this team? This is only possible if it has no tasks.')) return;
    try {
      await deleteTeam(id);
      navigate('/teams');
    } catch (err) {
      setError(err.message);
    }
  }

  if (loading) return <p>Loading team…</p>;
  if (!team) return <div className="error-banner">{error}</div>;

  const memberIds = new Set(team.members.map((m) => m.id));
  const availableUsers = allUsers.filter((u) => !memberIds.has(u.id));

  return (
    <div>
      <button className="btn btn-secondary" onClick={() => navigate('/teams')}>
        ← Back to teams
      </button>

      {error && <div className="error-banner">{error}</div>}

      <div className="card" style={{ marginTop: '1rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
          <h1>{team.name}</h1>
          {user.role === 'Admin' && (
            <button className="btn btn-danger" onClick={handleDeleteTeam}>
              Delete team
            </button>
          )}
        </div>
        <p>Manager: {team.managerName ?? 'Unassigned'}</p>
      </div>

      <div className="card">
        <h3>Members ({team.members.length})</h3>
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Role</th>
              {canManage && <th></th>}
            </tr>
          </thead>
          <tbody>
            {team.members.map((m) => (
              <tr key={m.id}>
                <td>{m.fullName}</td>
                <td>{m.email}</td>
                <td>{m.role}</td>
                {canManage && (
                  <td>
                    <button className="btn btn-danger" onClick={() => handleRemoveMember(m.id)}>
                      Remove
                    </button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>

        {canManage && (
          <form onSubmit={handleAddMember} style={{ marginTop: '1rem' }}>
            <div className="form-group">
              <label htmlFor="userId">Add member</label>
              <select
                id="userId"
                value={selectedUserId}
                onChange={(e) => setSelectedUserId(e.target.value)}
              >
                <option value="">Select a user</option>
                {availableUsers.map((u) => (
                  <option key={u.id} value={u.id}>{u.fullName} ({u.email})</option>
                ))}
              </select>
            </div>
            <button className="btn" type="submit" disabled={!selectedUserId}>
              Add to team
            </button>
          </form>
        )}
      </div>
    </div>
  );
}
