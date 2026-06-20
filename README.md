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
- **MVVM-first** — bind `Items` and `Images` collections, zero imperative code needed
- Dirty tracking — `IsDirty` property with `DirtyChanged` event and `MarkClean()`
- Markdown helpers — `TodoMarkdown.ParseMarkdown()` / `ToMarkdown()` for `- [x] text` format
- Cross-platform via Avalonia (Windows, macOS, Linux)

## Installation

```bash
dotnet add package TodoList.Avalonia
```

## Usage (MVVM)

### 1. MarkdownText — single-string binding

Bind `MarkdownText` for two-way markdown sync (simplest approach):

```xml
<todo:TodoListEditor MarkdownText="{Binding Markdown}" />
```

### 2. Items + Images — collection binding

Bind `Items` and `Images` collections for full control:

```xml
<todo:TodoListEditor Items="{Binding Items}" Images="{Binding Images}" />
```

```csharp
// ViewModel
public ObservableCollection<TodoItemData> Items { get; } = new();
public ObservableCollection<TodoImageEntry> Images { get; } = new();
```

### Markdown helpers

Static helpers for manual conversion (accepts both `- [x] text` and `[x] text` formats):

```csharp
// Load
foreach (var item in TodoMarkdown.ParseMarkdown(markdown))
    Items.Add(item);

// Save
var markdown = TodoMarkdown.ToMarkdown(Items);
```

### Deferred image loading

```csharp
var entry = new TodoImageEntry("photo");  // null bitmap
Images.Add(entry);
// ...later, when loaded:
entry.Bitmap = await LoadBitmapAsync(url);  // triggers re-render
```

### Image references in text

Item text can contain markdown-compatible image references: `![alt](key)`. Images resolve from the bound `Images` collection by key.

## Building from Source

```bash
dotnet build TodoList.Avalonia.slnx
dotnet run --project TodoList.Avalonia.Demo
dotnet run --project TodoList.Avalonia.Demo -- --mvvm
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
    TodoImageEntry.cs      Observable image entry (Key + Bitmap)
    TodoMarkdown.cs        Markdown parse/serialize helpers
    TodoItemsChangedEventArgs.cs  ChangeKind enum + event args
    ImagePastedEventArgs.cs       Image paste event args
TodoList.Avalonia.Demo/      Demo application
  MvvmWindow.axaml/.cs    XAML MVVM demo
  MainWindow.cs            Imperative/legacy demo
  TodoViewModel.cs         Sample ViewModel
TodoList.Avalonia.Tests/     NUnit tests (headless Avalonia)
```

## Roadmap

- [ ] Android soft keyboard support — implement `ITextInputMethodClient` so the on-screen keyboard appears when the editor receives focus on mobile platforms

## License

MIT
