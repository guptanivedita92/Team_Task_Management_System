using TaskManagement.Api.Data;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Models;
using TaskManagement.Api.Services;
using TaskManagement.Tests.TestHelpers;

namespace TaskManagement.Tests
{
    public class UserServiceTests
    {
        private class Fixture
        {
            public AppDbContext Db { get; }
            public UserService UserService { get; }
            public Team TeamA { get; }
            public Team TeamB { get; }
            public User ManagerA { get; }
            public User UserA1 { get; }
            public User UserB1 { get; }

            public Fixture()
            {
                Db = TestDbContextFactory.Create();
                UserService = new UserService(Db);

                TeamA = new Team { Name = "Team A" };
                TeamB = new Team { Name = "Team B" };
                Db.Teams.AddRange(TeamA, TeamB);
                Db.SaveChanges();

                ManagerA = new User { FullName = "Mgr A", Email = "mgra@test.com", Role = UserRole.Manager, TeamId = TeamA.Id, PasswordHash = "x" };
                UserA1 = new User { FullName = "A1", Email = "a1@test.com", Role = UserRole.User, TeamId = TeamA.Id, PasswordHash = "x" };
                UserB1 = new User { FullName = "B1", Email = "b1@test.com", Role = UserRole.User, TeamId = TeamB.Id, PasswordHash = "x" };
                Db.Users.AddRange(ManagerA, UserA1, UserB1);
                Db.SaveChanges();
            }
        }

        [Fact]
        public async Task GetUsers_AsManager_OnlyReturnsOwnTeam()
        {
            var f = new Fixture();

            var result = await f.UserService.GetUsersAsync(f.ManagerA.Id, UserRole.Manager);

            Assert.Contains(result, u => u.Id == f.ManagerA.Id);
            Assert.Contains(result, u => u.Id == f.UserA1.Id);
            Assert.DoesNotContain(result, u => u.Id == f.UserB1.Id);
        }

        [Fact]
        public async Task GetUsers_AsPlainUser_OnlyReturnsSelf()
        {
            var f = new Fixture();

            var result = await f.UserService.GetUsersAsync(f.UserA1.Id, UserRole.User);

            Assert.Single(result);
            Assert.Equal(f.UserA1.Id, result[0].Id);
        }

        [Fact]
        public async Task AssignUserToTeam_ManagerAssigningToOtherTeam_Throws403()
        {
            var f = new Fixture();

            var ex = await Assert.ThrowsAsync<ApiException>(() => f.UserService.AssignUserToTeamAsync(
                f.UserB1.Id, f.TeamA.Id, f.ManagerA.Id, UserRole.Manager, f.TeamA.Id));

            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task GetUserById_ManagerViewingOtherTeamMember_Throws403()
        {
            var f = new Fixture();

            var ex = await Assert.ThrowsAsync<ApiException>(() => f.UserService.GetUserByIdAsync(
                f.UserB1.Id, f.ManagerA.Id, UserRole.Manager, f.TeamA.Id));

            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
        }
    }
}
