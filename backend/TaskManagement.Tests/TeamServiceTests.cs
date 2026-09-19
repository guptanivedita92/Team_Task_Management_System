using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Models;
using TaskManagement.Api.Services;
using TaskManagement.Tests.TestHelpers;

namespace TaskManagement.Tests
{
    public class TeamServiceTests
    {
        private static (TeamService svc, AppDbContext db) CreateService()
        {
            var db = TestDbContextFactory.Create();
            return (new TeamService(db), db);
        }

        [Fact]
        public async Task CreateTeam_DuplicateName_Throws409()
        {
            var (svc, _) = CreateService();
            await svc.CreateTeamAsync(new CreateTeamDto { Name = "Engineering" });

            var ex = await Assert.ThrowsAsync<ApiException>(() =>
                svc.CreateTeamAsync(new CreateTeamDto { Name = "Engineering" }));

            Assert.Equal(StatusCodes.Status409Conflict, ex.StatusCode);
        }

        [Fact]
        public async Task AddMember_ManagerOfDifferentTeam_Throws403()
        {
            var (svc, db) = CreateService();
            var teamA = await svc.CreateTeamAsync(new CreateTeamDto { Name = "Team A" });
            var teamB = await svc.CreateTeamAsync(new CreateTeamDto { Name = "Team B" });
            var user = new User { FullName = "U", Email = "u@test.com", Role = UserRole.User, PasswordHash = "x" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // A manager whose own team is B trying to add a member into team A.
            var ex = await Assert.ThrowsAsync<ApiException>(() =>
                svc.AddMemberAsync(teamA.Id, user.Id, UserRole.Manager, teamB.Id));

            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task AddMember_ManagerOfOwnTeam_Succeeds()
        {
            var (svc, db) = CreateService();
            var team = await svc.CreateTeamAsync(new CreateTeamDto { Name = "Team A" });
            var user = new User { FullName = "U", Email = "u2@test.com", Role = UserRole.User, PasswordHash = "x" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var result = await svc.AddMemberAsync(team.Id, user.Id, UserRole.Manager, team.Id);

            Assert.Contains(result.Members, m => m.Id == user.Id);
        }

        [Fact]
        public async Task DeleteTeam_WithExistingTasks_Throws409()
        {
            var (svc, db) = CreateService();
            var team = await svc.CreateTeamAsync(new CreateTeamDto { Name = "Has Tasks" });
            var creator = new User { FullName = "C", Email = "c@test.com", Role = UserRole.Manager, PasswordHash = "x", TeamId = team.Id };
            db.Users.Add(creator);
            await db.SaveChangesAsync();
            db.Tasks.Add(new TaskItem
            {
                Title = "Blocking task",
                CreatedById = creator.Id,
                TeamId = team.Id
            });
            await db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<ApiException>(() => svc.DeleteTeamAsync(team.Id));

            Assert.Equal(StatusCodes.Status409Conflict, ex.StatusCode);
        }
    }
}
