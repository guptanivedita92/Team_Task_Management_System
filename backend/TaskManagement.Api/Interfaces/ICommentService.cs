using TaskManagement.Api.DTOs;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Interfaces
{
    public interface ICommentService
    {
        Task<List<CommentResponseDto>> GetCommentsAsync(int taskId, int requestingUserId, UserRole requestingRole, int? requestingTeamId);
        Task<CommentResponseDto> AddCommentAsync(int taskId, CreateCommentDto dto, int requestingUserId, UserRole requestingRole, int? requestingTeamId);
    }
}
