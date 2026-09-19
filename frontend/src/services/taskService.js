import api from './api';

export async function getTasks(filters = {}) {
  const { data } = await api.get('/tasks', { params: filters });
  return data;
}

export async function getTask(id) {
  const { data } = await api.get(`/tasks/${id}`);
  return data;
}

export async function createTask(payload) {
  const { data } = await api.post('/tasks', payload);
  return data;
}

export async function updateTask(id, payload) {
  const { data } = await api.put(`/tasks/${id}`, payload);
  return data;
}

export async function updateTaskStatus(id, status) {
  const { data } = await api.patch(`/tasks/${id}/status`, { status });
  return data;
}

export async function deleteTask(id) {
  await api.delete(`/tasks/${id}`);
}

export async function getComments(taskId) {
  const { data } = await api.get(`/tasks/${taskId}/comments`);
  return data;
}

export async function addComment(taskId, commentText) {
  const { data } = await api.post(`/tasks/${taskId}/comments`, { commentText });
  return data;
}
