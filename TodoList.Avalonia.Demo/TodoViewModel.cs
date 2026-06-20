using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using global::Avalonia;
using global::Avalonia.Media;
using global::Avalonia.Media.Imaging;
using global::Avalonia.Platform;
using global::Avalonia.Platform.Storage;
using TodoList.Avalonia.Model;

namespace TodoList.Avalonia.Demo;

public class TodoViewModel : INotifyPropertyChanged
{
    public ObservableCollection<TodoItemData> Items { get; } = new();
    public ObservableCollection<TodoImageEntry> Images { get; } = new();

    public ICommand AddItemCommand { get; }
    public ICommand CheckAllCommand { get; }
    public ICommand UncheckAllCommand { get; }
    public ICommand RemoveCheckedCommand { get; }

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set { if (_statusText != value) { _statusText = value; OnPropertyChanged(); } }
    }

    public TodoViewModel()
    {
        AddItemCommand = new RelayCommand(() =>
            Items.Add(new TodoItemData($"New item #{Items.Count + 1}")));

        CheckAllCommand = new RelayCommand(() =>
        {
            foreach (var item in Items) item.IsChecked = true;
        });

        UncheckAllCommand = new RelayCommand(() =>
        {
            foreach (var item in Items) item.IsChecked = false;
        });

        RemoveCheckedCommand = new RelayCommand(() =>
        {
            for (int i = Items.Count - 1; i >= 0; i--)
                if (Items[i].IsChecked) Items.RemoveAt(i);
        });

        Items.CollectionChanged += OnItemsCollectionChanged;

        LoadSampleData();
        UpdateStatus();
    }

    private void LoadSampleData()
    {
        var dogBitmap = new Bitmap(
            global::Avalonia.Platform.AssetLoader.Open(
                new Uri("avares://TodoList.Avalonia.Demo/Assets/dog.png")));

        Images.Add(new TodoImageEntry("dog", dogBitmap));
        Images.Add(new TodoImageEntry("star", CreateSolidBitmap(Colors.Gold)));
        Images.Add(new TodoImageEntry("check", CreateSolidBitmap(Colors.LimeGreen)));

        Items.Add(new TodoItemData("Buy milk"));
        Items.Add(new TodoItemData("Morning walk in the park ![dog](dog)", true));
        Items.Add(new TodoItemData("Code review PR #42"));
        Items.Add(new TodoItemData("MVVM binding example"));
        Items.Add(new TodoItemData("Images in text: ![star](star) great ![ok](check) done"));
        Items.Add(new TodoItemData("Brush that shiny black coat ![dog](dog)"));
    }

    private static WriteableBitmap CreateSolidBitmap(Color color)
    {
        const int size = 32;
        var bmp = new WriteableBitmap(
            new PixelSize(size, size),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using var buf = bmp.Lock();
        var pixels = new byte[size * size * 4];
        for (int i = 0; i < size * size; i++)
        {
            pixels[i * 4 + 0] = color.B;
            pixels[i * 4 + 1] = color.G;
            pixels[i * 4 + 2] = color.R;
            pixels[i * 4 + 3] = 255;
        }
        System.Runtime.InteropServices.Marshal.Copy(pixels, 0, buf.Address, pixels.Length);

        return bmp;
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (TodoItemData item in e.OldItems)
                item.PropertyChanged -= OnItemPropertyChanged;
        if (e.NewItems != null)
            foreach (TodoItemData item in e.NewItems)
                item.PropertyChanged += OnItemPropertyChanged;
        UpdateStatus();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateStatus();

    private void UpdateStatus()
    {
        int checkedCount = 0;
        foreach (var item in Items)
            if (item.IsChecked) checkedCount++;
        StatusText = $"Items: {Items.Count}, checked: {checkedCount}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
