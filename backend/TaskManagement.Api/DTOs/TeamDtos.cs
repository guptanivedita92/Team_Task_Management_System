using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Api.DTOs
{
    public class CreateTeamDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public int? ManagerId { get; set; }
    }

    public class UpdateTeamDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public int? ManagerId { get; set; }
    }

    public class TeamResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TeamDetailDto : TeamResponseDto
    {
        public List<UserResponseDto> Members { get; set; } = new();
    }

    public class AddTeamMemberDto
    {
        [Required]
        public int UserId { get; set; }
    }
}
