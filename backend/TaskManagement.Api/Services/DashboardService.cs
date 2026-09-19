using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _db;

        public DashboardService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardResponseDto> GetDashboardAsync(TaskFilterDto filter, int requestingUserId, UserRole requestingRole, int? requestingTeamId)
        {
            // Same scoping rule as TaskService.GetTasksAsync - the dashboard must never
            // reveal counts/tasks the caller couldn't otherwise see individually.
            IQueryable<TaskItem> query = _db.Tasks
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedBy)
                .Include(t => t.Team);

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

            var tasks = await query.ToListAsync();

            var response = new DashboardResponseDto
            {
                TotalTasks = tasks.Count,
                ToDoCount = tasks.Count(t => t.Status == Models.TaskStatus.ToDo),
                InProgressCount = tasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                DoneCount = tasks.Count(t => t.Status == Models.TaskStatus.Done),

                TasksByUser = tasks
                    .Where(t => t.AssignedUserId.HasValue)
                    .GroupBy(t => new { t.AssignedUserId, Name = t.AssignedUser!.FullName })
                    .Select(g => new TasksByUserDto { UserId = g.Key.AssignedUserId!.Value, UserName = g.Key.Name, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList(),

                TasksByPriority = tasks
                    .GroupBy(t => t.Priority)
                    .Select(g => new TasksByPriorityDto { Priority = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Priority)
                    .ToList(),

                UpcomingDeadlines = tasks
                    .Where(t => t.Deadline.HasValue && t.Deadline.Value >= DateTime.UtcNow && t.Status != Models.TaskStatus.Done)
                    .OrderBy(t => t.Deadline)
                    .Take(10)
                    .Select(t => new TaskResponseDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        AssignedUserId = t.AssignedUserId,
                        AssignedUserName = t.AssignedUser?.FullName,
                        CreatedById = t.CreatedById,
                        CreatedByName = t.CreatedBy?.FullName ?? string.Empty,
                        TeamId = t.TeamId,
                        TeamName = t.Team?.Name ?? string.Empty,
                        Priority = t.Priority,
                        Status = t.Status,
                        Deadline = t.Deadline,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt
                    })
                    .ToList()
            };

            return response;
        }
    }
}
