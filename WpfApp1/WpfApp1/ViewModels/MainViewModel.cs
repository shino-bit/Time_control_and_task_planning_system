using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
	public class MainViewModel : INotifyPropertyChanged
	{
		private readonly JsonDataService _dataService;
		private string _newTaskTitle = string.Empty;
		private TaskItem _selectedTask;

		public ObservableCollection<TaskItem> Tasks { get; set; }

		public string NewTaskTitle
		{
			get => _newTaskTitle;
			set
			{
				_newTaskTitle = value;
				OnPropertyChanged();
			}
		}

		public TaskItem SelectedTask
		{
			get => _selectedTask;
			set
			{
				_selectedTask = value;
				OnPropertyChanged();
			}
		}

		public ICommand AddTaskCommand { get; }
		public ICommand DeleteTaskCommand { get; }

		public MainViewModel()
		{
			_dataService = new JsonDataService();

			var loadedTasks = _dataService.LoadTasks();
			Tasks = new ObservableCollection<TaskItem>(loadedTasks);

			Tasks.CollectionChanged += Tasks_CollectionChanged;

			foreach (var task in Tasks)
			{
				task.PropertyChanged += Task_PropertyChanged;
			}

			AddTaskCommand = new RelayCommand(AddTask, CanAddTask);
			DeleteTaskCommand = new RelayCommand(DeleteTask, CanDeleteTask);
		}

		private void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
			{
				foreach (TaskItem item in e.NewItems)
				{
					item.PropertyChanged += Task_PropertyChanged;
				}
			}

			if (e.OldItems != null)
			{
				foreach (TaskItem item in e.OldItems)
				{
					item.PropertyChanged -= Task_PropertyChanged;
				}
			}
		}

		private void Task_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			// Зберігаємо зміни при редагуванні 
			SaveData();
		}

		private bool CanAddTask(object parameter) => !string.IsNullOrWhiteSpace(NewTaskTitle);

		private void AddTask(object parameter)
		{
			var newTask = new TaskItem { Title = NewTaskTitle };
			Tasks.Add(newTask);
			NewTaskTitle = string.Empty;
		}

		private bool CanDeleteTask(object parameter) => SelectedTask != null;

		private void DeleteTask(object parameter)
		{
			if (SelectedTask != null)
			{
				Tasks.Remove(SelectedTask);
			}
		}

		private void SaveData()
		{
			_dataService.SaveTasks(Tasks.ToList());
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}