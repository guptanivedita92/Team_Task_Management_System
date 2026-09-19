import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { getTasks, createTask, deleteTask } from '../services/taskService';
import { getTeams } from '../services/teamService';

const STATUS_OPTIONS = ['ToDo', 'InProgress', 'Done'];
const PRIORITY_OPTIONS = ['Low', 'Medium', 'High'];

function statusBadgeClass(status) {
  if (status === 'InProgress') return 'badge badge-inprogress';
  if (status === 'Done') return 'badge badge-done';
  return 'badge badge-todo';
}

function priorityBadgeClass(priority) {
  if (priority === 'High') return 'badge badge-high';
  if (priority === 'Medium') return 'badge badge-medium';
  return 'badge badge-low';
}

export default function TasksPage() {
  const { user } = useAuth();
  const canCreate = user.role === 'Admin' || user.role === 'Manager';

  const [tasks, setTasks] = useState([]);
  const [teams, setTeams] = useState([]);
  const [filters, setFilters] = useState({ status: '', priority: '', search: '' });
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ title: '', description: '', teamId: '', priority: 'Medium', deadline: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    load();
    if (canCreate) {
      getTeams().then(setTeams).catch(() => {});
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters]);

  async function load() {
    setLoading(true);
    setError('');
    try {
      const params = {};
      if (filters.status) params.status = filters.status;
      if (filters.priority) params.priority = filters.priority;
      if (filters.search) params.search = filters.search;
      const result = await getTasks(params);
      setTasks(result);
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
      await createTask({
        title: form.title,
        description: form.description || null,
        teamId: Number(form.teamId),
        priority: form.priority,
        deadline: form.deadline ? new Date(form.deadline).toISOString() : null
      });
      setForm({ title: '', description: '', teamId: '', priority: 'Medium', deadline: '' });
      setShowForm(false);
      load();
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleDelete(id) {
    if (!confirm('Delete this task?')) return;
    try {
      await deleteTask(id);
      load();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1>Tasks</h1>
        {canCreate && (
          <button className="btn" onClick={() => setShowForm((s) => !s)}>
            {showForm ? 'Cancel' : 'New task'}
          </button>
        )}
      </div>

      {error && <div className="error-banner">{error}</div>}

      {showForm && (
        <form className="card" onSubmit={handleCreate}>
          <div className="form-group">
            <label htmlFor="title">Title</label>
            <input
              id="title"
              value={form.title}
              onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))}
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="description">Description</label>
            <textarea
              id="description"
              rows={3}
              value={form.description}
              onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
            />
          </div>
          <div className="form-group">
            <label htmlFor="teamId">Team</label>
            <select
              id="teamId"
              value={form.teamId}
              onChange={(e) => setForm((f) => ({ ...f, teamId: e.target.value }))}
              required
            >
              <option value="">Select a team</option>
              {teams.map((t) => (
                <option key={t.id} value={t.id}>{t.name}</option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label htmlFor="priority">Priority</label>
            <select
              id="priority"
              value={form.priority}
              onChange={(e) => setForm((f) => ({ ...f, priority: e.target.value }))}
            >
              {PRIORITY_OPTIONS.map((p) => (
                <option key={p} value={p}>{p}</option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label htmlFor="deadline">Deadline</label>
            <input
              id="deadline"
              type="date"
              value={form.deadline}
              onChange={(e) => setForm((f) => ({ ...f, deadline: e.target.value }))}
            />
          </div>
          <button className="btn" type="submit">Create task</button>
        </form>
      )}

      <div className="filters-bar">
        <input
          placeholder="Search title or description…"
          value={filters.search}
          onChange={(e) => setFilters((f) => ({ ...f, search: e.target.value }))}
        />
        <select
          value={filters.status}
          onChange={(e) => setFilters((f) => ({ ...f, status: e.target.value }))}
        >
          <option value="">All statuses</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
        <select
          value={filters.priority}
          onChange={(e) => setFilters((f) => ({ ...f, priority: e.target.value }))}
        >
          <option value="">All priorities</option>
          {PRIORITY_OPTIONS.map((p) => (
            <option key={p} value={p}>{p}</option>
          ))}
        </select>
      </div>

      {loading ? (
        <p>Loading tasks…</p>
      ) : tasks.length === 0 ? (
        <p className="empty-state">No tasks match your filters.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Title</th>
              <th>Assigned to</th>
              <th>Status</th>
              <th>Priority</th>
              <th>Deadline</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {tasks.map((t) => (
              <tr key={t.id}>
                <td><Link to={`/tasks/${t.id}`}>{t.title}</Link></td>
                <td>{t.assignedUserName ?? '—'}</td>
                <td><span className={statusBadgeClass(t.status)}>{t.status}</span></td>
                <td><span className={priorityBadgeClass(t.priority)}>{t.priority}</span></td>
                <td>{t.deadline ? new Date(t.deadline).toLocaleDateString() : '—'}</td>
                <td>
                  {canCreate && (
                    <button className="btn btn-danger" onClick={() => handleDelete(t.id)}>
                      Delete
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
