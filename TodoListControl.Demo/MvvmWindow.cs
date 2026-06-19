using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using TodoListControl.Controls;

namespace TodoListControl.Demo;

public class MvvmWindow : Window
{
    public MvvmWindow()
    {
        Title = "TodoList Editor — MVVM Demo";
        Width = 900;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var vm = new TodoViewModel();
        DataContext = vm;

        var editor = new TodoListEditor
        {
            DefaultFont = new FontFamily("Segoe UI"),
            DefaultFontSize = 15,
            [!TodoListEditor.ItemsProperty] = new Binding("Items")
        };

        editor.ImageStore["star"] = CreateSampleBitmap(Colors.Gold);
        editor.ImageStore["check"] = CreateSampleBitmap(Colors.LimeGreen);

        editor.ItemsChanged += (_, _) =>
        {
            if (DataContext is TodoViewModel v)
            {
                v.StatusText = $"Editor changed — items: {v.ItemCount}, checked: {v.CheckedCount}";
            }
        };

        var addButton = new Button { Content = "+ Add item", Margin = new Thickness(4) };
        addButton[!Button.CommandProperty] = new Binding("AddItemCommand");

        var checkAllButton = new Button { Content = "Check all", Margin = new Thickness(4) };
        checkAllButton[!Button.CommandProperty] = new Binding("CheckAllCommand");

        var uncheckAllButton = new Button { Content = "Uncheck all", Margin = new Thickness(4) };
        uncheckAllButton[!Button.CommandProperty] = new Binding("UncheckAllCommand");

        var removeCheckedButton = new Button { Content = "Remove checked", Margin = new Thickness(4) };
        removeCheckedButton[!Button.CommandProperty] = new Binding("RemoveCheckedCommand");

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(4),
            Children = { addButton, checkAllButton, uncheckAllButton, removeCheckedButton }
        };

        var scrollViewer = new ScrollViewer
        {
            Content = editor,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var itemCountText = new TextBlock { Margin = new Thickness(8, 4), FontSize = 12 };
        itemCountText[!TextBlock.TextProperty] = new Binding("ItemCount") { StringFormat = "Items: {0}" };

        var checkedCountText = new TextBlock { Margin = new Thickness(8, 4), FontSize = 12 };
        checkedCountText[!TextBlock.TextProperty] = new Binding("CheckedCount") { StringFormat = "Checked: {0}" };

        var statusText = new TextBlock
        {
            Margin = new Thickness(8, 4),
            FontSize = 12,
            Foreground = Brushes.Gray
        };
        statusText[!TextBlock.TextProperty] = new Binding("StatusText");

        var statusBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { itemCountText, checkedCountText, statusText }
        };

        var vmItemsList = new ListBox
        {
            Width = 250,
            FontSize = 12,
            [!ListBox.ItemsSourceProperty] = new Binding("Items")
        };
        vmItemsList.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<TodoListControl.Model.TodoItemData>(
            (item, _) =>
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                var cb = new CheckBox();
                cb[!CheckBox.IsCheckedProperty] = new Binding("IsChecked");
                var tb = new TextBlock { VerticalAlignment = VerticalAlignment.Center, MaxWidth = 180, TextTrimming = TextTrimming.CharacterEllipsis };
                tb[!TextBlock.TextProperty] = new Binding("Text");
                panel.Children.Add(cb);
                panel.Children.Add(tb);
                return panel;
            });

        var vmPanel = new DockPanel { Width = 250 };
        var vmHeader = new TextBlock
        {
            Text = "ViewModel Items",
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(8, 4),
            FontSize = 13
        };
        DockPanel.SetDock(vmHeader, Dock.Top);
        vmPanel.Children.Add(vmHeader);
        vmPanel.Children.Add(vmItemsList);

        var editorPanel = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        editorPanel.Children.Add(toolbar);
        editorPanel.Children.Add(scrollViewer);

        var splitPanel = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("*, Auto, 250")
        };
        Grid.SetColumn(editorPanel, 0);

        var splitter = new GridSplitter
        {
            Width = 4,
            Background = Brushes.LightGray
        };
        Grid.SetColumn(splitter, 1);
        Grid.SetColumn(vmPanel, 2);

        splitPanel.Children.Add(editorPanel);
        splitPanel.Children.Add(splitter);
        splitPanel.Children.Add(vmPanel);

        var mainPanel = new DockPanel();
        DockPanel.SetDock(statusBar, Dock.Bottom);
        mainPanel.Children.Add(statusBar);
        mainPanel.Children.Add(splitPanel);

        Content = mainPanel;
    }

    private static WriteableBitmap CreateSampleBitmap(Color bgColor)
    {
        var bmp = new WriteableBitmap(
            new PixelSize(32, 32),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using (var buf = bmp.Lock())
        {
            unsafe
            {
                byte* ptr = (byte*)buf.Address;
                byte r = bgColor.R, g = bgColor.G, b = bgColor.B;
                for (int i = 0; i < 32 * 32; i++)
                {
                    ptr[i * 4 + 0] = b;
                    ptr[i * 4 + 1] = g;
                    ptr[i * 4 + 2] = r;
                    ptr[i * 4 + 3] = 255;
                }
            }
        }

        return bmp;
    }
}
