using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Helpers;
using TaskManagement.Api.Interfaces;

namespace TaskManagement.Api.Controllers
{
    [ApiController]
    [Route("api/tasks/{taskId:int}/comments")]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetComments(int taskId)
        {
            var comments = await _commentService.GetCommentsAsync(taskId, User.GetUserId(), User.GetRole(), User.GetTeamId());
            return Ok(comments);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int taskId, [FromBody] CreateCommentDto dto)
        {
            var comment = await _commentService.AddCommentAsync(taskId, dto, User.GetUserId(), User.GetRole(), User.GetTeamId());
            return CreatedAtAction(nameof(GetComments), new { taskId }, comment);
        }
    }
}
