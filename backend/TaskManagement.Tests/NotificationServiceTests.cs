using TaskManagement.Api.Data;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Models;
using TaskManagement.Api.Services;
using TaskManagement.Tests.TestHelpers;

namespace TaskManagement.Tests
{
    public class NotificationServiceTests
    {
        private static (NotificationService svc, AppDbContext db) CreateService()
        {
            var db = TestDbContextFactory.Create();
            return (new NotificationService(db), db);
        }

        [Fact]
        public async Task MarkAsRead_OwnNotification_Succeeds()
        {
            var (svc, db) = CreateService();
            var user = new User { FullName = "U", Email = "u@test.com", Role = UserRole.User, PasswordHash = "x" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            await svc.NotifyAsync(user.Id, "Hello", NotificationType.TaskAssigned);
            var notification = db.Notifications.Single();

            await svc.MarkAsReadAsync(notification.Id, user.Id);

            Assert.True(db.Notifications.Single().IsRead);
        }

        [Fact]
        public async Task MarkAsRead_OtherUsersNotification_Throws404()
        {
            var (svc, db) = CreateService();
            var owner = new User { FullName = "Owner", Email = "owner@test.com", Role = UserRole.User, PasswordHash = "x" };
            var intruder = new User { FullName = "Intruder", Email = "intruder@test.com", Role = UserRole.User, PasswordHash = "x" };
            db.Users.AddRange(owner, intruder);
            await db.SaveChangesAsync();

            await svc.NotifyAsync(owner.Id, "Private", NotificationType.StatusUpdated);
            var notification = db.Notifications.Single();

            // Someone else's notification id must not be readable/markable by another user.
            var ex = await Assert.ThrowsAsync<ApiException>(() => svc.MarkAsReadAsync(notification.Id, intruder.Id));

            Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
        }

        [Fact]
        public async Task MarkAllAsRead_OnlyAffectsCallingUsersNotifications()
        {
            var (svc, db) = CreateService();
            var userA = new User { FullName = "A", Email = "a@test.com", Role = UserRole.User, PasswordHash = "x" };
            var userB = new User { FullName = "B", Email = "b@test.com", Role = UserRole.User, PasswordHash = "x" };
            db.Users.AddRange(userA, userB);
            await db.SaveChangesAsync();

            await svc.NotifyAsync(userA.Id, "For A", NotificationType.TaskAssigned);
            await svc.NotifyAsync(userB.Id, "For B", NotificationType.TaskAssigned);

            await svc.MarkAllAsReadAsync(userA.Id);

            Assert.True(db.Notifications.Single(n => n.UserId == userA.Id).IsRead);
            Assert.False(db.Notifications.Single(n => n.UserId == userB.Id).IsRead);
        }
    }
}
