using TaskManagement.Api.DTOs;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Interfaces
{
    public interface INotificationService
    {
        /// <summary>Creates an in-app notification for a user. Used internally by TaskService.</summary>
        Task NotifyAsync(int userId, string message, NotificationType type);

        Task<List<NotificationResponseDto>> GetMyNotificationsAsync(int userId);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
    }
}
