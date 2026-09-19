import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import {
  getTask,
  updateTask,
  updateTaskStatus,
  deleteTask,
  getComments,
  addComment
} from '../services/taskService';

const STATUS_OPTIONS = ['ToDo', 'InProgress', 'Done'];
const PRIORITY_OPTIONS = ['Low', 'Medium', 'High'];

export default function TaskDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();

  const [task, setTask] = useState(null);
  const [comments, setComments] = useState([]);
  const [newComment, setNewComment] = useState('');
  const [editing, setEditing] = useState(false);
  const [editForm, setEditForm] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  const canManage = user.role === 'Admin' || user.role === 'Manager';

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  async function load() {
    setLoading(true);
    setError('');
    try {
      const [taskResult, commentsResult] = await Promise.all([getTask(id), getComments(id)]);
      setTask(taskResult);
      setComments(commentsResult);
      setEditForm({
        title: taskResult.title,
        description: taskResult.description ?? '',
        priority: taskResult.priority,
        deadline: taskResult.deadline ? taskResult.deadline.slice(0, 10) : ''
      });
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function handleStatusChange(status) {
    setError('');
    try {
      const updated = await updateTaskStatus(id, status);
      setTask(updated);
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleSaveEdit(e) {
    e.preventDefault();
    setError('');
    try {
      const updated = await updateTask(id, {
        title: editForm.title,
        description: editForm.description || null,
        assignedUserId: task.assignedUserId ?? null,
        priority: editForm.priority,
        deadline: editForm.deadline ? new Date(editForm.deadline).toISOString() : null
      });
      setTask(updated);
      setEditing(false);
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleDelete() {
    if (!confirm('Delete this task?')) return;
    try {
      await deleteTask(id);
      navigate('/tasks');
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleAddComment(e) {
    e.preventDefault();
    if (!newComment.trim()) return;
    setError('');
    try {
      const comment = await addComment(id, newComment.trim());
      setComments((c) => [...c, comment]);
      setNewComment('');
    } catch (err) {
      setError(err.message);
    }
  }

  if (loading) return <p>Loading task…</p>;
  if (error && !task) return <div className="error-banner">{error}</div>;
  if (!task) return null;

  const canUpdateStatus =
    user.role === 'Admin' || user.role === 'Manager' || task.assignedUserId === user.id;

  return (
    <div>
      <button className="btn btn-secondary" onClick={() => navigate('/tasks')}>
        ← Back to tasks
      </button>

      {error && <div className="error-banner">{error}</div>}

      <div className="card" style={{ marginTop: '1rem' }}>
        {editing ? (
          <form onSubmit={handleSaveEdit}>
            <div className="form-group">
              <label htmlFor="title">Title</label>
              <input
                id="title"
                value={editForm.title}
                onChange={(e) => setEditForm((f) => ({ ...f, title: e.target.value }))}
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="description">Description</label>
              <textarea
                id="description"
                rows={3}
                value={editForm.description}
                onChange={(e) => setEditForm((f) => ({ ...f, description: e.target.value }))}
              />
            </div>
            <div className="form-group">
              <label htmlFor="priority">Priority</label>
              <select
                id="priority"
                value={editForm.priority}
                onChange={(e) => setEditForm((f) => ({ ...f, priority: e.target.value }))}
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
                value={editForm.deadline}
                onChange={(e) => setEditForm((f) => ({ ...f, deadline: e.target.value }))}
              />
            </div>
            <button className="btn" type="submit">Save</button>{' '}
            <button className="btn btn-secondary" type="button" onClick={() => setEditing(false)}>
              Cancel
            </button>
          </form>
        ) : (
          <>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <h1>{task.title}</h1>
              {canManage && (
                <div>
                  <button className="btn btn-secondary" onClick={() => setEditing(true)}>
                    Edit
                  </button>{' '}
                  <button className="btn btn-danger" onClick={handleDelete}>
                    Delete
                  </button>
                </div>
              )}
            </div>
            <p>{task.description || <em>No description.</em>}</p>
            <p>
              <strong>Team:</strong> {task.teamName} &nbsp;|&nbsp;
              <strong> Assigned to:</strong> {task.assignedUserName ?? 'Unassigned'} &nbsp;|&nbsp;
              <strong> Created by:</strong> {task.createdByName}
            </p>
            <p>
              <strong>Priority:</strong> {task.priority} &nbsp;|&nbsp;
              <strong> Deadline:</strong>{' '}
              {task.deadline ? new Date(task.deadline).toLocaleDateString() : '—'}
            </p>
          </>
        )}

        <div className="form-group" style={{ marginTop: '1rem' }}>
          <label htmlFor="status">Status</label>
          <select
            id="status"
            value={task.status}
            disabled={!canUpdateStatus}
            onChange={(e) => handleStatusChange(e.target.value)}
          >
            {STATUS_OPTIONS.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="card">
        <h3>Comments</h3>
        <div className="comment-list">
          {comments.length === 0 && <p className="empty-state">No comments yet.</p>}
          {comments.map((c) => (
            <div key={c.id} className="comment-item">
              <div>{c.commentText}</div>
              <div className="comment-meta">
                {c.userName} · {new Date(c.createdAt).toLocaleString()}
              </div>
            </div>
          ))}
        </div>
        <form onSubmit={handleAddComment}>
          <div className="form-group">
            <textarea
              rows={2}
              placeholder="Add a comment…"
              value={newComment}
              onChange={(e) => setNewComment(e.target.value)}
            />
          </div>
          <button className="btn" type="submit">Post comment</button>
        </form>
      </div>
    </div>
  );
}
