using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<TaskItem> Tasks => Set<TaskItem>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- User ----------
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();

                entity.HasOne(u => u.Team)
                    .WithMany(t => t.Members)
                    .HasForeignKey(u => u.TeamId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ---------- Team ----------
            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasIndex(t => t.Name).IsUnique();

                entity.HasOne(t => t.Manager)
                    .WithMany()
                    .HasForeignKey(t => t.ManagerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ---------- TaskItem ----------
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasIndex(t => t.AssignedUserId);
                entity.HasIndex(t => t.TeamId);
                entity.HasIndex(t => t.Status);

                entity.HasOne(t => t.AssignedUser)
                    .WithMany(u => u.AssignedTasks)
                    .HasForeignKey(t => t.AssignedUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(t => t.CreatedBy)
                    .WithMany(u => u.CreatedTasks)
                    .HasForeignKey(t => t.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Team)
                    .WithMany(team => team.Tasks)
                    .HasForeignKey(t => t.TeamId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
                entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            });

            // ---------- Comment ----------
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasIndex(c => c.TaskId);

                entity.HasOne(c => c.Task)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(c => c.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.User)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- Notification ----------
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasIndex(n => n.UserId);

                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
            });

            // ---------- Enum on User ----------
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>()
                .HasMaxLength(20);
        }
    }
}
