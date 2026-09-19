using TaskManagement.Api.DTOs;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Interfaces
{
    public interface ITaskService
    {
        Task<List<TaskResponseDto>> GetTasksAsync(TaskFilterDto filter, int requestingUserId, UserRole requestingRole, int? requestingTeamId);

        /// <summary>Throws 403 if the requesting user is not authorized to view this specific task (the IDOR guard).</summary>
        Task<TaskResponseDto> GetTaskByIdAsync(int taskId, int requestingUserId, UserRole requestingRole, int? requestingTeamId);

        Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto, int requestingUserId, UserRole requestingRole, int? requestingTeamId);

        Task<TaskResponseDto> UpdateTaskAsync(int taskId, UpdateTaskDto dto, int requestingUserId, UserRole requestingRole, int? requestingTeamId);

        Task<TaskResponseDto> UpdateTaskStatusAsync(int taskId, Models.TaskStatus newStatus, int requestingUserId, UserRole requestingRole, int? requestingTeamId);

        Task DeleteTaskAsync(int taskId, UserRole requestingRole, int? requestingTeamId);
    }
}
