import api from './api';

export async function getDashboard(filters = {}) {
  const { data } = await api.get('/dashboard', { params: filters });
  return data;
}
