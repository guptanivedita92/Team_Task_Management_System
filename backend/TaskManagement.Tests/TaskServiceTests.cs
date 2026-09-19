using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Models;
using TaskManagement.Api.Services;
using TaskManagement.Tests.TestHelpers;
using AppTaskStatus = TaskManagement.Api.Models.TaskStatus;

namespace TaskManagement.Tests
{
    public class TaskServiceTests
    {
        // ---------- fixture ----------

        private class Fixture
        {
            public AppDbContext Db { get; }
            public TaskService TaskService { get; }
            public Team TeamA { get; }
            public Team TeamB { get; }
            public User Admin { get; }
            public User ManagerA { get; }
            public User ManagerB { get; }
            public User UserA1 { get; } // member of TeamA
            public User UserA2 { get; } // member of TeamA
            public User UserB1 { get; } // member of TeamB

            public Fixture()
            {
                Db = TestDbContextFactory.Create();
                INotificationService notifications = new NotificationService(Db);
                TaskService = new TaskService(Db, notifications);

                TeamA = new Team { Name = "Team A" };
                TeamB = new Team { Name = "Team B" };
                Db.Teams.AddRange(TeamA, TeamB);
                Db.SaveChanges();

                Admin = new User { FullName = "Admin", Email = "admin@test.com", Role = UserRole.Admin, PasswordHash = "x" };
                ManagerA = new User { FullName = "Manager A", Email = "mgra@test.com", Role = UserRole.Manager, TeamId = TeamA.Id, PasswordHash = "x" };
                ManagerB = new User { FullName = "Manager B", Email = "mgrb@test.com", Role = UserRole.Manager, TeamId = TeamB.Id, PasswordHash = "x" };
                UserA1 = new User { FullName = "User A1", Email = "a1@test.com", Role = UserRole.User, TeamId = TeamA.Id, PasswordHash = "x" };
                UserA2 = new User { FullName = "User A2", Email = "a2@test.com", Role = UserRole.User, TeamId = TeamA.Id, PasswordHash = "x" };
                UserB1 = new User { FullName = "User B1", Email = "b1@test.com", Role = UserRole.User, TeamId = TeamB.Id, PasswordHash = "x" };

                Db.Users.AddRange(Admin, ManagerA, ManagerB, UserA1, UserA2, UserB1);
                Db.SaveChanges();
            }
        }

        // ---------- create ----------

        [Fact]
        public async Task CreateTask_ManagerForOwnTeam_Succeeds()
        {
            var f = new Fixture();

            var result = await f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "Task 1", TeamId = f.TeamA.Id, AssignedUserId = f.UserA1.Id },
                f.ManagerA.Id, UserRole.Manager, f.TeamA.Id);

