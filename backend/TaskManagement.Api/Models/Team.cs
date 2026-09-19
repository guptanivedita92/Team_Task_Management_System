using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Api.Models
{
    public class Team
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public int? ManagerId { get; set; }
        public User? Manager { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<User> Members { get; set; } = new List<User>();
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
