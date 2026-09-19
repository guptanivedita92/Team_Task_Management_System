using System.Security.Claims;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Helpers
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (value == null || !int.TryParse(value, out var id))
            {
                throw new InvalidOperationException("User id claim missing or invalid.");
            }
            return id;
        }

        public static UserRole GetRole(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirstValue(ClaimTypes.Role);
            if (value == null || !Enum.TryParse<UserRole>(value, out var role))
            {
                throw new InvalidOperationException("Role claim missing or invalid.");
            }
            return role;
        }

        public static int? GetTeamId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirstValue("teamId");
            return value != null && int.TryParse(value, out var teamId) ? teamId : null;
        }
    }
}
