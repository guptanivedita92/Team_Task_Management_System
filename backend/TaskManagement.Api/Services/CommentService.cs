using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Services
{
    public class CommentService : ICommentService
    {
        private readonly AppDbContext _db;

        public CommentService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<CommentResponseDto>> GetCommentsAsync(int taskId, int requestingUserId, UserRole requestingRole, int? requestingTeamId)
        {
            var task = await GetTaskWithAccessCheckAsync(taskId, requestingUserId, requestingRole, requestingTeamId);

            var comments = await _db.Comments
                .Include(c => c.User)
                .Where(c => c.TaskId == task.Id)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            return comments.Select(MapToDto).ToList();
        }

        public async Task<CommentResponseDto> AddCommentAsync(int taskId, CreateCommentDto dto, int requestingUserId, UserRole requestingRole, int? requestingTeamId)
        {
            var task = await GetTaskWithAccessCheckAsync(taskId, requestingUserId, requestingRole, requestingTeamId);

            var comment = new Comment
            {
                TaskId = task.Id,
                UserId = requestingUserId,
                CommentText = dto.CommentText.Trim()
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            await _db.Entry(comment).Reference(c => c.User).LoadAsync();
            return MapToDto(comment);
        }

        // Comment access mirrors task view access exactly: anyone who can view the task
        // can view/add its comments, and no one else - checked server-side, not by hiding UI.
        private async Task<TaskItem> GetTaskWithAccessCheckAsync(int taskId, int requestingUserId, UserRole requestingRole, int? requestingTeamId)
        {
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Task not found.");

            var allowed = requestingRole switch
            {
                UserRole.Admin => true,
                UserRole.Manager => task.TeamId == requestingTeamId,
                UserRole.User => task.AssignedUserId == requestingUserId,
                _ => false
            };

            if (!allowed)
            {
                throw new ApiException(StatusCodes.Status403Forbidden, "You are not authorized to access comments on this task.");
            }

            return task;
        }

        private static CommentResponseDto MapToDto(Comment comment) => new()
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            UserId = comment.UserId,
            UserName = comment.User?.FullName ?? string.Empty,
            CommentText = comment.CommentText,
            CreatedAt = comment.CreatedAt
        };
    }
}
