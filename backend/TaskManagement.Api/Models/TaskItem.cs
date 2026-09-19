using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Api.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public int? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }

        [Required]
        public int CreatedById { get; set; }
        public User CreatedBy { get; set; } = null!;

        [Required]
        public int TeamId { get; set; }
        public Team Team { get; set; } = null!;

        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        [Required]
        public Models.TaskStatus Status { get; set; } = Models.TaskStatus.ToDo;

        public DateTime? Deadline { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
