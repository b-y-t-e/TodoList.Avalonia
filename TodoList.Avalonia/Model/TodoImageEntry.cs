using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;

namespace TodoList.Avalonia.Model;

public class TodoImageEntry : INotifyPropertyChanged
{
    private string _key;
    private Bitmap? _bitmap;

    public string Key
    {
        get => _key;
        set { if (_key != value) { _key = value ?? throw new ArgumentNullException(nameof(value)); OnPropertyChanged(); } }
    }

    public Bitmap? Bitmap
    {
        get => _bitmap;
        set { if (!ReferenceEquals(_bitmap, value)) { _bitmap = value; OnPropertyChanged(); } }
    }

    public TodoImageEntry(string key, Bitmap? bitmap = null)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        _bitmap = bitmap;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
