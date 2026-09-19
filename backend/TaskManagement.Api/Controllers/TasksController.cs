using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Helpers;
using TaskManagement.Api.Interfaces;

namespace TaskManagement.Api.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        /// <summary>Scoped automatically by role: Admin sees all, Manager their team, User their own assigned tasks.</summary>
        [HttpGet]
        public async Task<IActionResult> GetTasks([FromQuery] TaskFilterDto filter)
        {
            var tasks = await _taskService.GetTasksAsync(filter, User.GetUserId(), User.GetRole(), User.GetTeamId());
            return Ok(tasks);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTask(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id, User.GetUserId(), User.GetRole(), User.GetTeamId());
            return Ok(task);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
        {
            var task = await _taskService.CreateTaskAsync(dto, User.GetUserId(), User.GetRole(), User.GetTeamId());
            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto dto)
        {
            var task = await _taskService.UpdateTaskAsync(id, dto, User.GetUserId(), User.GetRole(), User.GetTeamId());
            return Ok(task);
        }

        /// <summary>Available to Admin/Manager (any task they manage) and User (only their own assigned task).</summary>
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTaskStatusDto dto)
        {
            var task = await _taskService.UpdateTaskStatusAsync(id, dto.Status, User.GetUserId(), User.GetRole(), User.GetTeamId());
            return Ok(task);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            await _taskService.DeleteTaskAsync(id, User.GetRole(), User.GetTeamId());
            return NoContent();
        }
    }
}
