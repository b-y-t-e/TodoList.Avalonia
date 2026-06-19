using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TodoList.Avalonia.Model;

namespace TodoList.Avalonia.Demo;

public class TodoViewModel : INotifyPropertyChanged
{
    public ObservableCollection<TodoItemData> Items { get; } = new();

    public ICommand AddItemCommand { get; }
    public ICommand CheckAllCommand { get; }
    public ICommand UncheckAllCommand { get; }
    public ICommand RemoveCheckedCommand { get; }

    private string _statusText = "MVVM Demo — edit the list, changes sync with ViewModel.";
    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
    }

    private int _itemCount;
    public int ItemCount
    {
        get => _itemCount;
        private set
        {
            if (_itemCount != value)
            {
                _itemCount = value;
                OnPropertyChanged();
            }
        }
    }

    private int _checkedCount;
    public int CheckedCount
    {
        get => _checkedCount;
        private set
        {
            if (_checkedCount != value)
            {
                _checkedCount = value;
                OnPropertyChanged();
            }
        }
    }

    public TodoViewModel()
    {
        AddItemCommand = new RelayCommand(() =>
        {
            Items.Add(new TodoItemData($"New item #{Items.Count + 1}"));
            UpdateCounts();
        });

        CheckAllCommand = new RelayCommand(() =>
        {
            foreach (var item in Items) item.IsChecked = true;
            UpdateCounts();
        });

        UncheckAllCommand = new RelayCommand(() =>
        {
            foreach (var item in Items) item.IsChecked = false;
            UpdateCounts();
        });

        RemoveCheckedCommand = new RelayCommand(() =>
        {
            for (int i = Items.Count - 1; i >= 0; i--)
                if (Items[i].IsChecked) Items.RemoveAt(i);
            UpdateCounts();
        });

        Items.Add(new TodoItemData("Buy milk"));
        Items.Add(new TodoItemData("Walk the dog", true));
        Items.Add(new TodoItemData("Code review PR #42"));
        Items.Add(new TodoItemData("MVVM binding example"));
        Items.Add(new TodoItemData("Images in text: ![star](star) great ![ok](check) done"));
        Items.Add(new TodoItemData("Paste text here (Ctrl+V)"));

        Items.CollectionChanged += OnCollectionChanged;
        foreach (var item in Items)
            item.PropertyChanged += OnItemPropertyChanged;

        UpdateCounts();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateCounts();

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (TodoItemData item in e.OldItems)
                item.PropertyChanged -= OnItemPropertyChanged;
        if (e.NewItems != null)
            foreach (TodoItemData item in e.NewItems)
                item.PropertyChanged += OnItemPropertyChanged;
        UpdateCounts();
    }

    private void UpdateCounts()
    {
        ItemCount = Items.Count;
        int c = 0;
        foreach (var item in Items)
            if (item.IsChecked) c++;
        CheckedCount = c;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    public RelayCommand(Action execute) => _execute = execute;
#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}
