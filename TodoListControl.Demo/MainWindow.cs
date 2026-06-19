using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using TodoListControl.Controls;
using TodoListControl.Model;

namespace TodoListControl.Demo;

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
            Text = "Gotowe. Kliknij checkbox aby zaznaczyć. Wpisuj tekst. Ctrl+V wkleja tekst/obrazki. Shift+strzałki zaznaczają.",
            Margin = new Thickness(8, 4),
            FontSize = 12,
            Foreground = Brushes.Gray
        };

        var addButton = new Button
        {
            Content = "+ Dodaj punkt",
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
            Content = "✓ Zaznacz wszystkie",
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
            Content = "✗ Odznacz wszystkie",
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

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(4),
            Children = { addButton, checkAllButton, uncheckAllButton, fontCombo, fontSizeCombo }
        };

        var scrollViewer = new ScrollViewer
        {
            Content = _editor,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var mainPanel = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        DockPanel.SetDock(_statusBar, Dock.Bottom);
        mainPanel.Children.Add(toolbar);
        mainPanel.Children.Add(_statusBar);
        mainPanel.Children.Add(scrollViewer);

        Content = mainPanel;

        _editor.Document.Items.Clear();
        _editor.Document.Items.Add(new TodoItem("Kupić mleko"));
        _editor.Document.Items.Add(new TodoItem("Wyprowadzić psa", true));
        _editor.Document.Items.Add(new TodoItem("Code review PR #42"));
        _editor.Document.Items.Add(new TodoItem("Wkleić tu tekst wieloliniowy (Ctrl+V)"));
        _editor.Document.Items.Add(new TodoItem("Wkleić tu obrazek (Ctrl+V)"));
        _editor.InvalidateVisual();
    }
}
