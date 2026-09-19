import { useEffect, useState } from 'react';
import { getDashboard } from '../services/dashboardService';
import { Link } from 'react-router-dom';

const STATUS_OPTIONS = ['ToDo', 'InProgress', 'Done'];
const PRIORITY_OPTIONS = ['Low', 'Medium', 'High'];

export default function DashboardPage() {
  const [data, setData] = useState(null);
  const [filters, setFilters] = useState({ status: '', priority: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters]);

  async function load() {
    setLoading(true);
    setError('');
    try {
      const params = {};
      if (filters.status) params.status = filters.status;
      if (filters.priority) params.priority = filters.priority;
      const result = await getDashboard(params);
      setData(result);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  if (loading && !data) return <p>Loading dashboard…</p>;
  if (error) return <div className="error-banner">{error}</div>;
  if (!data) return null;

  return (
    <div>
      <h1>Dashboard</h1>

      <div className="filters-bar">
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

      <div className="card-grid">
        <div className="card stat-card">
          <div className="value">{data.totalTasks}</div>
          <div className="label">Total tasks</div>
        </div>
        <div className="card stat-card">
          <div className="value">{data.toDoCount}</div>
          <div className="label">To Do</div>
        </div>
        <div className="card stat-card">
          <div className="value">{data.inProgressCount}</div>
          <div className="label">In Progress</div>
        </div>
        <div className="card stat-card">
          <div className="value">{data.doneCount}</div>
          <div className="label">Done</div>
        </div>
      </div>

      <div className="card-grid" style={{ marginTop: '1rem' }}>
        <div className="card">
          <h3>Tasks by user</h3>
          {data.tasksByUser.length === 0 && <p className="empty-state">No assigned tasks.</p>}
          <ul>
            {data.tasksByUser.map((u) => (
              <li key={u.userId}>
                {u.userName}: {u.count}
              </li>
            ))}
          </ul>
        </div>

        <div className="card">
          <h3>Tasks by priority</h3>
          <ul>
            {data.tasksByPriority.map((p) => (
              <li key={p.priority}>
                {p.priority}: {p.count}
              </li>
            ))}
          </ul>
        </div>
      </div>

      <div className="card">
        <h3>Upcoming deadlines</h3>
        {data.upcomingDeadlines.length === 0 && (
          <p className="empty-state">Nothing due soon.</p>
        )}
        <table>
          <thead>
            <tr>
              <th>Task</th>
              <th>Assigned to</th>
              <th>Deadline</th>
              <th>Priority</th>
            </tr>
          </thead>
          <tbody>
            {data.upcomingDeadlines.map((t) => (
              <tr key={t.id}>
                <td><Link to={`/tasks/${t.id}`}>{t.title}</Link></td>
                <td>{t.assignedUserName ?? '—'}</td>
                <td>{new Date(t.deadline).toLocaleDateString()}</td>
                <td>{t.priority}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
