using TaskManagement.Api.Models;

namespace TaskManagement.Api.DTOs
{
    public class TasksByUserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TasksByPriorityDto
    {
        public TaskPriority Priority { get; set; }
        public int Count { get; set; }
    }

    public class DashboardResponseDto
    {
        public int TotalTasks { get; set; }
        public int ToDoCount { get; set; }
        public int InProgressCount { get; set; }
        public int DoneCount { get; set; }
        public List<TasksByUserDto> TasksByUser { get; set; } = new();
        public List<TasksByPriorityDto> TasksByPriority { get; set; } = new();
        public List<TaskResponseDto> UpcomingDeadlines { get; set; } = new();
    }
}
