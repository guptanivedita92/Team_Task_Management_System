using TaskManagement.Api.DTOs;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardResponseDto> GetDashboardAsync(TaskFilterDto filter, int requestingUserId, UserRole requestingRole, int? requestingTeamId);
    }
}
