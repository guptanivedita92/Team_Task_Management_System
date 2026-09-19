using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Services
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _db;
        private readonly INotificationService _notifications;

        public TaskService(AppDbContext db, INotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        public async Task<List<TaskResponseDto>> GetTasksAsync(TaskFilterDto filter, int requestingUserId, UserRole requestingRole, int? requestingTeamId)
        {
            IQueryable<TaskItem> query = _db.Tasks
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedBy)
                .Include(t => t.Team);

            // Scope BEFORE applying filters, so a User/Manager can never widen their
            // view just by omitting a filter value.
            query = requestingRole switch
            {
                UserRole.Admin => query,
                UserRole.Manager => requestingTeamId.HasValue
                    ? query.Where(t => t.TeamId == requestingTeamId.Value)
                    : query.Where(t => false),
                UserRole.User => query.Where(t => t.AssignedUserId == requestingUserId),
                _ => query.Where(t => false)
            };

            if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status.Value);
            if (filter.Priority.HasValue) query = query.Where(t => t.Priority == filter.Priority.Value);
            if (filter.DeadlineFrom.HasValue) query = query.Where(t => t.Deadline >= filter.DeadlineFrom.Value);
            if (filter.DeadlineTo.HasValue) query = query.Where(t => t.Deadline <= filter.DeadlineTo.Value);
            if (filter.AssignedUserId.HasValue) query = query.Where(t => t.AssignedUserId == filter.AssignedUserId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim();
                query = query.Where(t => t.Title.Contains(term) || (t.Description != null && t.Description.Contains(term)));
            }

            var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return tasks.Select(MapToDto).ToList();
        }

        public async Task<TaskResponseDto> GetTaskByIdAsync(int taskId, int requestingUserId, UserRole requestingRole, int? requestingTeamId)
        {
            var task = await LoadTaskAsync(taskId);
            EnsureCanView(task, requestingUserId, requestingRole, requestingTeamId);
            return MapToDto(task);
        }

        public async Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto, int requestingUserId, UserRole requestingRole, int? requestingTeamId)
        {
            if (requestingRole == UserRole.User)
            {
                throw new ApiException(StatusCodes.Status403Forbidden, "Users are not authorized to create tasks.");
            }

            if (requestingRole == UserRole.Manager && dto.TeamId != requestingTeamId)
            {
                throw new ApiException(StatusCodes.Status403Forbidden, "Managers can only create tasks for their own team.");
            }

            var team = await _db.Teams.FindAsync(dto.TeamId)
                ?? throw new ApiException(StatusCodes.Status400BadRequest, "The specified team does not exist.");

            if (dto.AssignedUserId.HasValue)
            {
                await EnsureAssigneeBelongsToTeamAsync(dto.AssignedUserId.Value, dto.TeamId);
            }

            var task = new TaskItem
            {
                Title = dto.Title.Trim(),
                Description = dto.Description,
                AssignedUserId = dto.AssignedUserId,
                CreatedById = requestingUserId,
                TeamId = dto.TeamId,
                Priority = dto.Priority,
                Status = Models.TaskStatus.ToDo,
                Deadline = dto.Deadline
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            if (dto.AssignedUserId.HasValue)
            {
                await _notifications.NotifyAsync(
                    dto.AssignedUserId.Value,
                    $"You have been assigned a new task: \"{task.Title}\".",
                    NotificationType.TaskAssigned);
            }

            return MapToDto(await LoadTaskAsync(task.Id));
        }

        public async Task<TaskResponseDto> UpdateTaskAsync(int taskId, UpdateTaskDto dto, int requestingUserId, UserRole requestingRole, int? requestingTeamId)
        {
            var task = await LoadTaskAsync(taskId);
            EnsureCanManage(task, requestingRole, requestingTeamId);

            var previousAssignee = task.AssignedUserId;

            if (dto.AssignedUserId.HasValue && dto.AssignedUserId != task.AssignedUserId)
            {
                await EnsureAssigneeBelongsToTeamAsync(dto.AssignedUserId.Value, task.TeamId);
            }

            task.Title = dto.Title.Trim();
            task.Description = dto.Description;
            task.AssignedUserId = dto.AssignedUserId;
            task.Priority = dto.Priority;
            task.Deadline = dto.Deadline;
            task.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            if (dto.AssignedUserId.HasValue && dto.AssignedUserId != previousAssignee)
            {
                await _notifications.NotifyAsync(
                    dto.AssignedUserId.Value,
                    $"You have been assigned to task: \"{task.Title}\".",
                    NotificationType.TaskAssigned);
            }

            return MapToDto(await LoadTaskAsync(task.Id));
        }

        public async Task<TaskResponseDto> UpdateTaskStatusAsync(int taskId, Models.TaskStatus newStatus, int requestingUserId, UserRole requestingRole, int? requestingTeamId)
        {
            var task = await LoadTaskAsync(taskId);

            var allowed = requestingRole switch
            {
                UserRole.Admin => true,
                UserRole.Manager => task.TeamId == requestingTeamId,
                UserRole.User => task.AssignedUserId == requestingUserId,
                _ => false
            };

            if (!allowed)
            {
                // Deliberately 404 (not 403) here would leak less, but the assignment calls for
                // clear Forbidden semantics on role restriction violations - matches section 11.
                throw new ApiException(StatusCodes.Status403Forbidden, "You are not authorized to update this task's status.");
            }

            task.Status = newStatus;
            task.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Notify whoever isn't the person making the change: the creator (if not them)
            // and the assignee (if not them and not the same as the creator).
            var recipients = new HashSet<int>();
            if (task.CreatedById != requestingUserId) recipients.Add(task.CreatedById);
            if (task.AssignedUserId.HasValue && task.AssignedUserId.Value != requestingUserId) recipients.Add(task.AssignedUserId.Value);

            foreach (var recipientId in recipients)
            {
                await _notifications.NotifyAsync(
                    recipientId,
                    $"Task \"{task.Title}\" status changed to {newStatus}.",
                    NotificationType.StatusUpdated);
            }

            return MapToDto(await LoadTaskAsync(task.Id));
        }

        public async Task DeleteTaskAsync(int taskId, UserRole requestingRole, int? requestingTeamId)
        {
            if (requestingRole == UserRole.User)
            {
                throw new ApiException(StatusCodes.Status403Forbidden, "Users are not authorized to delete tasks.");
            }

            var task = await _db.Tasks.FindAsync(taskId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Task not found.");

            if (requestingRole == UserRole.Manager && task.TeamId != requestingTeamId)
            {
                throw new ApiException(StatusCodes.Status403Forbidden, "Managers can only delete tasks belonging to their own team.");
            }

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
        }

        // ---------- helpers ----------

        private async Task<TaskItem> LoadTaskAsync(int taskId)
        {
            return await _db.Tasks
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedBy)
                .Include(t => t.Team)
                .FirstOrDefaultAsync(t => t.Id == taskId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Task not found.");
        }

        private static void EnsureCanView(TaskItem task, int requestingUserId, UserRole requestingRole, int? requestingTeamId)
        {
            var allowed = requestingRole switch
            {
                UserRole.Admin => true,
                UserRole.Manager => task.TeamId == requestingTeamId,
                UserRole.User => task.AssignedUserId == requestingUserId,
                _ => false
            };

            if (!allowed)
            {
                // This is the guard that stops "change the :id in the URL" IDOR attempts -
                // a user cannot view a task by guessing/incrementing its id.
                throw new ApiException(StatusCodes.Status403Forbidden, "You are not authorized to view this task.");
            }
        }

        private static void EnsureCanManage(TaskItem task, UserRole requestingRole, int? requestingTeamId)
        {
            var allowed = requestingRole switch
            {
                UserRole.Admin => true,
                UserRole.Manager => task.TeamId == requestingTeamId,
                UserRole.User => false,
                _ => false
            };

            if (!allowed)
            {
                throw new ApiException(StatusCodes.Status403Forbidden, "You are not authorized to modify this task.");
            }
        }

        private async Task EnsureAssigneeBelongsToTeamAsync(int userId, int teamId)
        {
            var belongs = await _db.Users.AnyAsync(u => u.Id == userId && u.TeamId == teamId);
            if (!belongs)
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "The assigned user must be a member of the task's team.");
            }
        }

        private static TaskResponseDto MapToDto(TaskItem task) => new()
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            AssignedUserId = task.AssignedUserId,
            AssignedUserName = task.AssignedUser?.FullName,
            CreatedById = task.CreatedById,
            CreatedByName = task.CreatedBy?.FullName ?? string.Empty,
            TeamId = task.TeamId,
            TeamName = task.Team?.Name ?? string.Empty,
            Priority = task.Priority,
            Status = task.Status,
            Deadline = task.Deadline,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
