using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Helpers;
using TaskManagement.Api.Interfaces;

namespace TaskManagement.Api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>Overview scoped to the caller: Admin sees org-wide, Manager their team, User their own tasks.</summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery] TaskFilterDto filter)
        {
            var dashboard = await _dashboardService.GetDashboardAsync(filter, User.GetUserId(), User.GetRole(), User.GetTeamId());
            return Ok(dashboard);
        }
    }
}
