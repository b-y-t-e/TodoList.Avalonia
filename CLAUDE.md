# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

TodoList.Avalonia — a custom rich-text todo-list editor control for Avalonia UI, published as a NuGet package (`TodoList.Avalonia`). Targets .NET 10. MIT licensed.

## Build & Test

```bash
dotnet build TodoList.Avalonia.slnx
dotnet run --project TodoList.Avalonia.Demo
dotnet run --project TodoList.Avalonia.Demo -- --mvvm
dotnet test TodoList.Avalonia.Tests
dotnet test TodoList.Avalonia.Tests --filter "TestName"
```

Tests use NUnit — `[AvaloniaTest]` for UI tests (headless Avalonia), `[Test]` for pure logic (TodoMarkdown, TodoImageEntry).

## Architecture

### Rendering & Input

`TodoListEditor` extends Avalonia `Control` directly — no XAML templates. It owns the full rendering pipeline:

- `MeasureOverride` / `ArrangeOverride` compute line wrapping and item layout
- `Render()` draws everything via `DrawingContext`: checkboxes, text runs, inline/block images, selection highlight, caret
- `OnKeyDown` / `OnTextInput` handle keyboard; `OnPointerPressed` / `OnPointerMoved` / `OnPointerReleased` handle mouse

### Two-layer data model

| Layer | Class | Purpose |
|-------|-------|---------|
| MVVM (public) | `TodoItemData` | POCO with `Text` + `IsChecked`, implements `INotifyPropertyChanged` |
| Internal (rich) | `TodoItem` → `List<ContentElement>` | Mixed text/image elements with font info, cursor math, word boundaries |

The editor's `Items` StyledProperty holds `IList<TodoItemData>`. Sync methods bridge the layers:

- **`ResetDocumentFromItems()`** — full reload when `Items` property is set, clears undo, marks clean
- **`ApplyItemsCollectionChange()`** — incremental rebuild on collection Add/Remove, preserves dirty state
- **`SyncToItems()`** — serializes internal `TodoItem` back to `TodoItemData.Text`
- **`SuppressSync()`** — guard that prevents circular updates during batch operations

### Image handling (MVVM-first)

Two bindable StyledProperties:

- **`Items`** (`IList<TodoItemData>?`) — todo items, text may contain `![alt](key)` image references
- **`Images`** (`IEnumerable<TodoImageEntry>?`) — image entries with `Key` (string) and `Bitmap` (nullable for deferred loading)

Resolution: `_imageCache` (built from `Images` collection) → `_legacyImageStore` (backs `[Obsolete] ImageStore`) → null (renders as placeholder text). Images and Items can be bound in any order — placeholders resolve reactively when images arrive or `Bitmap` is set later.

`ImagePasted` event fires on Ctrl+V image paste. `TodoImageEntry` auto-added to `Images` if it's `ICollection<T>`.

### Dirty tracking

Generation counter: `_changeGeneration` incremented on each change, compared to `_cleanGeneration` in O(1). `MarkClean()` saves current generation. `IsDirty` is a read-only `DirectProperty` with `DirtyChanged` event.

### Markdown helpers

`TodoMarkdown.ParseMarkdown(string?)` / `ToMarkdown(IEnumerable<TodoItemData>?)` — static helpers for `- [x] text` / `- [ ] text` format.

### Configuration surface

All visual properties (colors, fonts, sizes, padding) are Avalonia `StyledProperty` — bindable from XAML or code. `ColorTheme` (Light/Dark/None) applies a preset palette. `DefaultFontName` ↔ `DefaultFont` sync bidirectionally.

## Demo app

- **`MainWindow`** — direct/imperative API usage (legacy `ImageStore`, programmatic events)
- **`MvvmWindow`** — XAML + code-behind MVVM demo (`MvvmWindow.axaml` + `TodoViewModel`)

## Conventions

- All code, comments, commit messages, branch names, PR descriptions, and UI strings must be in **English**, regardless of the language the user communicates in.
- Commit messages follow conventional commits: `type(scope): description`
- This is a library — changes must work cross-platform (Windows, Linux, macOS) and should not break future Android support.
