using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfApp1.Models
{
    public class TaskItem : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _category = "Загальне";
        private bool _isCompleted = false;
        private int _timeSpentSeconds = 0;
        private DateTime? _deadline; 

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOverdue)); 
            }
        }

        public DateTime? Deadline
        {
            get => _deadline;
            set
            {
                _deadline = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOverdue));
            }
        }

        public bool IsOverdue => Deadline.HasValue && Deadline.Value.Date < DateTime.Now.Date && !IsCompleted;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int TimeSpentSeconds
        {
            get => _timeSpentSeconds;
            set
            {
                _timeSpentSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedTime));
            }
        }

        public string FormattedTime => TimeSpan.FromSeconds(TimeSpentSeconds).ToString(@"hh\:mm\:ss");

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}