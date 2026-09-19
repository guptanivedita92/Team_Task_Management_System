import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

// This only controls what's shown in the UI. Every real authorization decision
// happens server-side (see backend TaskService/UserService/TeamService) - this
// component just avoids showing a page the API would reject anyway.
export default function ProtectedRoute({ children, roles }) {
  const { user } = useAuth();
  const location = useLocation();

  if (!user) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (roles && !roles.includes(user.role)) {
    return <Navigate to="/dashboard" replace />;
  }

  return children;
}
