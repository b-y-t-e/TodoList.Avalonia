using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TodoListControl.Model;

public class TodoItemData : INotifyPropertyChanged
{
    private string _text = string.Empty;
    private bool _isChecked;

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }
    }

    public TodoItemData() { }

    public TodoItemData(string text, bool isChecked = false)
    {
        _text = text;
        _isChecked = isChecked;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
