using System;

namespace TimeTrackerApp.Models
{
    public class TaskItem
    {
        // ідентифікатор задачі
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // назва задачі
        public string Title { get; set; } = string.Empty;

        // детальний опис
        public string Description { get; set; } = string.Empty;

        // категорія
        public string Category { get; set; } = "Загальне";

        // статус виконання
        public bool IsCompleted { get; set; } = false;

        // дата створення
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // витрачений час 
        public int TimeSpentSeconds { get; set; } = 0;
    }
}