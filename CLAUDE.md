# TodoList.Avalonia

Avalonia UI custom todo-list editor control for .NET 10.

## Build & Run

```bash
dotnet build TodoList.Avalonia.slnx
dotnet run -p TodoList.Avalonia.Demo
dotnet test TodoList.Avalonia.Tests
```

## Project Structure

- **TodoList.Avalonia/** — core library: `TodoListEditor` control, `DocumentModel`, `TodoItemData`
- **TodoList.Avalonia.Demo/** — demo app with direct API usage (`MainWindow`) and MVVM binding (`MvvmWindow`)
- **TodoList.Avalonia.Tests/** — NUnit tests (headless Avalonia)

## Conventions

- All code, comments, commit messages, branch names, PR descriptions, and UI strings must be in **English**, regardless of the language the user communicates in.
- Commit messages follow conventional commits format: `type(scope): description`
