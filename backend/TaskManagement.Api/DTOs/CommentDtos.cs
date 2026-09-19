using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Api.DTOs
{
    public class CreateCommentDto
    {
        [Required, MaxLength(2000)]
        public string CommentText { get; set; } = string.Empty;
    }

    public class CommentResponseDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string CommentText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