            Assert.Equal("Task 1", result.Title);
            Assert.Equal(AppTaskStatus.ToDo, result.Status);
        }

        [Fact]
        public async Task CreateTask_ManagerForOtherTeam_Throws403()
        {
            var f = new Fixture();

            var ex = await Assert.ThrowsAsync<ApiException>(() => f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "Cross-team", TeamId = f.TeamB.Id },
                f.ManagerA.Id, UserRole.Manager, f.TeamA.Id));

            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task CreateTask_AsPlainUser_Throws403()
        {
            var f = new Fixture();

            var ex = await Assert.ThrowsAsync<ApiException>(() => f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "Nope", TeamId = f.TeamA.Id },
                f.UserA1.Id, UserRole.User, f.TeamA.Id));

            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task CreateTask_AssigneeNotInTeam_Throws400()
        {
            var f = new Fixture();

            var ex = await Assert.ThrowsAsync<ApiException>(() => f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "Bad assign", TeamId = f.TeamA.Id, AssignedUserId = f.UserB1.Id },
                f.Admin.Id, UserRole.Admin, null));

            Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
        }

        [Fact]
        public async Task CreateTask_WithAssignee_SendsAssignmentNotification()
        {
            var f = new Fixture();

            await f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "Notify me", TeamId = f.TeamA.Id, AssignedUserId = f.UserA1.Id },
                f.ManagerA.Id, UserRole.Manager, f.TeamA.Id);

            var notification = f.Db.Notifications.Single(n => n.UserId == f.UserA1.Id);
            Assert.Equal(NotificationType.TaskAssigned, notification.Type);
            Assert.False(notification.IsRead);
        }

        // ---------- IDOR: viewing a task by id ----------

        [Fact]
        public async Task GetTaskById_UserNotAssigned_Throws403_EvenIfTeammateIsAssigned()
        {
            var f = new Fixture();
            var task = await f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "A1's task", TeamId = f.TeamA.Id, AssignedUserId = f.UserA1.Id },
                f.ManagerA.Id, UserRole.Manager, f.TeamA.Id);

            // UserA2 is on the SAME team but the task is not assigned to them - this is exactly
            // the "change the id in the URL" IDOR case the assignment calls out.
            var ex = await Assert.ThrowsAsync<ApiException>(() => f.TaskService.GetTaskByIdAsync(
                task.Id, f.UserA2.Id, UserRole.User, f.TeamA.Id));

            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task GetTaskById_AssignedUser_Succeeds()
        {
            var f = new Fixture();
            var task = await f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "A1's task", TeamId = f.TeamA.Id, AssignedUserId = f.UserA1.Id },
                f.ManagerA.Id, UserRole.Manager, f.TeamA.Id);

            var result = await f.TaskService.GetTaskByIdAsync(task.Id, f.UserA1.Id, UserRole.User, f.TeamA.Id);

            Assert.Equal(task.Id, result.Id);
        }

        [Fact]
        public async Task GetTaskById_ManagerFromOtherTeam_Throws403()
        {
            var f = new Fixture();
            var task = await f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "Team A only", TeamId = f.TeamA.Id },
                f.ManagerA.Id, UserRole.Manager, f.TeamA.Id);

            var ex = await Assert.ThrowsAsync<ApiException>(() => f.TaskService.GetTaskByIdAsync(
                task.Id, f.ManagerB.Id, UserRole.Manager, f.TeamB.Id));

            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task GetTaskById_Admin_CanViewAnyTask()
        {
            var f = new Fixture();
            var task = await f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "Team B task", TeamId = f.TeamB.Id },
                f.Admin.Id, UserRole.Admin, null);

            var result = await f.TaskService.GetTaskByIdAsync(task.Id, f.Admin.Id, UserRole.Admin, null);

            Assert.Equal(task.Id, result.Id);
        }

        // ---------- status updates ----------

        [Fact]
        public async Task UpdateStatus_AssignedUser_CanUpdateOwnTask()
        {
            var f = new Fixture();
            var task = await f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "Mine", TeamId = f.TeamA.Id, AssignedUserId = f.UserA1.Id },
                f.ManagerA.Id, UserRole.Manager, f.TeamA.Id);

            var result = await f.TaskService.UpdateTaskStatusAsync(
                task.Id, AppTaskStatus.InProgress, f.UserA1.Id, UserRole.User, f.TeamA.Id);

            Assert.Equal(AppTaskStatus.InProgress, result.Status);
        }

        [Fact]
        public async Task UpdateStatus_UserNotAssigned_Throws403()
        {
            var f = new Fixture();
            var task = await f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "Not yours", TeamId = f.TeamA.Id, AssignedUserId = f.UserA1.Id },
                f.ManagerA.Id, UserRole.Manager, f.TeamA.Id);

            var ex = await Assert.ThrowsAsync<ApiException>(() => f.TaskService.UpdateTaskStatusAsync(
                task.Id, AppTaskStatus.Done, f.UserA2.Id, UserRole.User, f.TeamA.Id));

            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_ByManager_NotifiesAssignedUser()
        {
            var f = new Fixture();
            var task = await f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "Status test", TeamId = f.TeamA.Id, AssignedUserId = f.UserA1.Id },
                f.ManagerA.Id, UserRole.Manager, f.TeamA.Id);

            await f.TaskService.UpdateTaskStatusAsync(task.Id, AppTaskStatus.InProgress, f.ManagerA.Id, UserRole.Manager, f.TeamA.Id);

            var notification = f.Db.Notifications.Single(n => n.UserId == f.UserA1.Id && n.Type == NotificationType.StatusUpdated);
            Assert.Contains("InProgress", notification.Message);
        }

        // ---------- delete ----------

        [Fact]
        public async Task DeleteTask_AsUser_Throws403()
        {
            var f = new Fixture();
            var task = await f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "Cannot delete", TeamId = f.TeamA.Id },
                f.ManagerA.Id, UserRole.Manager, f.TeamA.Id);

            var ex = await Assert.ThrowsAsync<ApiException>(() =>
                f.TaskService.DeleteTaskAsync(task.Id, UserRole.User, f.TeamA.Id));

            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
        }

        [Fact]
        public async Task DeleteTask_ManagerOfOtherTeam_Throws403()
        {
            var f = new Fixture();
            var task = await f.TaskService.CreateTaskAsync(
                new CreateTaskDto { Title = "Team A task", TeamId = f.TeamA.Id },
                f.ManagerA.Id, UserRole.Manager, f.TeamA.Id);

            var ex = await Assert.ThrowsAsync<ApiException>(() =>
                f.TaskService.DeleteTaskAsync(task.Id, UserRole.Manager, f.TeamB.Id));

            Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
        }
    }
}
