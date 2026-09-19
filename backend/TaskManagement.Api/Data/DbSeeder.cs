using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Data
{
    // Development/demo seed data ONLY. Credentials here are intentionally simple and are
    // documented as such in the README - never use these values, or this seeding approach,
    // for a real production deployment.
    public static class DbSeeder
    {
        public const string SeedPassword = "Passw0rd!123";

        public static async Task SeedAsync(AppDbContext db, IPasswordHasher<User> hasher)
        {
            if (await db.Users.AnyAsync())
            {
                return; // already seeded
            }

            var team = new Team { Name = "Engineering" };
            db.Teams.Add(team);
            await db.SaveChangesAsync();

            var admin = new User
            {
                FullName = "Alice Admin",
                Email = "admin@taskmanagement.dev",
                Role = UserRole.Admin
            };
            admin.PasswordHash = hasher.HashPassword(admin, SeedPassword);

            var manager = new User
            {
                FullName = "Mo Manager",
                Email = "manager@taskmanagement.dev",
                Role = UserRole.Manager,
                TeamId = team.Id
            };
            manager.PasswordHash = hasher.HashPassword(manager, SeedPassword);

            var user = new User
            {
                FullName = "Uma User",
                Email = "user@taskmanagement.dev",
                Role = UserRole.User,
                TeamId = team.Id
            };
            user.PasswordHash = hasher.HashPassword(user, SeedPassword);

            db.Users.AddRange(admin, manager, user);
            await db.SaveChangesAsync();

            team.ManagerId = manager.Id;
            await db.SaveChangesAsync();

            var task = new TaskItem
            {
                Title = "Set up project repository",
                Description = "Initialize the repo, branch protection, and CI pipeline.",
                AssignedUserId = user.Id,
                CreatedById = manager.Id,
                TeamId = team.Id,
                Priority = TaskPriority.High,
                Status = Models.TaskStatus.ToDo,
                Deadline = DateTime.UtcNow.AddDays(7)
            };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
        }
    }
}
