import { useEffect, useState } from 'react';
import {
  getNotifications,
  markNotificationRead,
  markAllNotificationsRead
} from '../services/notificationService';

export default function NotificationsPage() {
  const [notifications, setNotifications] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    load();
  }, []);

  async function load() {
    setLoading(true);
    setError('');
    try {
      setNotifications(await getNotifications());
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function handleMarkRead(id) {
    try {
      await markNotificationRead(id);
      setNotifications((list) => list.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleMarkAll() {
    try {
      await markAllNotificationsRead();
      setNotifications((list) => list.map((n) => ({ ...n, isRead: true })));
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1>Notifications</h1>
        <button className="btn btn-secondary" onClick={handleMarkAll}>
          Mark all as read
        </button>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {loading ? (
        <p>Loading notifications…</p>
      ) : notifications.length === 0 ? (
        <p className="empty-state">No notifications yet.</p>
      ) : (
        <div className="card-grid" style={{ gridTemplateColumns: '1fr' }}>
          {notifications.map((n) => (
            <div
              key={n.id}
              className="card"
              style={{ opacity: n.isRead ? 0.6 : 1, cursor: n.isRead ? 'default' : 'pointer' }}
              onClick={() => !n.isRead && handleMarkRead(n.id)}
            >
              <p style={{ margin: 0 }}>{n.message}</p>
              <div className="comment-meta">
                {n.type} · {new Date(n.createdAt).toLocaleString()}
                {!n.isRead && ' · unread'}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
