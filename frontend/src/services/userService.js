import api from './api';

export async function getUsers() {
  const { data } = await api.get('/users');
  return data;
}

export async function getCurrentUser() {
  const { data } = await api.get('/users/me');
  return data;
}

export async function updateUserRole(userId, role) {
  const { data } = await api.patch(`/users/${userId}/role`, { role });
  return data;
}

export async function assignUserTeam(userId, teamId) {
  const { data } = await api.patch(`/users/${userId}/team`, { teamId });
  return data;
}
