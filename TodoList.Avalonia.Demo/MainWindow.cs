using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Layout;
using global::Avalonia.Media;
using TodoList.Avalonia.Controls;
using TodoList.Avalonia.Model;

namespace TodoList.Avalonia.Demo;

public class MainWindow : Window
{
    private readonly TodoListEditor _editor;
    private readonly TextBlock _statusBar;

    public MainWindow()
    {
        Title = "TodoList Editor — Demo";
        Width = 800;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _editor = new TodoListEditor
        {
            DefaultFont = new FontFamily("Segoe UI"),
            DefaultFontSize = 15,
            Margin = new Thickness(0)
        };

        _statusBar = new TextBlock
        {
            Text = "Ready. Click checkbox to check. Type text. Ctrl+V pastes text/images. Shift+arrows to select.",
            Margin = new Thickness(8, 4),
            FontSize = 12,
            Foreground = Brushes.Gray
        };

        var addButton = new Button
        {
            Content = "+ Add item",
            Margin = new Thickness(4),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        addButton.Click += (_, _) =>
        {
            _editor.AddItem("");
            _editor.Caret = new CursorPosition(_editor.Document.Items.Count - 1, 0);
            _editor.SelectionAnchor = _editor.Caret;
            _editor.Focus();
            _editor.InvalidateVisual();
        };

        var checkAllButton = new Button
        {
            Content = "✓ Check all",
            Margin = new Thickness(4)
        };
        checkAllButton.Click += (_, _) =>
        {
            foreach (var item in _editor.Document.Items)
                item.IsChecked = true;
            _editor.InvalidateVisual();
        };

        var uncheckAllButton = new Button
        {
            Content = "✗ Uncheck all",
            Margin = new Thickness(4)
        };
        uncheckAllButton.Click += (_, _) =>
        {
            foreach (var item in _editor.Document.Items)
                item.IsChecked = false;
            _editor.InvalidateVisual();
        };

        var fontCombo = new ComboBox
        {
            Items = { "Segoe UI", "Consolas", "Arial", "Courier New", "Times New Roman" },
            SelectedIndex = 0,
            Margin = new Thickness(4),
            Width = 160
        };
        fontCombo.SelectionChanged += (_, _) =>
        {
            if (fontCombo.SelectedItem is string fontName)
            {
                _editor.DefaultFont = new FontFamily(fontName);
                _editor.Focus();
            }
        };

        var fontSizeCombo = new ComboBox
        {
            Items = { "12", "14", "16", "18", "20", "24", "28" },
            SelectedIndex = 2,
            Margin = new Thickness(4),
            Width = 70
        };
        fontSizeCombo.SelectionChanged += (_, _) =>
        {
            if (fontSizeCombo.SelectedItem is string sizeStr && double.TryParse(sizeStr, out var size))
            {
                _editor.DefaultFontSize = size;
                _editor.Focus();
            }
        };

        var imageDisplayCombo = new ComboBox
        {
            Items = { "Inline", "Block" },
            SelectedIndex = 0,
            Margin = new Thickness(4),
            Width = 100
        };
        imageDisplayCombo.SelectionChanged += (_, _) =>
        {
            if (imageDisplayCombo.SelectedItem is string mode)
            {
                _editor.ImageDisplay = mode == "Block"
                    ? ImageDisplayMode.Block
                    : ImageDisplayMode.Inline;
                _editor.Focus();
            }
        };

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(4),
            Children = { addButton, checkAllButton, uncheckAllButton, fontCombo, fontSizeCombo, imageDisplayCombo }
        };

        var scrollViewer = new ScrollViewer
        {
            Content = _editor,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var mainPanel = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        DockPanel.SetDock(_statusBar, Dock.Bottom);
        mainPanel.Children.Add(toolbar);
        mainPanel.Children.Add(_statusBar);
        mainPanel.Children.Add(scrollViewer);

        Content = mainPanel;

        _editor.Document.Items.Clear();
        _editor.Document.Items.Add(new TodoItem("Buy milk"));
        _editor.Document.Items.Add(new TodoItem("Walk the dog", true));
        _editor.Document.Items.Add(new TodoItem("Code review PR #42"));
        _editor.Document.Items.Add(new TodoItem("Paste multiline text here (Ctrl+V)"));
        _editor.Document.Items.Add(new TodoItem("Paste an image here (Ctrl+V)"));
        _editor.InvalidateVisual();
    }
}
