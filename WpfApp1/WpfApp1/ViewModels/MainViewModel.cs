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
        private string _selectedCategory;
        private DateTime? _newTaskDeadline = DateTime.Today;
        private TaskItem _selectedTask;

        private DispatcherTimer _timer;
        private bool _isTimerRunning;
        private string _currentFilter = "All";

        // Поля статистики
        private int _totalTasksCount;
        private int _completedTasksCount;
        private string _totalTimeSpent = "00:00:00";

        public ObservableCollection<TaskItem> Tasks { get; set; }
        public ObservableCollection<string> Categories { get; set; }
        public ICollectionView TasksView { get; }

        // Властивості для статистики
        public int TotalTasksCount
        {
            get => _totalTasksCount;
            set { _totalTasksCount = value; OnPropertyChanged(); }
        }

        public int CompletedTasksCount
        {
            get => _completedTasksCount;
            set { _completedTasksCount = value; OnPropertyChanged(); }
        }

        public string TotalTimeSpent
        {
            get => _totalTimeSpent;
            set { _totalTimeSpent = value; OnPropertyChanged(); }
        }

        public string NewTaskTitle
        {
            get => _newTaskTitle;
            set { _newTaskTitle = value; OnPropertyChanged(); }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        public DateTime? NewTaskDeadline
        {
            get => _newTaskDeadline;
            set { _newTaskDeadline = value; OnPropertyChanged(); }
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
        public ICommand SortCommand { get; }

        public MainViewModel()
        {
            _dataService = new JsonDataService();
            var loadedTasks = _dataService.LoadTasks();
            Tasks = new ObservableCollection<TaskItem>(loadedTasks);

            Categories = new ObservableCollection<string> { "Загальне", "Навчання", "Робота", "Особисте" };
            SelectedCategory = Categories.First();

            TasksView = CollectionViewSource.GetDefaultView(Tasks);
            TasksView.Filter = FilterTasksCondition;
            TasksView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

            // Сортування за терміновістю
            TasksView.SortDescriptions.Add(new SortDescription("IsCompleted", ListSortDirection.Ascending));
            TasksView.SortDescriptions.Add(new SortDescription("Deadline", ListSortDirection.Ascending));

            Tasks.CollectionChanged += Tasks_CollectionChanged;
            foreach (var task in Tasks) task.PropertyChanged += Task_PropertyChanged;

            UpdateStatistics();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;

            AddTaskCommand = new RelayCommand(AddTask, CanAddTask);
            DeleteTaskCommand = new RelayCommand(DeleteTask, CanDeleteTask);
            StartTimerCommand = new RelayCommand(StartTimer, CanStartTimer);
            StopTimerCommand = new RelayCommand(StopTimer, CanStopTimer);
            FilterCommand = new RelayCommand(ChangeFilter);
            SortCommand = new RelayCommand(ApplySort);
        }

        private void UpdateStatistics()
        {
            TotalTasksCount = Tasks.Count;
            CompletedTasksCount = Tasks.Count(t => t.IsCompleted);

            long totalSeconds = Tasks.Sum(t => t.TimeSpentSeconds);
            TotalTimeSpent = TimeSpan.FromSeconds(totalSeconds).ToString(@"hh\:mm\:ss");
        }

        // Метод сортування
        private void ApplySort(object parameter)
        {
            TasksView.SortDescriptions.Clear();

            if (parameter is string sortType && sortType == "Urgency")
            {

                TasksView.SortDescriptions.Add(new SortDescription("IsCompleted", ListSortDirection.Ascending));
                TasksView.SortDescriptions.Add(new SortDescription("Deadline", ListSortDirection.Ascending));
            }
            else
            {

                TasksView.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
                TasksView.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Ascending));
            }

            TasksView.Refresh();
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
            if (SelectedTask != null)
            {
                SelectedTask.TimeSpentSeconds++;
                UpdateStatistics();
            }
        }

        private bool CanStartTimer(object parameter) => SelectedTask != null && !IsTimerRunning;
        private void StartTimer(object parameter) { IsTimerRunning = true; _timer.Start(); }

        private bool CanStopTimer(object parameter) => IsTimerRunning;
        private void StopTimer(object parameter) { IsTimerRunning = false; _timer.Stop(); SaveData(); }

        private void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null) foreach (TaskItem item in e.NewItems) item.PropertyChanged += Task_PropertyChanged;
            if (e.OldItems != null) foreach (TaskItem item in e.OldItems) item.PropertyChanged -= Task_PropertyChanged;

            UpdateStatistics();
            SaveData();
        }

        private void Task_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskItem.TimeSpentSeconds) && IsTimerRunning)
            {
                return;
            }

            if (e.PropertyName == nameof(TaskItem.IsCompleted) || e.PropertyName == nameof(TaskItem.TimeSpentSeconds))
            {
                UpdateStatistics();
            }

            if (e.PropertyName == nameof(TaskItem.IsCompleted)) TasksView.Refresh();

            SaveData();
        }

        private bool CanAddTask(object parameter) => !string.IsNullOrWhiteSpace(NewTaskTitle);

        private void AddTask(object parameter)
        {
            Tasks.Add(new TaskItem
            {
                Title = NewTaskTitle,
                Category = SelectedCategory,
                Deadline = NewTaskDeadline
            });
            NewTaskTitle = string.Empty;
            NewTaskDeadline = DateTime.Today;
        }

        private bool CanDeleteTask(object parameter) => SelectedTask != null;
        private void DeleteTask(object parameter) { if (SelectedTask != null) Tasks.Remove(SelectedTask); }

        private void SaveData() => _dataService.SaveTasks(Tasks.ToList());

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}