using System.ComponentModel.DataAnnotations;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.DTOs
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Admin-only: update another user's role/team.
    public class UpdateUserRoleDto
    {
        [Required]
        public UserRole Role { get; set; }
    }

    public class AssignUserTeamDto
    {
        // Null clears the assignment.
        public int? TeamId { get; set; }
    }
}
