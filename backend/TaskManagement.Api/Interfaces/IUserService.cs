using TaskManagement.Api.DTOs;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Interfaces
{
    public interface IUserService
    {
        /// <summary>Admin: all users. Manager: only users on their own team.</summary>
        Task<List<UserResponseDto>> GetUsersAsync(int requestingUserId, UserRole requestingRole);

        Task<UserResponseDto> GetUserByIdAsync(int targetUserId, int requestingUserId, UserRole requestingRole, int? requestingTeamId);

        Task<UserResponseDto> GetCurrentUserAsync(int userId);

        /// <summary>Admin only.</summary>
        Task<UserResponseDto> UpdateUserRoleAsync(int targetUserId, UserRole newRole);

        /// <summary>Admin: any user to any team. Manager: only into their own team.</summary>
        Task<UserResponseDto> AssignUserToTeamAsync(int targetUserId, int? teamId, int requestingUserId, UserRole requestingRole, int? requestingTeamId);
    }
}
