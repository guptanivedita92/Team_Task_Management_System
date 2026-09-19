using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Services
{
    public class TeamService : ITeamService
    {
        private readonly AppDbContext _db;

        public TeamService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<TeamResponseDto>> GetTeamsAsync(int requestingUserId, UserRole requestingRole, int? requestingTeamId)
        {
            IQueryable<Team> query = _db.Teams.Include(t => t.Manager).Include(t => t.Members);

            if (requestingRole != UserRole.Admin)
            {
                // Manager/User only see their own team - not the whole org's team list.
                query = requestingTeamId.HasValue
                    ? query.Where(t => t.Id == requestingTeamId.Value)
                    : query.Where(t => false);
            }

            var teams = await query.OrderBy(t => t.Name).ToListAsync();
            return teams.Select(MapToSummary).ToList();
        }

        public async Task<TeamDetailDto> GetTeamByIdAsync(int teamId, UserRole requestingRole, int? requestingTeamId)
        {
            if (requestingRole != UserRole.Admin && requestingTeamId != teamId)
            {
                throw new ApiException(StatusCodes.Status403Forbidden, "You are not authorized to view this team.");
            }

            var team = await _db.Teams
                .Include(t => t.Manager)
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Team not found.");

            return MapToDetail(team);
        }

        public async Task<TeamResponseDto> CreateTeamAsync(CreateTeamDto dto)
        {
            var nameExists = await _db.Teams.AnyAsync(t => t.Name == dto.Name);
            if (nameExists)
            {
                throw new ApiException(StatusCodes.Status409Conflict, "A team with this name already exists.");
            }

            if (dto.ManagerId.HasValue)
            {
                var manager = await _db.Users.FindAsync(dto.ManagerId.Value)
                    ?? throw new ApiException(StatusCodes.Status400BadRequest, "The specified manager does not exist.");
                if (manager.Role != UserRole.Manager && manager.Role != UserRole.Admin)
                {
                    throw new ApiException(StatusCodes.Status400BadRequest, "Assigned manager must have the Manager (or Admin) role.");
                }
            }

            var team = new Team { Name = dto.Name.Trim(), ManagerId = dto.ManagerId };
            _db.Teams.Add(team);
            await _db.SaveChangesAsync();

            await _db.Entry(team).Reference(t => t.Manager).LoadAsync();
            return MapToSummary(team);
        }

        public async Task<TeamResponseDto> UpdateTeamAsync(int teamId, UpdateTeamDto dto)
        {
            var team = await _db.Teams.Include(t => t.Manager).Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Team not found.");

            var nameTaken = await _db.Teams.AnyAsync(t => t.Id != teamId && t.Name == dto.Name);
            if (nameTaken)
            {
                throw new ApiException(StatusCodes.Status409Conflict, "A team with this name already exists.");
            }

            team.Name = dto.Name.Trim();
            team.ManagerId = dto.ManagerId;
            await _db.SaveChangesAsync();

            return MapToSummary(team);
        }

        public async Task DeleteTeamAsync(int teamId)
        {
            var team = await _db.Teams.Include(t => t.Members).Include(t => t.Tasks)
                .FirstOrDefaultAsync(t => t.Id == teamId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Team not found.");

            if (team.Tasks.Any())
            {
                throw new ApiException(StatusCodes.Status409Conflict,
                    "Cannot delete a team that still has tasks. Reassign or delete its tasks first.");
            }

            // Deleting the team just detaches members (FK is SetNull) rather than deleting users.
            _db.Teams.Remove(team);
            await _db.SaveChangesAsync();
        }

        public async Task<TeamDetailDto> AddMemberAsync(int teamId, int userId, UserRole requestingRole, int? requestingTeamId)
        {
            EnsureCanManageTeam(teamId, requestingRole, requestingTeamId);

            var team = await _db.Teams.Include(t => t.Manager).Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Team not found.");

            var user = await _db.Users.FindAsync(userId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found.");

            user.TeamId = teamId;
            await _db.SaveChangesAsync();

            await _db.Entry(team).ReloadAsync();
            await _db.Entry(team).Collection(t => t.Members).LoadAsync();
            return MapToDetail(team);
        }

        public async Task<TeamDetailDto> RemoveMemberAsync(int teamId, int userId, UserRole requestingRole, int? requestingTeamId)
        {
            EnsureCanManageTeam(teamId, requestingRole, requestingTeamId);

            var team = await _db.Teams.Include(t => t.Manager).Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Team not found.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TeamId == teamId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "User is not a member of this team.");

            user.TeamId = null;
            await _db.SaveChangesAsync();

            await _db.Entry(team).Collection(t => t.Members).LoadAsync();
            return MapToDetail(team);
        }

        private static void EnsureCanManageTeam(int teamId, UserRole requestingRole, int? requestingTeamId)
        {
            if (requestingRole == UserRole.Manager && requestingTeamId != teamId)
            {
                throw new ApiException(StatusCodes.Status403Forbidden, "Managers can only manage members of their own team.");
            }
        }

        private static TeamResponseDto MapToSummary(Team team) => new()
        {
            Id = team.Id,
            Name = team.Name,
            ManagerId = team.ManagerId,
            ManagerName = team.Manager?.FullName,
            MemberCount = team.Members?.Count ?? 0,
            CreatedAt = team.CreatedAt
        };

        private static TeamDetailDto MapToDetail(Team team)
        {
            var summary = MapToSummary(team);
            return new TeamDetailDto
            {
                Id = summary.Id,
                Name = summary.Name,
                ManagerId = summary.ManagerId,
                ManagerName = summary.ManagerName,
                MemberCount = summary.MemberCount,
                CreatedAt = summary.CreatedAt,
                Members = team.Members.Select(m => new UserResponseDto
                {
                    Id = m.Id,
                    FullName = m.FullName,
                    Email = m.Email,
                    Role = m.Role,
                    TeamId = m.TeamId,
                    TeamName = team.Name,
                    CreatedAt = m.CreatedAt
                }).ToList()
            };
        }
    }
}
