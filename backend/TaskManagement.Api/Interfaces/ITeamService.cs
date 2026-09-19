using TaskManagement.Api.DTOs;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Interfaces
{
    public interface ITeamService
    {
        Task<List<TeamResponseDto>> GetTeamsAsync(int requestingUserId, UserRole requestingRole, int? requestingTeamId);
        Task<TeamDetailDto> GetTeamByIdAsync(int teamId, UserRole requestingRole, int? requestingTeamId);
        Task<TeamResponseDto> CreateTeamAsync(CreateTeamDto dto);
        Task<TeamResponseDto> UpdateTeamAsync(int teamId, UpdateTeamDto dto);
        Task DeleteTeamAsync(int teamId);

        /// <summary>Admin: any team. Manager: only their own.</summary>
        Task<TeamDetailDto> AddMemberAsync(int teamId, int userId, UserRole requestingRole, int? requestingTeamId);
        Task<TeamDetailDto> RemoveMemberAsync(int teamId, int userId, UserRole requestingRole, int? requestingTeamId);
    }
}
