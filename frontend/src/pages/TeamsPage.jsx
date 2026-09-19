import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { getTeams, createTeam } from '../services/teamService';
import { getUsers } from '../services/userService';

export default function TeamsPage() {
  const { user } = useAuth();
  const isAdmin = user.role === 'Admin';

  const [teams, setTeams] = useState([]);
  const [managers, setManagers] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: '', managerId: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    load();
    if (isAdmin) {
      getUsers()
        .then((users) => setManagers(users.filter((u) => u.role === 'Manager')))
        .catch(() => {});
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function load() {
    setLoading(true);
    setError('');
    try {
      setTeams(await getTeams());
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function handleCreate(e) {
    e.preventDefault();
    setError('');
    try {
      await createTeam({
        name: form.name,
        managerId: form.managerId ? Number(form.managerId) : null
      });
      setForm({ name: '', managerId: '' });
      setShowForm(false);
      load();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1>Teams</h1>
        {isAdmin && (
          <button className="btn" onClick={() => setShowForm((s) => !s)}>
            {showForm ? 'Cancel' : 'New team'}
          </button>
        )}
      </div>

      {error && <div className="error-banner">{error}</div>}

      {showForm && (
        <form className="card" onSubmit={handleCreate}>
          <div className="form-group">
            <label htmlFor="name">Team name</label>
            <input
              id="name"
              value={form.name}
              onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="managerId">Manager</label>
            <select
              id="managerId"
              value={form.managerId}
              onChange={(e) => setForm((f) => ({ ...f, managerId: e.target.value }))}
            >
              <option value="">No manager yet</option>
              {managers.map((m) => (
                <option key={m.id} value={m.id}>{m.fullName}</option>
              ))}
            </select>
          </div>
          <button className="btn" type="submit">Create team</button>
        </form>
      )}

      {loading ? (
        <p>Loading teams…</p>
      ) : teams.length === 0 ? (
        <p className="empty-state">No teams yet.</p>
      ) : (
        <div className="card-grid">
          {teams.map((t) => (
            <Link key={t.id} to={`/teams/${t.id}`} className="card">
              <h3>{t.name}</h3>
              <p>Manager: {t.managerName ?? 'Unassigned'}</p>
              <p>{t.memberCount} member(s)</p>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
