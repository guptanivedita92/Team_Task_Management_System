using System.ComponentModel.DataAnnotations;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.DTOs
{
    public class CreateTaskDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public int? AssignedUserId { get; set; }

        [Required]
        public int TeamId { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime? Deadline { get; set; }
    }

    public class UpdateTaskDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public int? AssignedUserId { get; set; }

        public TaskPriority Priority { get; set; }

        public DateTime? Deadline { get; set; }
    }

    public class UpdateTaskStatusDto
    {
        [Required]
        public Models.TaskStatus Status { get; set; }
    }

    public class TaskResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public TaskPriority Priority { get; set; }
        public Models.TaskStatus Status { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Bound from query string on GET /api/tasks
    public class TaskFilterDto
    {
        public Models.TaskStatus? Status { get; set; }
        public TaskPriority? Priority { get; set; }
        public DateTime? DeadlineFrom { get; set; }
        public DateTime? DeadlineTo { get; set; }
        public int? AssignedUserId { get; set; }
        public string? Search { get; set; }
    }
}
