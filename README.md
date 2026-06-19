# TodoList.Avalonia

[![NuGet](https://img.shields.io/nuget/v/TodoList.Avalonia)](https://www.nuget.org/packages/TodoList.Avalonia)

A custom rich-text todo-list editor control for [Avalonia UI](https://avaloniaui.net/), targeting .NET 10.

![TodoList.Avalonia screenshot](docs/screenshot.png)

## Features

- Checkbox todo items with strikethrough for completed items
- Rich text editing with inline images (inline and block display modes)
- Full undo/redo
- Clipboard integration — paste text and images
- Fully themeable — all colors and layout constants exposed as Avalonia `StyledProperty`
- MVVM-ready — bind to `Items` property with `ObservableCollection<TodoItemData>`
- Cross-platform via Avalonia (Windows, macOS, Linux)

## Installation

```bash
dotnet add package TodoList.Avalonia
```

## Usage

```csharp
var editor = new TodoListEditor
{
    DefaultFont = new FontFamily("Segoe UI"),
    DefaultFontSize = 15
};

// Add items directly
editor.Document.Items.Add(new TodoItem("Buy milk"));
editor.Document.Items.Add(new TodoItem("Walk the dog", isChecked: true));

// Or bind via MVVM
editor[!TodoListEditor.ItemsProperty] = new Binding("Items");
```

### Inline Images

Register images in the `ImageStore` dictionary, then reference them in item text:

```csharp
editor.ImageStore["star"] = myBitmap;
// Item text: "Rating: ![star](star) excellent"
```

## Building from Source

```bash
dotnet build TodoList.Avalonia.slnx
dotnet run --project TodoList.Avalonia.Demo
dotnet test TodoList.Avalonia.Tests
```

## Project Structure

```
TodoList.Avalonia/           Core library
  Controls/
    TodoListEditor.cs      Main editor control (rendering, input, selection)
  Model/
    DocumentModel.cs       TodoDocument, TodoItem, ContentElement
    TodoItemData.cs        MVVM data model with INotifyPropertyChanged
TodoList.Avalonia.Demo/      Demo application
TodoList.Avalonia.Tests/     NUnit tests (headless Avalonia)
```

## License

MIT
