using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Helpers;
using TaskManagement.Api.Interfaces;

namespace TaskManagement.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>Admin: all users. Manager: users on their own team. User: just themselves.</summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userService.GetUsersAsync(User.GetUserId(), User.GetRole());
            return Ok(users);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _userService.GetCurrentUserAsync(User.GetUserId());
            return Ok(user);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id, User.GetUserId(), User.GetRole(), User.GetTeamId());
            return Ok(user);
        }

        /// <summary>Admin only: promote/demote a user's role.</summary>
        [HttpPatch("{id:int}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleDto dto)
        {
            var user = await _userService.UpdateUserRoleAsync(id, dto.Role);
            return Ok(user);
        }

        /// <summary>Admin: assign any user to any team. Manager: only into/out of their own team.</summary>
        [HttpPatch("{id:int}/team")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignTeam(int id, [FromBody] AssignUserTeamDto dto)
        {
            var user = await _userService.AssignUserToTeamAsync(
                id, dto.TeamId, User.GetUserId(), User.GetRole(), User.GetTeamId());
            return Ok(user);
        }
    }
}
