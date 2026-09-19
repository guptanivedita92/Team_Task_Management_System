using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Models;
using TaskManagement.Api.Services;
using TaskManagement.Tests.TestHelpers;

namespace TaskManagement.Tests
{
    public class CommentServiceTests
    {
        private class Fixture
        {
            public AppDbContext Db { get; }
            public CommentService CommentService { get; }
            public Team Team { get; }
            public User Manager { get; }
            public User AssignedUser { get; }
            public User OtherUser { get; }
            public TaskItem Task { get; }

            public Fixture()
            {
                Db = TestDbContextFactory.Create();
                CommentService = new CommentService(Db);

                Team = new Team { Name = "Team A" };
                Db.Teams.Add(Team);
                Db.SaveChanges();

                Manager = new User { FullName = "Mgr", Email = "mgr@test.com", Role = UserRole.Manager, TeamId = Team.Id, PasswordHash = "x" };
                AssignedUser = new User { FullName = "Assignee", Email = "assignee@test.com", Role = UserRole.User, TeamId = Team.Id, PasswordHash = "x" };
                OtherUser = new User { FullName = "Other", Email = "other@test.com", Role = UserRole.User, TeamId = Team.Id, PasswordHash = "x" };
                Db.Users.AddRange(Manager, AssignedUser, OtherUser);
                Db.SaveChanges();

                Task = new TaskItem
                {
                    Title = "Task",
                    CreatedById = Manager.Id,
                    TeamId = Team.Id,
                    AssignedUserId = AssignedUser.Id
                };
                Db.Tasks.Add(Task);
                Db.SaveChanges();
            }
        }

        [Fact]
        public async Task AddComment_AssignedUser_Succeeds()
        {
            var f = new Fixture();

            var comment = await f.CommentService.AddCommentAsync(
                f.Task.Id, new CreateCommentDto { CommentText = "Looks good" },
                f.AssignedUser.Id, UserRole.User, f.Team.Id);

            Assert.Equal("Looks good", comment.CommentText);
        }

        [Fact]
        public async Task AddComment_UnrelatedTeammate_Throws403()
        {
            var f = new Fixture();

            var ex = await Assert.ThrowsAsync<ApiException>(() => f.CommentService.AddCommentAsync(
                f.Task.Id, new CreateCommentDto { CommentText = "Nosy comment" },
                f.OtherUser.Id, UserRole.User, f.Team.Id));

            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task GetComments_Manager_CanViewTeamTaskComments()
        {
            var f = new Fixture();
            await f.CommentService.AddCommentAsync(
                f.Task.Id, new CreateCommentDto { CommentText = "Update" },
                f.AssignedUser.Id, UserRole.User, f.Team.Id);

            var comments = await f.CommentService.GetCommentsAsync(f.Task.Id, f.Manager.Id, UserRole.Manager, f.Team.Id);

            Assert.Single(comments);
        }
    }
}
