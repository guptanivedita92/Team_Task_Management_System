import axios from 'axios';

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

const api = axios.create({ baseURL });

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('ttms_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Central place to react to auth failures / surface a consistent error message.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('ttms_token');
      localStorage.removeItem('ttms_user');
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }

    const message =
      error.response?.data?.message ||
      error.response?.data?.errors?.[Object.keys(error.response?.data?.errors ?? {})[0]]?.[0] ||
      error.message ||
      'Something went wrong. Please try again.';

    return Promise.reject(new Error(message));
  }
);

export default api;
