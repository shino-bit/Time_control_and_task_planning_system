using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly JsonDataService _dataService;
        private string _newTaskTitle = string.Empty;
        private TaskItem _selectedTask;
        
        private DispatcherTimer _timer;
        private bool _isTimerRunning;
        private string _currentFilter = "All";

        public ObservableCollection<TaskItem> Tasks { get; set; }
        public ICollectionView TasksView { get; } 

        public string NewTaskTitle
        {
            get => _newTaskTitle;
            set { _newTaskTitle = value; OnPropertyChanged(); }
        }

        public TaskItem SelectedTask
        {
            get => _selectedTask;
            set
            {
                if (_isTimerRunning) StopTimer(null);
                _selectedTask = value;
                OnPropertyChanged();
            }
        }

        public bool IsTimerRunning
        {
            get => _isTimerRunning;
            set { _isTimerRunning = value; OnPropertyChanged(); }
        }

        public ICommand AddTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand StartTimerCommand { get; }
        public ICommand StopTimerCommand { get; }
        public ICommand FilterCommand { get; }

        public MainViewModel()
        {
            _dataService = new JsonDataService();
            var loadedTasks = _dataService.LoadTasks();
            Tasks = new ObservableCollection<TaskItem>(loadedTasks);

            TasksView = CollectionViewSource.GetDefaultView(Tasks);
            TasksView.Filter = FilterTasksCondition;

            Tasks.CollectionChanged += Tasks_CollectionChanged;
            foreach (var task in Tasks) task.PropertyChanged += Task_PropertyChanged;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;

            AddTaskCommand = new RelayCommand(AddTask, CanAddTask);
            DeleteTaskCommand = new RelayCommand(DeleteTask, CanDeleteTask);
            StartTimerCommand = new RelayCommand(StartTimer, CanStartTimer);
            StopTimerCommand = new RelayCommand(StopTimer, CanStopTimer);
            FilterCommand = new RelayCommand(ChangeFilter);
        }

        private bool FilterTasksCondition(object obj)
        {
            if (obj is TaskItem task)
            {
                return _currentFilter switch
                {
                    "Active" => !task.IsCompleted,
                    "Completed" => task.IsCompleted,
                    _ => true
                };
            }
            return false;
        }

        private void ChangeFilter(object parameter)
        {
            if (parameter is string filter)
            {
                _currentFilter = filter;
                TasksView.Refresh(); 
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (SelectedTask != null) SelectedTask.TimeSpentSeconds++;
        }

        private bool CanStartTimer(object parameter) => SelectedTask != null && !IsTimerRunning;
        private void StartTimer(object parameter) { IsTimerRunning = true; _timer.Start(); }

        private bool CanStopTimer(object parameter) => IsTimerRunning;
        private void StopTimer(object parameter) { IsTimerRunning = false; _timer.Stop(); SaveData(); }

        private void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null) foreach (TaskItem item in e.NewItems) item.PropertyChanged += Task_PropertyChanged;
            if (e.OldItems != null) foreach (TaskItem item in e.OldItems) item.PropertyChanged -= Task_PropertyChanged;
        }

        private void Task_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskItem.TimeSpentSeconds) && IsTimerRunning) return;

            if (e.PropertyName == nameof(TaskItem.IsCompleted)) TasksView.Refresh();
            
            SaveData();
        }

        private bool CanAddTask(object parameter) => !string.IsNullOrWhiteSpace(NewTaskTitle);
        private void AddTask(object parameter)
        {
            Tasks.Add(new TaskItem { Title = NewTaskTitle });
            NewTaskTitle = string.Empty; 
        }

        private bool CanDeleteTask(object parameter) => SelectedTask != null;
        private void DeleteTask(object parameter) { if (SelectedTask != null) Tasks.Remove(SelectedTask); }

        private void SaveData() => _dataService.SaveTasks(Tasks.ToList());

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}