using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class JsonDataService
    {
        private readonly string _filePath;

        public JsonDataService(string fileName = "tasks.json")
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }

        public void SaveTasks(IEnumerable<TaskItem> tasks)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(tasks, options);
            File.WriteAllText(_filePath, json);
        }

        public IEnumerable<TaskItem> LoadTasks()
        {
            if (!File.Exists(_filePath))
            {
                return new List<TaskItem>();
            }

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<IEnumerable<TaskItem>>(json) ?? new List<TaskItem>();
        }
    }
}