using System;

namespace TodoList.Avalonia.Model;

public enum ChangeKind
{
    TextChanged,
    CheckedChanged,
    StructureChanged,
    ImageChanged
}

public class TodoItemsChangedEventArgs : EventArgs
{
    public ChangeKind Kind { get; }

    public TodoItemsChangedEventArgs(ChangeKind kind)
    {
        Kind = kind;
    }
}
