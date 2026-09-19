using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<UserResponseDto>> GetUsersAsync(int requestingUserId, UserRole requestingRole)
        {
            IQueryable<User> query = _db.Users.Include(u => u.Team);

            if (requestingRole == UserRole.Manager)
            {
                // Managers only see users on their own team (not the whole org).
                var requester = await _db.Users.FindAsync(requestingUserId)
                    ?? throw new ApiException(StatusCodes.Status401Unauthorized, "Requesting user not found.");
                query = query.Where(u => u.TeamId != null && u.TeamId == requester.TeamId);
            }
            else if (requestingRole == UserRole.User)
            {
                // Plain users have no business listing other users; restrict to themselves.
                query = query.Where(u => u.Id == requestingUserId);
            }
            // Admin: no filter, sees everyone.

            var users = await query.OrderBy(u => u.FullName).ToListAsync();
            return users.Select(MapToDto).ToList();
        }

        public async Task<UserResponseDto> GetUserByIdAsync(int targetUserId, int requestingUserId, UserRole requestingRole, int? requestingTeamId)
        {
            var user = await _db.Users.Include(u => u.Team).FirstOrDefaultAsync(u => u.Id == targetUserId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found.");

            var allowed = requestingRole switch
            {
                UserRole.Admin => true,
                UserRole.Manager => user.TeamId != null && user.TeamId == requestingTeamId,
                UserRole.User => user.Id == requestingUserId,
                _ => false
            };

            if (!allowed)
            {
                throw new ApiException(StatusCodes.Status403Forbidden, "You are not authorized to view this user.");
            }

            return MapToDto(user);
        }

        public async Task<UserResponseDto> GetCurrentUserAsync(int userId)
        {
            var user = await _db.Users.Include(u => u.Team).FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found.");
            return MapToDto(user);
        }

        public async Task<UserResponseDto> UpdateUserRoleAsync(int targetUserId, UserRole newRole)
        {
            var user = await _db.Users.Include(u => u.Team).FirstOrDefaultAsync(u => u.Id == targetUserId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found.");

            user.Role = newRole;
            await _db.SaveChangesAsync();

            return MapToDto(user);
        }

        public async Task<UserResponseDto> AssignUserToTeamAsync(int targetUserId, int? teamId, int requestingUserId, UserRole requestingRole, int? requestingTeamId)
        {
            if (requestingRole == UserRole.Manager)
            {
                // A manager may only pull users into their own team, and may not move
                // a user out to some other team (that would touch a team they don't own).
                if (teamId != null && teamId != requestingTeamId)
                {
                    throw new ApiException(StatusCodes.Status403Forbidden, "Managers can only assign users to their own team.");
                }
            }

            var user = await _db.Users.Include(u => u.Team).FirstOrDefaultAsync(u => u.Id == targetUserId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found.");

            if (requestingRole == UserRole.Manager && user.TeamId != null && user.TeamId != requestingTeamId && teamId == null)
            {
                // Manager trying to remove a user from a team they don't manage.
                throw new ApiException(StatusCodes.Status403Forbidden, "Managers can only manage members of their own team.");
            }

            if (teamId.HasValue)
            {
                var teamExists = await _db.Teams.AnyAsync(t => t.Id == teamId.Value);
                if (!teamExists)
                {
                    throw new ApiException(StatusCodes.Status400BadRequest, "The specified team does not exist.");
                }
            }

            user.TeamId = teamId;
            await _db.SaveChangesAsync();

            await _db.Entry(user).Reference(u => u.Team).LoadAsync();
            return MapToDto(user);
        }

        private static UserResponseDto MapToDto(User user) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            TeamId = user.TeamId,
            TeamName = user.Team?.Name,
            CreatedAt = user.CreatedAt
        };
    }
}
