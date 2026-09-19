using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Helpers;
using TaskManagement.Api.Interfaces;

namespace TaskManagement.Api.Controllers
{
    [ApiController]
    [Route("api/teams")]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        /// <summary>Admin: all teams. Manager/User: only their own team.</summary>
        [HttpGet]
        public async Task<IActionResult> GetTeams()
        {
            var teams = await _teamService.GetTeamsAsync(User.GetUserId(), User.GetRole(), User.GetTeamId());
            return Ok(teams);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTeam(int id)
        {
            var team = await _teamService.GetTeamByIdAsync(id, User.GetRole(), User.GetTeamId());
            return Ok(team);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto)
        {
            var team = await _teamService.CreateTeamAsync(dto);
            return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTeam(int id, [FromBody] UpdateTeamDto dto)
        {
            var team = await _teamService.UpdateTeamAsync(id, dto);
            return Ok(team);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            await _teamService.DeleteTeamAsync(id);
            return NoContent();
        }

        /// <summary>Admin: any team. Manager: only their own team.</summary>
        [HttpPost("{id:int}/members")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddMember(int id, [FromBody] AddTeamMemberDto dto)
        {
            var team = await _teamService.AddMemberAsync(id, dto.UserId, User.GetRole(), User.GetTeamId());
            return Ok(team);
        }

        [HttpDelete("{id:int}/members/{userId:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> RemoveMember(int id, int userId)
        {
            var team = await _teamService.RemoveMemberAsync(id, userId, User.GetRole(), User.GetTeamId());
            return Ok(team);
        }
    }
}
