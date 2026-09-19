namespace TaskManagement.Api.Models
{
    public enum UserRole
    {
        Admin = 0,
        Manager = 1,
        User = 2
    }

    public enum TaskPriority
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum TaskStatus
    {
        ToDo = 0,
        InProgress = 1,
        Done = 2
    }

    public enum NotificationType
    {
        TaskAssigned = 0,
        StatusUpdated = 1
    }
}
