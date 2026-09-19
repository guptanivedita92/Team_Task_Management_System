import api from './api';

export async function getTeams() {
  const { data } = await api.get('/teams');
  return data;
}

export async function getTeam(id) {
  const { data } = await api.get(`/teams/${id}`);
  return data;
}

export async function createTeam(payload) {
  const { data } = await api.post('/teams', payload);
  return data;
}

export async function updateTeam(id, payload) {
  const { data } = await api.put(`/teams/${id}`, payload);
  return data;
}

export async function deleteTeam(id) {
  await api.delete(`/teams/${id}`);
}

export async function addTeamMember(teamId, userId) {
  const { data } = await api.post(`/teams/${teamId}/members`, { userId });
  return data;
}

export async function removeTeamMember(teamId, userId) {
  const { data } = await api.delete(`/teams/${teamId}/members/${userId}`);
  return data;
}
