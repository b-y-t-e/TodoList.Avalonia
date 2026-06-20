using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Data;
using global::Avalonia.Controls.Primitives;
using global::Avalonia.Input;
using global::Avalonia.Layout;
using global::Avalonia.Input.Platform;
using global::Avalonia.Media;
using global::Avalonia.Media.Imaging;
using TodoList.Avalonia.Model;

namespace TodoList.Avalonia.Controls;

public enum ImageDisplayMode { Inline, Block }

public enum EditorTheme { None, Light, Dark }

public class TodoListEditor : Control
{
    public static readonly StyledProperty<EditorTheme> ColorThemeProperty =
        AvaloniaProperty.Register<TodoListEditor, EditorTheme>(nameof(ColorTheme), EditorTheme.Light);

    public static readonly StyledProperty<FontFamily> DefaultFontProperty =
        AvaloniaProperty.Register<TodoListEditor, FontFamily>(nameof(DefaultFont), FontFamily.Default);

    public static readonly StyledProperty<double> DefaultFontSizeProperty =
        AvaloniaProperty.Register<TodoListEditor, double>(nameof(DefaultFontSize), 14.0);

    public static readonly StyledProperty<IList<TodoItemData>?> ItemsProperty =
        AvaloniaProperty.Register<TodoListEditor, IList<TodoItemData>?>(nameof(Items));

    public static readonly StyledProperty<double> InlineImageMaxHeightProperty =
        AvaloniaProperty.Register<TodoListEditor, double>(nameof(InlineImageMaxHeight), 100.0);

    public static readonly StyledProperty<ImageDisplayMode> ImageDisplayProperty =
        AvaloniaProperty.Register<TodoListEditor, ImageDisplayMode>(
            nameof(ImageDisplay), ImageDisplayMode.Block);

    public static readonly StyledProperty<IBrush> BackgroundBrushProperty =
        AvaloniaProperty.Register<TodoListEditor, IBrush>(nameof(BackgroundBrush), Brushes.White);

    public static readonly StyledProperty<IBrush> ForegroundProperty =
        AvaloniaProperty.Register<TodoListEditor, IBrush>(nameof(Foreground), Brushes.Black);

    public static readonly StyledProperty<IBrush> CheckedForegroundProperty =
        AvaloniaProperty.Register<TodoListEditor, IBrush>(nameof(CheckedForeground), Brushes.Gray);

    public static readonly StyledProperty<IBrush> SelectionBrushProperty =
        AvaloniaProperty.Register<TodoListEditor, IBrush>(nameof(SelectionBrush),
            new SolidColorBrush(Color.FromArgb(80, 30, 144, 255)));

    public static readonly StyledProperty<IBrush> CaretBrushProperty =
        AvaloniaProperty.Register<TodoListEditor, IBrush>(nameof(CaretBrush), Brushes.Black);

    public static readonly StyledProperty<IBrush> CheckboxCheckedBrushProperty =
        AvaloniaProperty.Register<TodoListEditor, IBrush>(nameof(CheckboxCheckedBrush), Brushes.DodgerBlue);

    public static readonly StyledProperty<IBrush> CheckboxUncheckedBrushProperty =
        AvaloniaProperty.Register<TodoListEditor, IBrush>(nameof(CheckboxUncheckedBrush), Brushes.White);

    public static readonly StyledProperty<IBrush> CheckboxBorderBrushProperty =
        AvaloniaProperty.Register<TodoListEditor, IBrush>(nameof(CheckboxBorderBrush), Brushes.Gray);

    public static readonly StyledProperty<IBrush> CheckmarkBrushProperty =
        AvaloniaProperty.Register<TodoListEditor, IBrush>(nameof(CheckmarkBrush), Brushes.White);

    public static readonly StyledProperty<Thickness> EditorPaddingProperty =
        AvaloniaProperty.Register<TodoListEditor, Thickness>(nameof(EditorPadding), new Thickness(32, 8, 8, 8));

    public static readonly StyledProperty<double> CheckboxSizeProperty =
        AvaloniaProperty.Register<TodoListEditor, double>(nameof(CheckboxSize), 16.0);

    public static readonly StyledProperty<double> CheckboxMarginRightProperty =
        AvaloniaProperty.Register<TodoListEditor, double>(nameof(CheckboxMarginRight), 8.0);

    public static readonly StyledProperty<double> LineSpacingProperty =
        AvaloniaProperty.Register<TodoListEditor, double>(nameof(LineSpacing), 6.0);

    public static readonly StyledProperty<double> WrapLineSpacingProperty =
        AvaloniaProperty.Register<TodoListEditor, double>(nameof(WrapLineSpacing), 2.0);

    public static readonly StyledProperty<IEnumerable<TodoImageEntry>?> ImagesProperty =
        AvaloniaProperty.Register<TodoListEditor, IEnumerable<TodoImageEntry>?>(nameof(Images));

    public static readonly DirectProperty<TodoListEditor, bool> IsDirtyProperty =
        AvaloniaProperty.RegisterDirect<TodoListEditor, bool>(nameof(IsDirty), o => o.IsDirty);

    public static readonly StyledProperty<string?> DefaultFontNameProperty =
        AvaloniaProperty.Register<TodoListEditor, string?>(nameof(DefaultFontName));

    public static readonly StyledProperty<string?> MarkdownTextProperty =
        AvaloniaProperty.Register<TodoListEditor, string?>(nameof(MarkdownText),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IBrush> CheckboxFocusBrushProperty =
        AvaloniaProperty.Register<TodoListEditor, IBrush>(nameof(CheckboxFocusBrush), Brushes.DodgerBlue);

    public static readonly StyledProperty<bool> MoveCheckedToEndProperty =
        AvaloniaProperty.Register<TodoListEditor, bool>(nameof(MoveCheckedToEnd), false);

    public static readonly StyledProperty<bool> CheckboxNavigationProperty =
        AvaloniaProperty.Register<TodoListEditor, bool>(nameof(CheckboxNavigation), false);

    public EditorTheme ColorTheme
    {
        get => GetValue(ColorThemeProperty);
        set => SetValue(ColorThemeProperty, value);
    }

    public FontFamily DefaultFont
    {
        get => GetValue(DefaultFontProperty);
        set => SetValue(DefaultFontProperty, value);
    }

    public double DefaultFontSize
    {
        get => GetValue(DefaultFontSizeProperty);
        set => SetValue(DefaultFontSizeProperty, value);
    }

    public IList<TodoItemData>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public double InlineImageMaxHeight
    {
        get => GetValue(InlineImageMaxHeightProperty);
        set => SetValue(InlineImageMaxHeightProperty, value);
    }

    public ImageDisplayMode ImageDisplay
    {
        get => GetValue(ImageDisplayProperty);
        set => SetValue(ImageDisplayProperty, value);
    }

    public IBrush BackgroundBrush
    {
        get => GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    public IBrush Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public IBrush CheckedForeground
    {
        get => GetValue(CheckedForegroundProperty);
        set => SetValue(CheckedForegroundProperty, value);
    }

    public IBrush SelectionBrush
    {
        get => GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    public IBrush CaretBrush
    {
        get => GetValue(CaretBrushProperty);
        set => SetValue(CaretBrushProperty, value);
    }

    public IBrush CheckboxCheckedBrush
    {
        get => GetValue(CheckboxCheckedBrushProperty);
        set => SetValue(CheckboxCheckedBrushProperty, value);
    }

    public IBrush CheckboxUncheckedBrush
    {
        get => GetValue(CheckboxUncheckedBrushProperty);
        set => SetValue(CheckboxUncheckedBrushProperty, value);
    }

    public IBrush CheckboxBorderBrush
    {
        get => GetValue(CheckboxBorderBrushProperty);
        set => SetValue(CheckboxBorderBrushProperty, value);
    }

    public IBrush CheckmarkBrush
    {
        get => GetValue(CheckmarkBrushProperty);
        set => SetValue(CheckmarkBrushProperty, value);
    }

    public IBrush CheckboxFocusBrush
    {
        get => GetValue(CheckboxFocusBrushProperty);
        set => SetValue(CheckboxFocusBrushProperty, value);
    }

    public Thickness EditorPadding
    {
        get => GetValue(EditorPaddingProperty);
        set => SetValue(EditorPaddingProperty, value);
    }

    public double CheckboxSize
    {
        get => GetValue(CheckboxSizeProperty);
        set => SetValue(CheckboxSizeProperty, value);
    }

    public double CheckboxMarginRight
    {
        get => GetValue(CheckboxMarginRightProperty);
        set => SetValue(CheckboxMarginRightProperty, value);
    }

    public double LineSpacing
    {
        get => GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }

    public double WrapLineSpacing
    {
        get => GetValue(WrapLineSpacingProperty);
        set => SetValue(WrapLineSpacingProperty, value);
    }

    public IEnumerable<TodoImageEntry>? Images
    {
        get => GetValue(ImagesProperty);
        set => SetValue(ImagesProperty, value);
    }

    private bool _isDirty;
    public bool IsDirty => _isDirty;

    public string? DefaultFontName
    {
        get => GetValue(DefaultFontNameProperty);
        set => SetValue(DefaultFontNameProperty, value);
    }

    public string? MarkdownText
    {
        get => GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    public bool MoveCheckedToEnd
    {
        get => GetValue(MoveCheckedToEndProperty);
        set => SetValue(MoveCheckedToEndProperty, value);
    }

    public bool CheckboxNavigation
    {
        get => GetValue(CheckboxNavigationProperty);
        set => SetValue(CheckboxNavigationProperty, value);
    }

    private readonly Dictionary<string, Bitmap> _legacyImageStore = new();

    [Obsolete("Use the Images StyledProperty instead.")]
    public Dictionary<string, Bitmap> ImageStore => _legacyImageStore;

    public event EventHandler? ItemsChanged;
    public event EventHandler<TodoItemsChangedEventArgs>? ItemsDetailChanged;
    public event EventHandler<ImagePastedEventArgs>? ImagePasted;
    public event EventHandler? DirtyChanged;

    public TodoDocument Document { get; } = new();
    public CursorPosition Caret { get; set; } = CursorPosition.Start;
    public CursorPosition SelectionAnchor { get; set; } = CursorPosition.Start;
    public bool HasSelection => !CurrentSelection.IsEmpty;

    private bool _mouseSelecting;

    private readonly List<double> _itemYPositions = new();
    private readonly List<double> _itemHeights = new();
    private readonly List<List<WrappedLine>> _itemWrapping = new();
    private double _desiredHeight;
    private double _desiredWidth;

    private List<List<ContentElement>>? _internalClipboard;
    private string? _internalClipboardText;
    private int _suppressSyncCount;
    // _imageCache: built from Images collection; _legacyImageStore: backs [Obsolete] ImageStore for backward compat
    private readonly Dictionary<string, Bitmap> _imageCache = new();
    private readonly List<TodoImageEntry> _subscribedImageEntries = new();
    private long _changeGeneration;
    private long _cleanGeneration;
    private bool _syncingFontProperties;
    private bool _syncingMarkdownText;
    private bool _checkboxFocused;
    private int _checkboxFocusIndex;
    private double _goalX = -1;

    private void SyncMarkdownTextFromItems()
    {
        if (_syncingMarkdownText) return;
        _syncingMarkdownText = true;
        try { MarkdownText = TodoMarkdown.ToMarkdown(Items); }
        finally { _syncingMarkdownText = false; }
    }

    private void ApplyParsedMarkdownToItems(List<TodoItemData> parsed)
    {
        var items = Items;
        if (items == null)
        {
            Items = new ObservableCollection<TodoItemData>(parsed);
            return;
        }

        using (SuppressSync())
        {
            for (int i = 0; i < parsed.Count; i++)
            {
                if (i < items.Count)
                {
                    if (items[i].Text != parsed[i].Text) items[i].Text = parsed[i].Text;
                    if (items[i].IsChecked != parsed[i].IsChecked) items[i].IsChecked = parsed[i].IsChecked;
                }
                else
                {
                    var newData = new TodoItemData(parsed[i].Text, parsed[i].IsChecked);
                    newData.PropertyChanged += OnItemDataPropertyChanged;
                    items.Add(newData);
                }
            }

            while (items.Count > parsed.Count)
            {
                items[items.Count - 1].PropertyChanged -= OnItemDataPropertyChanged;
                items.RemoveAt(items.Count - 1);
            }
        }

        ResetDocumentFromItems();
    }

    private SyncGuard SuppressSync() => new(this);

    private readonly struct SyncGuard : IDisposable
    {
        private readonly TodoListEditor _editor;
        public SyncGuard(TodoListEditor editor)
        {
            _editor = editor;
            _editor._suppressSyncCount++;
        }
        public void Dispose() => _editor._suppressSyncCount--;
    }

    private const int MaxUndoSteps = 100;
    private const long CoalesceThresholdMs = 800;
    private readonly List<UndoSnapshot> _undoStack = new();
    private readonly List<UndoSnapshot> _redoStack = new();
    private UndoActionKind _lastUndoAction;
    private long _lastUndoTimestamp;

    internal enum UndoActionKind { Other, Typing, Backspace, Delete }

    private record UndoSnapshot(
        List<(bool IsChecked, List<ContentElement> Elements)> Items,
        CursorPosition Caret,
        CursorPosition Anchor);

    private static readonly Regex ImagePattern =
        new(@"!\[([^\]]*)\]\(([^)]+)\)", RegexOptions.Compiled);

    public SelectionRange CurrentSelection => new(SelectionAnchor, Caret);

    private class WrappedLine
    {
        public double YOffset;
        public double Height;
        public int StartGlobalOffset;
        public int EndGlobalOffset;
        public readonly List<WrappedSegment> Segments = new();
    }

    private class WrappedSegment
    {
        public int ElementIndex;
        public int LocalStart;
        public int LocalEnd;
        public double X;
        public double Width;
        public double Height;
    }

    public TodoListEditor()
    {
        Focusable = true;
        IsTabStop = true;
        ClipToBounds = true;
        SyncDocumentDefaults();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        UnsubscribeItems(Items);
        UnsubscribeImages(Images);
        _imageCache.Clear();
    }

    private void UnsubscribeItems(IList<TodoItemData>? items)
    {
        if (items == null) return;
        if (items is INotifyCollectionChanged ncc)
            ncc.CollectionChanged -= OnItemsCollectionChanged;
        foreach (var item in items)
            item.PropertyChanged -= OnItemDataPropertyChanged;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DefaultFontProperty || change.Property == DefaultFontSizeProperty)
        {
            SyncDocumentDefaults();
            if (change.Property == DefaultFontProperty && !_syncingFontProperties)
            {
                _syncingFontProperties = true;
                try
                {
                    var font = (FontFamily?)change.NewValue;
                    if (font != null)
                        DefaultFontName = font.Name;
                }
                finally { _syncingFontProperties = false; }
            }
            InvalidateMeasure();
        }
        else if (change.Property == DefaultFontNameProperty && !_syncingFontProperties)
        {
            _syncingFontProperties = true;
            try
            {
                var name = (string?)change.NewValue;
                if (name != null)
                    DefaultFont = new FontFamily(name);
            }
            finally { _syncingFontProperties = false; }
        }
        else if (change.Property == InlineImageMaxHeightProperty
            || change.Property == ImageDisplayProperty
            || change.Property == EditorPaddingProperty
            || change.Property == CheckboxSizeProperty
            || change.Property == CheckboxMarginRightProperty
            || change.Property == LineSpacingProperty
            || change.Property == WrapLineSpacingProperty)
        {
            InvalidateMeasure();
        }
        else if (change.Property == ColorThemeProperty)
        {
            ApplyTheme((EditorTheme)change.NewValue!);
        }
        else if (change.Property == BackgroundBrushProperty
            || change.Property == ForegroundProperty
            || change.Property == CheckedForegroundProperty
            || change.Property == SelectionBrushProperty
            || change.Property == CaretBrushProperty
            || change.Property == CheckboxCheckedBrushProperty
            || change.Property == CheckboxUncheckedBrushProperty
            || change.Property == CheckboxBorderBrushProperty
            || change.Property == CheckmarkBrushProperty
            || change.Property == CheckboxFocusBrushProperty)
        {
            InvalidateVisual();
        }
        else if (change.Property == ItemsProperty)
        {
            OnItemsPropertyChanged(
                change.OldValue as IList<TodoItemData>,
                change.NewValue as IList<TodoItemData>);
        }
        else if (change.Property == ImagesProperty)
        {
            OnImagesPropertyChanged(
                change.OldValue as IEnumerable<TodoImageEntry>,
                change.NewValue as IEnumerable<TodoImageEntry>);
        }
        else if (change.Property == MarkdownTextProperty && !_syncingMarkdownText)
        {
            _syncingMarkdownText = true;
            try
            {
                var parsed = TodoMarkdown.ParseMarkdown(change.NewValue as string);
                ApplyParsedMarkdownToItems(parsed);
            }
            finally { _syncingMarkdownText = false; }
        }
        else if (change.Property == MoveCheckedToEndProperty && (bool)change.NewValue!)
        {
            SortCheckedToEnd();
        }
    }

    private void ApplyTheme(EditorTheme theme)
    {
        if (theme == EditorTheme.None) return;

        if (theme == EditorTheme.Light)
        {
            BackgroundBrush = Brushes.White;
            Foreground = Brushes.Black;
            CheckedForeground = Brushes.Gray;
            SelectionBrush = new SolidColorBrush(Color.FromArgb(80, 30, 144, 255));
            CaretBrush = Brushes.Black;
            CheckboxCheckedBrush = Brushes.DodgerBlue;
            CheckboxUncheckedBrush = Brushes.White;
            CheckboxBorderBrush = Brushes.Gray;
            CheckmarkBrush = Brushes.White;
            CheckboxFocusBrush = Brushes.DodgerBlue;
        }
        else if (theme == EditorTheme.Dark)
        {
            BackgroundBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220));
            CheckedForeground = new SolidColorBrush(Color.FromRgb(120, 120, 120));
            SelectionBrush = new SolidColorBrush(Color.FromArgb(80, 60, 140, 230));
            CaretBrush = Brushes.White;
            CheckboxCheckedBrush = new SolidColorBrush(Color.FromRgb(55, 148, 255));
            CheckboxUncheckedBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            CheckboxBorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            CheckmarkBrush = Brushes.White;
            CheckboxFocusBrush = new SolidColorBrush(Color.FromRgb(80, 170, 255));
        }
    }

    private void SyncDocumentDefaults()
    {
        Document.DefaultFont = DefaultFont;
        Document.DefaultFontSize = DefaultFontSize;
    }

    // ---- Layout ----

    private void ComputeLayout()
    {
        _itemYPositions.Clear();
        _itemHeights.Clear();
        _itemWrapping.Clear();

        double availWidth = Math.Max((_desiredWidth > 0 ? _desiredWidth : 400) - EditorPadding.Left - EditorPadding.Right, 50);
        double y = EditorPadding.Top;

        for (int i = 0; i < Document.Items.Count; i++)
        {
            var item = Document.Items[i];
            _itemYPositions.Add(y);

            var wrappedLines = ComputeItemWrapping(item, availWidth);
            _itemWrapping.Add(wrappedLines);

            double itemH = 0;
            for (int li = 0; li < wrappedLines.Count; li++)
            {
                itemH += wrappedLines[li].Height;
                if (li < wrappedLines.Count - 1) itemH += WrapLineSpacing;
            }
            itemH = Math.Max(itemH, Document.DefaultFontSize + 4);

            _itemHeights.Add(itemH);
            y += itemH + LineSpacing;
        }

        _desiredHeight = y + EditorPadding.Top;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double w = double.IsInfinity(availableSize.Width) ? 400 : availableSize.Width;
        _desiredWidth = Math.Max(w, 200);
        ComputeLayout();
        return new Size(_desiredWidth, _desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double newWidth = Math.Max(finalSize.Width, 200);
        if (_itemWrapping.Count == 0 || Math.Abs(newWidth - _desiredWidth) > 0.1)
        {
            _desiredWidth = newWidth;
            ComputeLayout();
        }
        return new Size(_desiredWidth, Math.Max(finalSize.Height, _desiredHeight));
    }

    // ---- Rendering ----

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(BackgroundBrush, new Rect(Bounds.Size));

        if (_itemWrapping.Count != Document.Items.Count)
            ComputeLayout();

        var (selFirst, selLast) = CurrentSelection.Ordered();
        var selBrush = SelectionBrush;

        for (int i = 0; i < Document.Items.Count; i++)
        {
            var item = Document.Items[i];
            double itemY = _itemYPositions[i];
            var wrappedLines = _itemWrapping[i];

            double cbX = (EditorPadding.Left - CheckboxSize) / 2;
            double firstLineH = wrappedLines.Count > 0 ? wrappedLines[0].Height : (Document.DefaultFontSize + 4);
            double cbY = itemY + firstLineH - CheckboxSize;
            DrawCheckbox(context, cbX, cbY, item.IsChecked);

            if (_checkboxFocused && i == _checkboxFocusIndex && _checkboxFocusIndex < Document.Items.Count)
            {
                var s = CheckboxSize;
                var highlightRect = new Rect(cbX - 3, cbY - 3, s + 6, s + 6);
                var baseColor = (CheckboxFocusBrush as ISolidColorBrush)?.Color ?? Colors.DodgerBlue;
                var highlightBrush = new SolidColorBrush(Color.FromArgb(40, baseColor.R, baseColor.G, baseColor.B));
                context.FillRectangle(highlightBrush, highlightRect);
                var focusPen = new Pen(CheckboxFocusBrush, 2.5);
                context.DrawRectangle(null, focusPen, new RoundedRect(highlightRect, s * 0.25));
            }

            if (item.Elements.Count == 0)
            {
                if (i == Caret.ItemIndex && !HasSelection && !_checkboxFocused)
                    context.FillRectangle(CaretBrush,
                        new Rect(EditorPadding.Left, itemY, 1.5, Document.DefaultFontSize + 4));
                continue;
            }

            foreach (var wLine in wrappedLines)
            {
                double lineY = itemY + wLine.YOffset;

                foreach (var seg in wLine.Segments)
                {
                    var el = item.Elements[seg.ElementIndex];
                    double segX = EditorPadding.Left + seg.X;
                    int segGlobalStart = item.GlobalOffset(seg.ElementIndex, seg.LocalStart);

                    double segY = lineY + (wLine.Height - seg.Height);

                    if (el.Type == ContentElementType.Image && el.Image != null)
                    {
                        var imgRect = new Rect(segX, segY, seg.Width - 2, seg.Height);
                        context.DrawImage(el.Image, imgRect);

                        bool inSel = IsOffsetInSelection(i, segGlobalStart, selFirst, selLast);
                        if (inSel)
                            context.FillRectangle(selBrush, imgRect);
                    }
                    else
                    {
                        var (typeface, fs) = ResolveFont(el);
                        string segText = el.Text[seg.LocalStart..seg.LocalEnd];

                        int selStartInSeg = Math.Max(0,
                            (i == selFirst.ItemIndex ? selFirst.Offset : 0) - segGlobalStart);
                        int selEndInSeg = Math.Min(segText.Length,
                            (i == selLast.ItemIndex ? selLast.Offset : int.MaxValue) - segGlobalStart);
                        if (!CurrentSelection.IsEmpty && selStartInSeg < selEndInSeg
                            && i >= selFirst.ItemIndex && i <= selLast.ItemIndex)
                        {
                            double sX = selStartInSeg > 0
                                ? MeasureTextWidth(segText[..selStartInSeg], typeface, fs) : 0;
                            double sW = MeasureTextWidth(segText[selStartInSeg..selEndInSeg], typeface, fs);
                            context.FillRectangle(selBrush, new Rect(segX + sX, segY, sW, seg.Height));
                        }

                        var fmt = new FormattedText(segText,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight, typeface, fs,
                            item.IsChecked ? CheckedForeground : Foreground);

                        if (item.IsChecked)
                        {
                            double textY = segY + fmt.Height / 2;
                            context.DrawLine(new Pen(CheckedForeground, 1),
                                new Point(segX, textY), new Point(segX + fmt.Width, textY));
                        }

                        context.DrawText(fmt, new Point(segX, segY));
                    }
                }
            }

            if (i == Caret.ItemIndex && !HasSelection && !_checkboxFocused)
            {
                var (caretX, caretYOff, caretLineH) = CalculateCaretPosition(i, Caret.Offset);
                context.FillRectangle(CaretBrush,
                    new Rect(EditorPadding.Left + caretX, itemY + caretYOff, 1.5, caretLineH));
            }
        }
    }

    private void DrawCheckbox(DrawingContext ctx, double x, double y, bool isChecked)
    {
        var s = CheckboxSize;
        var r = s * 0.2;
        var rect = new RoundedRect(new Rect(x, y, s, s), r);

        ctx.DrawRectangle(isChecked ? CheckboxCheckedBrush : CheckboxUncheckedBrush,
            new Pen(isChecked ? CheckboxCheckedBrush : CheckboxBorderBrush, 1.5), rect);

        if (isChecked)
        {
            var pen = new Pen(CheckmarkBrush, s * 0.13)
            {
                LineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            ctx.DrawLine(pen,
                new Point(x + s * 0.22, y + s * 0.52),
                new Point(x + s * 0.40, y + s * 0.72));
            ctx.DrawLine(pen,
                new Point(x + s * 0.40, y + s * 0.72),
                new Point(x + s * 0.78, y + s * 0.28));
        }
    }

    private bool IsOffsetInSelection(int itemIdx, int offset,
        CursorPosition selFirst, CursorPosition selLast)
    {
        if (CurrentSelection.IsEmpty) return false;
        if (itemIdx < selFirst.ItemIndex || itemIdx > selLast.ItemIndex) return false;
        if (itemIdx == selFirst.ItemIndex && itemIdx == selLast.ItemIndex)
            return offset >= selFirst.Offset && offset < selLast.Offset;
        if (itemIdx == selFirst.ItemIndex)
            return offset >= selFirst.Offset;
        if (itemIdx == selLast.ItemIndex)
            return offset < selLast.Offset;
        return true;
    }

    private (double x, double yOffset, double lineHeight) CalculateCaretPosition(
        int itemIndex, int globalOffset)
    {
        double textH = Document.DefaultFontSize + 4;

        if (itemIndex >= _itemWrapping.Count)
            return (0, 0, textH);

        var wrappedLines = _itemWrapping[itemIndex];
        var item = Document.Items[itemIndex];

        for (int li = 0; li < wrappedLines.Count; li++)
        {
            var wLine = wrappedLines[li];
            bool isLastLine = li == wrappedLines.Count - 1;

            bool nextStartsWithImage = !isLastLine
                && wrappedLines[li + 1].Segments.Count > 0
                && item.Elements[wrappedLines[li + 1].Segments[0].ElementIndex].Type == ContentElementType.Image;

            if (globalOffset < wLine.EndGlobalOffset
                || (isLastLine && globalOffset <= wLine.EndGlobalOffset)
                || (globalOffset == wLine.EndGlobalOffset && nextStartsWithImage))
            {
                foreach (var seg in wLine.Segments)
                {
                    int segGlobalStart = item.GlobalOffset(seg.ElementIndex, seg.LocalStart);
                    int segLen = seg.LocalEnd - seg.LocalStart;

                    if (globalOffset <= segGlobalStart + segLen)
                    {
                        int localOff = Math.Max(0, globalOffset - segGlobalStart);
                        var el = item.Elements[seg.ElementIndex];
                        double x;

                        if (el.Type == ContentElementType.Image)
                        {
                            x = seg.X + (localOff > 0 ? seg.Width : 0);
                        }
                        else
                        {
                            var (typeface, fs) = ResolveFont(el);
                            x = seg.X + MeasureTextWidth(
                                el.Text[seg.LocalStart..(seg.LocalStart + localOff)], typeface, fs);
                        }

                        double h = seg.Height;
                        return (x, wLine.YOffset + (wLine.Height - h), h);
                    }
                }

                double endX = 0;
                double endH = textH;
                if (wLine.Segments.Count > 0)
                {
                    var lastSeg = wLine.Segments[^1];
                    endX = lastSeg.X + lastSeg.Width;
                    endH = lastSeg.Height;
                }
                return (endX, wLine.YOffset + (wLine.Height - endH), endH);
            }
        }

        if (wrappedLines.Count > 0)
        {
            var lastLine = wrappedLines[^1];
            double x = 0;
            if (lastLine.Segments.Count > 0)
            {
                var lastSeg = lastLine.Segments[^1];
                x = lastSeg.X + lastSeg.Width;
            }
            double lastH = lastLine.Segments.Count > 0 ? lastLine.Segments[^1].Height : textH;
            return (x, lastLine.YOffset + (lastLine.Height - lastH), lastH);
        }

        return (0, 0, textH);
    }

    private (int start, int end) GetCurrentWrappedLineRange(int itemIndex, int globalOffset)
    {
        if (itemIndex >= _itemWrapping.Count)
            return (0, 0);

        var wrappedLines = _itemWrapping[itemIndex];
        for (int li = 0; li < wrappedLines.Count; li++)
        {
            var wLine = wrappedLines[li];
            bool isLastLine = li == wrappedLines.Count - 1;
            if (globalOffset < wLine.EndGlobalOffset
                || (isLastLine && globalOffset <= wLine.EndGlobalOffset))
            {
                return (wLine.StartGlobalOffset, wLine.EndGlobalOffset);
            }
        }

        if (wrappedLines.Count > 0)
        {
            var last = wrappedLines[^1];
            return (last.StartGlobalOffset, last.EndGlobalOffset);
        }
        return (0, 0);
    }

    private int HitTestLineOffset(TodoItem item, WrappedLine wLine, double targetX)
    {
        foreach (var seg in wLine.Segments)
        {
            var el = item.Elements[seg.ElementIndex];
            int segGlobalStart = item.GlobalOffset(seg.ElementIndex, seg.LocalStart);

            if (el.Type == ContentElementType.Image)
            {
                if (targetX < seg.X + seg.Width / 2) return segGlobalStart;
                if (targetX < seg.X + seg.Width) return segGlobalStart + 1;
            }
            else
            {
                var (typeface, fs) = ResolveFont(el);
                string segText = el.Text[seg.LocalStart..seg.LocalEnd];

                if (targetX <= seg.X + seg.Width)
                {
                    // binary search: O(log n) MeasureTextWidth calls instead of O(n)
                    double relX = targetX - seg.X;
                    int lo = 0, hi = segText.Length;
                    while (lo < hi)
                    {
                        int mid = (lo + hi) / 2;
                        double w = MeasureTextWidth(segText[..(mid + 1)], typeface, fs);
                        if (w <= relX)
                            lo = mid + 1;
                        else
                            hi = mid;
                    }
                    if (lo < segText.Length)
                    {
                        double leftEdge = lo > 0 ? MeasureTextWidth(segText[..lo], typeface, fs) : 0;
                        double charW = MeasureTextWidth(segText[lo..(lo + 1)], typeface, fs);
                        if (relX < leftEdge + charW / 2) return segGlobalStart + lo;
                        return segGlobalStart + lo + 1;
                    }
                    return segGlobalStart + segText.Length;
                }
            }
        }

        if (wLine.Segments.Count > 0)
        {
            var lastSeg = wLine.Segments[^1];
            return item.GlobalOffset(lastSeg.ElementIndex, lastSeg.LocalEnd);
        }
        return wLine.StartGlobalOffset;
    }

    private Typeface BuildTypeface(ContentElement el)
    {
        return new Typeface(
            el.Font != FontFamily.Default ? el.Font : Document.DefaultFont,
            el.Italic ? FontStyle.Italic : FontStyle.Normal,
            el.Bold ? FontWeight.Bold : FontWeight.Normal);
    }

    private double ResolveFontSize(ContentElement el) =>
        el.FontSize > 0 ? el.FontSize : Document.DefaultFontSize;

    private (Typeface typeface, double fontSize) ResolveFont(ContentElement el) =>
        (BuildTypeface(el), ResolveFontSize(el));

    private double MeasureTextWidth(string text, Typeface typeface, double fontSize)
    {
        if (text.Length == 0) return 0;
        var fmt = new FormattedText(text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, typeface, fontSize, Foreground);
        return fmt.WidthIncludingTrailingWhitespace;
    }

    // ---- Wrapping ----

    private List<WrappedLine> ComputeItemWrapping(TodoItem item, double availWidth)
    {
        var lines = new List<WrappedLine>();
        var currentLine = new WrappedLine();
        lines.Add(currentLine);
        double lineX = 0;
        double lineH = Document.DefaultFontSize + 4;
        int globalOff = 0;

        for (int ei = 0; ei < item.Elements.Count; ei++)
        {
            var el = item.Elements[ei];

            if (el.Type == ContentElementType.Image && el.Image != null)
            {
                double imgH = Math.Min(el.ImageHeight, InlineImageMaxHeight);
                double scale = imgH / el.ImageHeight;
                double imgW = el.ImageWidth * scale + 2;
                bool blockMode = ImageDisplay == ImageDisplayMode.Block;

                if (lineX > 0 && (blockMode || lineX + imgW > availWidth))
                {
                    currentLine.Height = lineH;
                    currentLine.EndGlobalOffset = globalOff;
                    currentLine = new WrappedLine { StartGlobalOffset = globalOff };
                    lines.Add(currentLine);
                    lineX = 0;
                    lineH = Document.DefaultFontSize + 4;
                }

                currentLine.Segments.Add(new WrappedSegment
                {
                    ElementIndex = ei, LocalStart = 0, LocalEnd = 1,
                    X = lineX, Width = imgW, Height = imgH
                });

                lineX += imgW;
                lineH = Math.Max(lineH, imgH);
                globalOff += 1;

                if (blockMode)
                {
                    currentLine.Height = lineH;
                    currentLine.EndGlobalOffset = globalOff;
                    currentLine = new WrappedLine { StartGlobalOffset = globalOff };
                    lines.Add(currentLine);
                    lineX = 0;
                    lineH = Document.DefaultFontSize + 4;
                }
            }
            else if (el.Type == ContentElementType.Text && el.Text.Length > 0)
            {
                var (typeface, fs) = ResolveFont(el);
                double textH = fs + 4;

                int textStart = 0;
                while (textStart < el.Text.Length)
                {
                    string remaining = el.Text[textStart..];
                    double remainingWidth = MeasureTextWidth(remaining, typeface, fs);

                    if (lineX + remainingWidth <= availWidth)
                    {
                        currentLine.Segments.Add(new WrappedSegment
                        {
                            ElementIndex = ei, LocalStart = textStart, LocalEnd = el.Text.Length,
                            X = lineX, Width = remainingWidth, Height = textH
                        });
                        lineX += remainingWidth;
                        lineH = Math.Max(lineH, textH);
                        globalOff += el.Text.Length - textStart;
                        break;
                    }

                    double availForText = availWidth - lineX;
                    int breakPos = FindTextBreakPosition(el.Text, textStart, typeface, fs, availForText);

                    if (breakPos <= textStart)
                    {
                        if (currentLine.Segments.Count > 0)
                        {
                            currentLine.Height = lineH;
                            currentLine.EndGlobalOffset = globalOff;
                            currentLine = new WrappedLine { StartGlobalOffset = globalOff };
                            lines.Add(currentLine);
                            lineX = 0;
                            lineH = Document.DefaultFontSize + 4;
                            continue;
                        }
                        breakPos = textStart + 1;
                    }

                    string chunk = el.Text[textStart..breakPos];
                    double chunkWidth = MeasureTextWidth(chunk, typeface, fs);

                    currentLine.Segments.Add(new WrappedSegment
                    {
                        ElementIndex = ei, LocalStart = textStart, LocalEnd = breakPos,
                        X = lineX, Width = chunkWidth, Height = textH
                    });

                    lineH = Math.Max(lineH, textH);
                    globalOff += breakPos - textStart;
                    textStart = breakPos;

                    currentLine.Height = lineH;
                    currentLine.EndGlobalOffset = globalOff;
                    currentLine = new WrappedLine { StartGlobalOffset = globalOff };
                    lines.Add(currentLine);
                    lineX = 0;
                    lineH = Document.DefaultFontSize + 4;
                }
            }
        }

        currentLine.Height = lineH;
        currentLine.EndGlobalOffset = item.TextLength;

        if (lines.Count > 1 && lines[^1].Segments.Count == 0)
            lines.RemoveAt(lines.Count - 1);

        double y = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            lines[i].YOffset = y;
            y += lines[i].Height;
            if (i < lines.Count - 1) y += WrapLineSpacing;
        }

        return lines;
    }

    private int FindTextBreakPosition(string text, int start, Typeface typeface,
        double fontSize, double availWidth)
    {
        if (availWidth <= 0) return start;

        int len = text.Length - start;
        if (len <= 0) return start;

        if (MeasureTextWidth(text[start..], typeface, fontSize) <= availWidth)
            return text.Length;

        // binary search: O(log n) MeasureTextWidth calls instead of O(n)
        int lo = 1, hi = len;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            double w = MeasureTextWidth(text[start..(start + mid)], typeface, fontSize);
            if (w <= availWidth)
                lo = mid + 1;
            else
                hi = mid;
        }

        int fitCount = lo - 1;
        int breakAt = start + fitCount;

        if (breakAt > start)
        {
            int lastSpace = text.LastIndexOf(' ', breakAt - 1, breakAt - start);
            if (lastSpace > start) return lastSpace + 1;
        }

        return breakAt > start ? breakAt : start;
    }

    // ---- Mouse input ----

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        _goalX = -1;

        var pos = e.GetPosition(this);

        if (pos.X < EditorPadding.Left - 4)
        {
            int itemIdx = HitTestItem(pos.Y);
            if (itemIdx >= 0 && itemIdx < Document.Items.Count)
            {
                if (CheckboxNavigation)
                {
                    _checkboxFocused = true;
                    _checkboxFocusIndex = itemIdx;
                    InvalidateVisual();
                }
                SaveUndoState();
                ToggleItemCore(itemIdx);
            }
            return;
        }

        if (_checkboxFocused)
        {
            _checkboxFocused = false;
            InvalidateVisual();
        }

        var cursor = HitTestCursor(pos);
        var props = e.GetCurrentPoint(this).Properties;

        if (e.ClickCount == 2 && props.IsLeftButtonPressed)
        {
            Caret = cursor;
            SelectWordAtCaret();
            _mouseSelecting = false;
            InvalidateMeasure();
            return;
        }

        Caret = cursor;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            _mouseSelecting = true;
        }
        else
        {
            SelectionAnchor = cursor;
            _mouseSelecting = true;
        }

        InvalidateMeasure();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_mouseSelecting) return;

        var pos = e.GetPosition(this);
        Caret = HitTestCursor(pos);
        InvalidateMeasure();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _mouseSelecting = false;
    }

    private int HitTestItem(double y)
    {
        for (int i = 0; i < _itemYPositions.Count; i++)
        {
            if (y >= _itemYPositions[i] && y < _itemYPositions[i] + _itemHeights[i] + LineSpacing)
                return i;
        }
        return Document.Items.Count - 1;
    }

    private CursorPosition HitTestCursor(Point pos)
    {
        int itemIdx = HitTestItem(pos.Y);
        if (itemIdx < 0) return CursorPosition.Start;
        itemIdx = Math.Clamp(itemIdx, 0, Document.Items.Count - 1);

        var item = Document.Items[itemIdx];
        double relX = pos.X - EditorPadding.Left;
        if (relX < 0) return new CursorPosition(itemIdx, 0);

        if (itemIdx >= _itemWrapping.Count)
            return new CursorPosition(itemIdx, 0);

        var wrappedLines = _itemWrapping[itemIdx];
        double itemY = _itemYPositions[itemIdx];
        double relY = pos.Y - itemY;

        WrappedLine? targetLine = null;
        foreach (var wLine in wrappedLines)
        {
            if (relY < wLine.YOffset + wLine.Height + WrapLineSpacing
                || wLine == wrappedLines[^1])
            {
                targetLine = wLine;
                break;
            }
        }

        if (targetLine == null || targetLine.Segments.Count == 0)
            return new CursorPosition(itemIdx, item.TextLength);

        return new CursorPosition(itemIdx, HitTestLineOffset(item, targetLine, relX));
    }

    // ---- Keyboard input ----

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (string.IsNullOrEmpty(e.Text)) return;
        _goalX = -1;

        if (_checkboxFocused)
        {
            e.Handled = true;
            return;
        }

        SaveUndoState(HasSelection ? UndoActionKind.Other : UndoActionKind.Typing);
        DeleteSelection();
        InsertTextAtCaret(e.Text);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (_checkboxFocused && HandleCheckboxNavigationKey(e))
            return;

        if (e.Key != Key.Up && e.Key != Key.Down)
            _goalX = -1;

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        switch (e.Key)
        {
            case Key.Enter:
                SaveUndoState();
                DeleteSelection();
                SplitItemAtCaret();
                e.Handled = true;
                break;

            case Key.Back when ctrl:
                SaveUndoState();
                if (HasSelection)
                    DeleteSelection();
                else
                    DeleteWordBackward();
                e.Handled = true;
                break;

            case Key.Back:
                SaveUndoState(HasSelection ? UndoActionKind.Other : UndoActionKind.Backspace);
                if (HasSelection)
                    DeleteSelection();
                else
                    HandleBackspace();
                e.Handled = true;
                break;

            case Key.Delete when ctrl:
                SaveUndoState();
                if (HasSelection)
                    DeleteSelection();
                else
                    DeleteWordForward();
                e.Handled = true;
                break;

            case Key.Delete:
                SaveUndoState(HasSelection ? UndoActionKind.Other : UndoActionKind.Delete);
                if (HasSelection)
                    DeleteSelection();
                else
                    HandleDelete();
                e.Handled = true;
                break;

            case Key.Left when ctrl:
                MoveCaretByWord(-1, shift);
                e.Handled = true;
                break;

            case Key.Left:
                if (CheckboxNavigation && !shift && Caret.Offset == 0)
                {
                    _checkboxFocused = true;
                    _checkboxFocusIndex = Caret.ItemIndex;
                    InvalidateVisual();
                }
                else
                {
                    MoveCaret(-1, shift);
                }
                e.Handled = true;
                break;

            case Key.Right when ctrl:
                MoveCaretByWord(1, shift);
                e.Handled = true;
                break;

            case Key.Right:
                MoveCaret(1, shift);
                e.Handled = true;
                break;

            case Key.Up:
                MoveCaretVertical(-1, shift);
                e.Handled = true;
                break;

            case Key.Down:
                MoveCaretVertical(1, shift);
                e.Handled = true;
                break;

            case Key.Home when ctrl:
                var docStart = CursorPosition.Start;
                if (!shift) SelectionAnchor = docStart;
                Caret = docStart;
                InvalidateMeasure();
                e.Handled = true;
                break;

            case Key.Home:
                var (lineStart, _) = GetCurrentWrappedLineRange(Caret.ItemIndex, Caret.Offset);
                var home = new CursorPosition(Caret.ItemIndex, lineStart);
                if (!shift) SelectionAnchor = home;
                Caret = home;
                InvalidateMeasure();
                e.Handled = true;
                break;

            case Key.End when ctrl:
                var lastItem = Document.Items[^1];
                var docEnd = new CursorPosition(Document.Items.Count - 1, lastItem.TextLength);
                if (!shift) SelectionAnchor = docEnd;
                Caret = docEnd;
                InvalidateMeasure();
                e.Handled = true;
                break;

            case Key.End:
                var (_, lineEnd) = GetCurrentWrappedLineRange(Caret.ItemIndex, Caret.Offset);
                var end = new CursorPosition(Caret.ItemIndex, lineEnd);
                if (!shift) SelectionAnchor = end;
                Caret = end;
                InvalidateMeasure();
                e.Handled = true;
                break;

            case Key.A when ctrl:
                SelectAll();
                e.Handled = true;
                break;

            case Key.C when ctrl:
                CopyToClipboard();
                e.Handled = true;
                break;

            case Key.X when ctrl:
                SaveUndoState();
                CopyToClipboard();
                DeleteSelection();
                e.Handled = true;
                break;

            case Key.V when ctrl:
                SaveUndoState();
                _ = PasteFromClipboard();
                e.Handled = true;
                break;

            case Key.Z when ctrl:
                Undo();
                e.Handled = true;
                break;

            case Key.Y when ctrl:
                Redo();
                e.Handled = true;
                break;
        }
    }

    private TodoItem CurrentItem => Document.Items[Math.Clamp(Caret.ItemIndex, 0, Document.Items.Count - 1)];

    // ---- Edit operations ----

    public void InsertTextAtCaret(string text)
    {
        EnsureValidCaret();
        var item = Document.Items[Caret.ItemIndex];
        int offset = Math.Min(Caret.Offset, item.TextLength);

        if (item.Elements.Count == 0)
        {
            item.Elements.Add(ContentElement.CreateText(text));
            Caret = new CursorPosition(Caret.ItemIndex, text.Length);
        }
        else
        {
            var (elIdx, localOff) = item.ResolveOffset(offset);
            var el = item.Elements[elIdx];

            if (el.Type == ContentElementType.Text)
            {
                el.Text = el.Text.Insert(localOff, text);
                Caret = new CursorPosition(Caret.ItemIndex, offset + text.Length);
            }
            else
            {
                var newEl = ContentElement.CreateText(text);
                item.Elements.Insert(elIdx + (localOff > 0 ? 1 : 0), newEl);
                Caret = new CursorPosition(Caret.ItemIndex, offset + text.Length);
            }
        }

        ClearSelectionNoDelete();
        InvalidateMeasure();
        NotifyDocumentChanged();
    }

    private string RegisterPastedImage(Bitmap bitmap)
    {
        string key = $"img_{Guid.NewGuid():N}";
        _imageCache[key] = bitmap;

        var args = new ImagePastedEventArgs(bitmap, key);
        ImagePasted?.Invoke(this, args);

        if (args.NewKey != null && args.NewKey != key)
        {
            _imageCache.Remove(key);
            key = args.NewKey;
            _imageCache[key] = bitmap;
        }

        if (Images is ICollection<TodoImageEntry> imageCollection)
            imageCollection.Add(new TodoImageEntry(key, bitmap));

        _legacyImageStore[key] = bitmap;
        return key;
    }

    public void InsertImageAtCaret(Bitmap bitmap)
    {
        EnsureValidCaret();
        SaveUndoState();
        InsertImageAtCaretCore(bitmap);
    }

    private void InsertImageAtCaretCore(Bitmap bitmap)
    {
        DeleteSelection();
        var item = Document.Items[Caret.ItemIndex];
        int offset = Math.Min(Caret.Offset, item.TextLength);

        string key = RegisterPastedImage(bitmap);

        var imgEl = ContentElement.CreateImage(bitmap);
        imgEl.ImageKey = key;

        if (item.Elements.Count == 0)
        {
            item.Elements.Add(imgEl);
            Caret = new CursorPosition(Caret.ItemIndex, 1);
        }
        else
        {
            var (elIdx, localOff) = item.ResolveOffset(offset);
            var el = item.Elements[elIdx];

            if (el.Type == ContentElementType.Text && localOff < el.Text.Length)
            {
                var after = ContentElement.CreateText(el.Text[localOff..], el.Font, el.FontSize);
                el.Text = el.Text[..localOff];
                item.Elements.Insert(elIdx + 1, imgEl);
                if (after.Text.Length > 0)
                    item.Elements.Insert(elIdx + 2, after);
            }
            else
            {
                item.Elements.Insert(elIdx + 1, imgEl);
            }

            Caret = new CursorPosition(Caret.ItemIndex, offset + 1);
        }

        ClearSelectionNoDelete();
        InvalidateMeasure();
        NotifyDocumentChanged(ChangeKind.ImageChanged);
    }

    public void SplitItemAtCaret()
    {
        EnsureValidCaret();
        var item = Document.Items[Caret.ItemIndex];
        int offset = Math.Min(Caret.Offset, item.TextLength);

        var newItem = new TodoItem();

        int pos = 0;
        bool split = false;
        for (int i = 0; i < item.Elements.Count; i++)
        {
            var el = item.Elements[i];
            int elLen = el.Type == ContentElementType.Text ? el.Text.Length : 1;

            if (!split && offset <= pos + elLen)
            {
                if (el.Type == ContentElementType.Text)
                {
                    int localOff = offset - pos;
                    string remainder = el.Text[localOff..];
                    el.Text = el.Text[..localOff];

                    if (remainder.Length > 0)
                        newItem.Elements.Add(ContentElement.CreateText(remainder, el.Font, el.FontSize));
                }
                else
                {
                    if (offset == pos)
                    {
                        newItem.Elements.Add(el.Clone());
                        item.Elements.RemoveAt(i);
                        i--;
                    }
                }

                split = true;
                for (int j = i + 1; j < item.Elements.Count; j++)
                    newItem.Elements.Add(item.Elements[j]);
                for (int j = item.Elements.Count - 1; j > i; j--)
                    item.Elements.RemoveAt(j);

                // Clean up empty text elements
                if (item.Elements.Count > 0 && item.Elements[^1].Type == ContentElementType.Text
                    && item.Elements[^1].Text.Length == 0)
                    item.Elements.RemoveAt(item.Elements.Count - 1);

                break;
            }
            pos += elLen;
        }

        Document.Items.Insert(Caret.ItemIndex + 1, newItem);
        Caret = new CursorPosition(Caret.ItemIndex + 1, 0);
        ClearSelectionNoDelete();
        InvalidateMeasure();
        NotifyDocumentChanged(ChangeKind.StructureChanged);
    }

    private void HandleBackspace()
    {
        EnsureValidCaret();
        var item = Document.Items[Caret.ItemIndex];

        if (Caret.Offset > 0)
        {
            int offset = Math.Min(Caret.Offset, item.TextLength);
            var (elIdx, localOff) = item.ResolveOffset(offset - 1);
            var el = item.Elements[elIdx];

            if (el.Type == ContentElementType.Text)
            {
                int deleteAt = localOff;
                if (deleteAt < el.Text.Length)
                {
                    el.Text = el.Text.Remove(deleteAt, 1);
                    if (el.Text.Length == 0) item.Elements.RemoveAt(elIdx);
                }
            }
            else
            {
                item.Elements.RemoveAt(elIdx);
            }

            Caret = new CursorPosition(Caret.ItemIndex, offset - 1);
        }
        else if (Caret.ItemIndex > 0)
        {
            var prevItem = Document.Items[Caret.ItemIndex - 1];
            int prevLen = prevItem.TextLength;

            foreach (var el in item.Elements)
                prevItem.Elements.Add(el);

            Document.Items.RemoveAt(Caret.ItemIndex);
            Caret = new CursorPosition(Caret.ItemIndex - 1, prevLen);
        }

        ClearSelectionNoDelete();
        InvalidateMeasure();
        NotifyDocumentChanged();
    }

    private void HandleDelete()
    {
        EnsureValidCaret();
        var item = Document.Items[Caret.ItemIndex];

        if (Caret.Offset < item.TextLength)
        {
            var (elIdx, localOff) = item.ResolveOffset(Caret.Offset);
            var el = item.Elements[elIdx];

            if (el.Type == ContentElementType.Text)
            {
                if (localOff < el.Text.Length)
                {
                    el.Text = el.Text.Remove(localOff, 1);
                    if (el.Text.Length == 0) item.Elements.RemoveAt(elIdx);
                }
            }
            else
            {
                item.Elements.RemoveAt(elIdx);
            }
        }
        else if (Caret.ItemIndex < Document.Items.Count - 1)
        {
            var nextItem = Document.Items[Caret.ItemIndex + 1];
            foreach (var el in nextItem.Elements)
                item.Elements.Add(el);
            Document.Items.RemoveAt(Caret.ItemIndex + 1);
        }

        ClearSelectionNoDelete();
        InvalidateMeasure();
        NotifyDocumentChanged();
    }

    public void DeleteSelection()
    {
        if (!HasSelection) return;

        var (first, last) = CurrentSelection.Ordered();

        if (first.ItemIndex == last.ItemIndex)
        {
            var item = Document.Items[first.ItemIndex];
            DeleteRange(item, first.Offset, last.Offset);
        }
        else
        {
            var firstItem = Document.Items[first.ItemIndex];
            var lastItem = Document.Items[last.ItemIndex];

            DeleteRange(firstItem, first.Offset, firstItem.TextLength);

            var remainingElements = new List<ContentElement>();
            CollectRange(lastItem, last.Offset, lastItem.TextLength, remainingElements);
            DeleteRange(lastItem, 0, lastItem.TextLength);

            for (int i = last.ItemIndex; i > first.ItemIndex; i--)
                Document.Items.RemoveAt(i);

            foreach (var el in remainingElements)
                firstItem.Elements.Add(el);
        }

        Caret = first;
        ClearSelectionNoDelete();
        EnsureNonEmpty();
        InvalidateMeasure();
        NotifyDocumentChanged();
    }

    private void DeleteRange(TodoItem item, int fromOffset, int toOffset)
    {
        if (fromOffset >= toOffset) return;

        int pos = 0;
        for (int i = item.Elements.Count - 1; i >= 0; i--)
        {
            pos = item.GlobalOffset(i, 0);
            var el = item.Elements[i];
            int elLen = el.Type == ContentElementType.Text ? el.Text.Length : 1;
            int elStart = pos;
            int elEnd = pos + elLen;

            if (elEnd <= fromOffset || elStart >= toOffset) continue;

            int delStart = Math.Max(fromOffset - elStart, 0);
            int delEnd = Math.Min(toOffset - elStart, elLen);

            if (el.Type == ContentElementType.Text)
            {
                el.Text = el.Text[..delStart] + el.Text[delEnd..];
                if (el.Text.Length == 0) item.Elements.RemoveAt(i);
            }
            else
            {
                item.Elements.RemoveAt(i);
            }
        }
    }

    private void CollectRange(TodoItem item, int fromOffset, int toOffset, List<ContentElement> result)
    {
        int pos = 0;
        foreach (var el in item.Elements)
        {
            int elLen = el.Type == ContentElementType.Text ? el.Text.Length : 1;
            int elStart = pos;
            int elEnd = pos + elLen;

            if (elEnd <= fromOffset) { pos += elLen; continue; }
            if (elStart >= toOffset) break;

            int colStart = Math.Max(fromOffset - elStart, 0);
            int colEnd = Math.Min(toOffset - elStart, elLen);

            if (el.Type == ContentElementType.Text)
            {
                var sub = el.Text[colStart..colEnd];
                if (sub.Length > 0)
                    result.Add(ContentElement.CreateText(sub, el.Font, el.FontSize));
            }
            else
            {
                result.Add(el.Clone());
            }

            pos += elLen;
        }
    }

    // ---- Selection ----

    private void SelectWordAtCaret()
    {
        var item = Document.Items[Caret.ItemIndex];
        if (item.TextLength == 0) return;

        int offset = Math.Min(Caret.Offset, item.TextLength - 1);
        var cls = item.ClassifyAt(offset);
        if (cls == CharClass.Image)
        {
            SelectionAnchor = new CursorPosition(Caret.ItemIndex, offset);
            Caret = new CursorPosition(Caret.ItemIndex, offset + 1);
            return;
        }

        int left = offset;
        while (left > 0 && item.ClassifyAt(left - 1) == cls)
            left--;
        int right = offset;
        while (right < item.TextLength && item.ClassifyAt(right) == cls)
            right++;

        SelectionAnchor = new CursorPosition(Caret.ItemIndex, left);
        Caret = new CursorPosition(Caret.ItemIndex, right);
    }

    public void SelectAll()
    {
        SelectionAnchor = CursorPosition.Start;
        var lastItem = Document.Items[^1];
        Caret = new CursorPosition(Document.Items.Count - 1, lastItem.TextLength);
        InvalidateMeasure();
    }

    private void ClearSelectionNoDelete()
    {
        SelectionAnchor = Caret;
    }

    public string GetSelectedText()
    {
        if (!HasSelection) return string.Empty;

        var (first, last) = CurrentSelection.Ordered();
        var sb = new StringBuilder();

        for (int i = first.ItemIndex; i <= last.ItemIndex; i++)
        {
            if (i > first.ItemIndex) sb.AppendLine();
            var item = Document.Items[i];
            int start = i == first.ItemIndex ? first.Offset : 0;
            int end = i == last.ItemIndex ? last.Offset : item.TextLength;

            int pos = 0;
            foreach (var el in item.Elements)
            {
                int elLen = el.Type == ContentElementType.Text ? el.Text.Length : 1;
                int elStart = pos;
                int elEnd = pos + elLen;

                if (elEnd <= start) { pos += elLen; continue; }
                if (elStart >= end) break;

                int colStart = Math.Max(start - elStart, 0);
                int colEnd = Math.Min(end - elStart, elLen);

                if (el.Type == ContentElementType.Text)
                    sb.Append(el.Text[colStart..colEnd]);
                else
                    sb.Append('￼');

                pos += elLen;
            }
        }

        return sb.ToString();
    }

    // ---- Clipboard ----

    private List<List<ContentElement>> CollectSelectedElements()
    {
        var result = new List<List<ContentElement>>();
        if (!HasSelection) return result;

        var (first, last) = CurrentSelection.Ordered();

        for (int i = first.ItemIndex; i <= last.ItemIndex; i++)
        {
            var item = Document.Items[i];
            int start = i == first.ItemIndex ? first.Offset : 0;
            int end = i == last.ItemIndex ? last.Offset : item.TextLength;

            var lineElements = new List<ContentElement>();
            CollectRange(item, start, end, lineElements);
            result.Add(lineElements);
        }

        return result;
    }

    public void CopyToClipboard()
    {
        if (HasSelection)
        {
            _internalClipboard = CollectSelectedElements();
            _internalClipboardText = GetSelectedText();
        }
        else
        {
            _internalClipboard = null;
            _internalClipboardText = null;
        }

        var text = HasSelection ? GetSelectedText() : Document.GetAllText();
        if (TopLevel.GetTopLevel(this) is { Clipboard: { } clipboard })
            clipboard.SetTextAsync(text);
    }

    public async System.Threading.Tasks.Task PasteFromClipboard()
    {
        if (TopLevel.GetTopLevel(this) is not { Clipboard: { } clipboard }) return;

        var sysText = await clipboard.TryGetTextAsync();

        if (_internalClipboard != null && _internalClipboardText != null
            && sysText == _internalClipboardText)
        {
            PasteRichContent(_internalClipboard);
            return;
        }

        var image = await clipboard.TryGetBitmapAsync();
        if (image is Bitmap bmp)
        {
            InsertImageAtCaret(bmp);
            return;
        }
        else if (image != null)
        {
            using var ms = new MemoryStream();
            image.Save(ms);
            ms.Position = 0;
            InsertImageAtCaret(new Bitmap(ms));
            return;
        }

        if (!string.IsNullOrEmpty(sysText))
            PasteMultilineText(sysText);
    }

    public void PasteRichFromInternalClipboard()
    {
        if (_internalClipboard != null)
            PasteRichContent(_internalClipboard);
    }

    private void PasteRichContent(List<List<ContentElement>> lines)
    {
        SaveUndoState();
        using (SuppressSync())
        {
            DeleteSelection();
            for (int i = 0; i < lines.Count; i++)
            {
                foreach (var el in lines[i])
                {
                    if (el.Type == ContentElementType.Image && el.Image != null)
                        InsertImageAtCaretCore(el.Image);
                    else if (el.Type == ContentElementType.Text && el.Text.Length > 0)
                        InsertTextAtCaret(el.Text);
                }
                if (i < lines.Count - 1)
                    SplitItemAtCaret();
            }
        }
        NotifyDocumentChanged();
    }

    public void PasteMultilineText(string text)
    {
        SaveUndoState();
        using (SuppressSync())
        {
            DeleteSelection();
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                if (line.Length > 0)
                    InsertTextAtCaret(line);
                if (i < lines.Length - 1)
                    SplitItemAtCaret();
            }
        }
        NotifyDocumentChanged();
    }

    // ---- Caret movement ----

    private void MoveCaret(int direction, bool extend)
    {
        EnsureValidCaret();
        var newPos = Caret;

        if (direction < 0)
        {
            if (Caret.Offset > 0)
                newPos = new CursorPosition(Caret.ItemIndex, Caret.Offset - 1);
            else if (Caret.ItemIndex > 0)
            {
                var prev = Document.Items[Caret.ItemIndex - 1];
                newPos = new CursorPosition(Caret.ItemIndex - 1, prev.TextLength);
            }
        }
        else
        {
            var item = Document.Items[Caret.ItemIndex];
            if (Caret.Offset < item.TextLength)
                newPos = new CursorPosition(Caret.ItemIndex, Caret.Offset + 1);
            else if (Caret.ItemIndex < Document.Items.Count - 1)
                newPos = new CursorPosition(Caret.ItemIndex + 1, 0);
        }

        Caret = newPos;
        if (!extend) SelectionAnchor = Caret;
        InvalidateMeasure();
    }

    internal void MoveCaretByWord(int direction, bool extend)
    {
        EnsureValidCaret();
        var item = Document.Items[Caret.ItemIndex];

        if (direction < 0)
        {
            int boundary = item.FindWordBoundaryLeft(Caret.Offset);
            if (boundary == Caret.Offset && Caret.Offset == 0 && Caret.ItemIndex > 0)
            {
                var prev = Document.Items[Caret.ItemIndex - 1];
                Caret = new CursorPosition(Caret.ItemIndex - 1, prev.TextLength);
            }
            else
            {
                Caret = new CursorPosition(Caret.ItemIndex, boundary);
            }
        }
        else
        {
            int boundary = item.FindWordBoundaryRight(Caret.Offset);
            if (boundary == Caret.Offset && Caret.Offset == item.TextLength
                && Caret.ItemIndex < Document.Items.Count - 1)
            {
                Caret = new CursorPosition(Caret.ItemIndex + 1, 0);
            }
            else
            {
                Caret = new CursorPosition(Caret.ItemIndex, boundary);
            }
        }

        if (!extend) SelectionAnchor = Caret;
        InvalidateMeasure();
    }

    private void DeleteWordBackward()
    {
        EnsureValidCaret();
        var item = Document.Items[Caret.ItemIndex];

        if (Caret.Offset > 0)
        {
            int boundary = item.FindWordBoundaryLeft(Caret.Offset);
            int offset = Math.Min(Caret.Offset, item.TextLength);
            DeleteRange(item, boundary, offset);
            Caret = new CursorPosition(Caret.ItemIndex, boundary);
        }
        else if (Caret.ItemIndex > 0)
        {
            var prevItem = Document.Items[Caret.ItemIndex - 1];
            int prevLen = prevItem.TextLength;
            foreach (var el in item.Elements)
                prevItem.Elements.Add(el);
            Document.Items.RemoveAt(Caret.ItemIndex);
            Caret = new CursorPosition(Caret.ItemIndex - 1, prevLen);
        }

        ClearSelectionNoDelete();
        InvalidateMeasure();
        NotifyDocumentChanged();
    }

    private void DeleteWordForward()
    {
        EnsureValidCaret();
        var item = Document.Items[Caret.ItemIndex];

        if (Caret.Offset < item.TextLength)
        {
            int boundary = item.FindWordBoundaryRight(Caret.Offset);
            DeleteRange(item, Caret.Offset, boundary);
        }
        else if (Caret.ItemIndex < Document.Items.Count - 1)
        {
            var nextItem = Document.Items[Caret.ItemIndex + 1];
            foreach (var el in nextItem.Elements)
                item.Elements.Add(el);
            Document.Items.RemoveAt(Caret.ItemIndex + 1);
        }

        ClearSelectionNoDelete();
        InvalidateMeasure();
        NotifyDocumentChanged();
    }

    internal void MoveCaretVertical(int direction, bool extend)
    {
        EnsureValidCaret();
        int itemIdx = Caret.ItemIndex;

        if (_goalX < 0)
        {
            var (cx, _, _) = CalculateCaretPosition(itemIdx, Caret.Offset);
            _goalX = cx;
        }

        double useX = _goalX;

        if (itemIdx < _itemWrapping.Count)
        {
            var wrappedLines = _itemWrapping[itemIdx];

            int currentLineIdx = FindCurrentWrappedLine(wrappedLines, Caret.Offset, direction);

            int targetLineIdx = currentLineIdx + direction;

            if (targetLineIdx >= 0 && targetLineIdx < wrappedLines.Count)
            {
                int newOffset = HitTestLineOffset(
                    Document.Items[itemIdx], wrappedLines[targetLineIdx], useX);
                Caret = new CursorPosition(itemIdx, newOffset);
                if (!extend) SelectionAnchor = Caret;
                InvalidateMeasure();
                return;
            }

            if (direction < 0 && itemIdx > 0)
            {
                int prevIdx = itemIdx - 1;
                if (prevIdx < _itemWrapping.Count && _itemWrapping[prevIdx].Count > 0)
                {
                    int newOffset = HitTestLineOffset(
                        Document.Items[prevIdx], _itemWrapping[prevIdx][^1], useX);
                    Caret = new CursorPosition(prevIdx, newOffset);
                }
                else
                {
                    Caret = new CursorPosition(prevIdx, Document.Items[prevIdx].TextLength);
                }
            }
            else if (direction < 0 && itemIdx == 0 && Caret.Offset > 0)
            {
                Caret = new CursorPosition(0, 0);
            }
            else if (direction > 0 && itemIdx < Document.Items.Count - 1)
            {
                int nextIdx = itemIdx + 1;
                if (nextIdx < _itemWrapping.Count && _itemWrapping[nextIdx].Count > 0)
                {
                    int newOffset = HitTestLineOffset(
                        Document.Items[nextIdx], _itemWrapping[nextIdx][0], useX);
                    Caret = new CursorPosition(nextIdx, newOffset);
                }
                else
                {
                    Caret = new CursorPosition(nextIdx, 0);
                }
            }
            else if (direction > 0 && itemIdx == Document.Items.Count - 1)
            {
                int endOffset = Document.Items[itemIdx].TextLength;
                if (Caret.Offset < endOffset)
                    Caret = new CursorPosition(itemIdx, endOffset);
            }
        }
        else
        {
            int newItem = Caret.ItemIndex + direction;
            newItem = Math.Clamp(newItem, 0, Document.Items.Count - 1);
            int offset = Math.Min(Caret.Offset, Document.Items[newItem].TextLength);
            Caret = new CursorPosition(newItem, offset);
        }

        if (!extend) SelectionAnchor = Caret;
        InvalidateMeasure();
    }

    private static int FindCurrentWrappedLine(List<WrappedLine> wrappedLines, int offset, int direction)
    {
        for (int li = 0; li < wrappedLines.Count; li++)
        {
            int end = wrappedLines[li].EndGlobalOffset;
            if (offset < end)
                return li;
            if (offset == end)
            {
                // At a wrap boundary: if moving down, prefer the next line (cursor is at its start)
                if (direction > 0 && li + 1 < wrappedLines.Count)
                    return li + 1;
                return li;
            }
        }
        return wrappedLines.Count - 1;
    }

    // ---- Checkbox navigation ----

    private bool HandleCheckboxNavigationKey(KeyEventArgs e)
    {
        if (Document.Items.Count == 0)
        {
            e.Handled = true;
            return true;
        }

        _checkboxFocusIndex = Math.Clamp(_checkboxFocusIndex, 0, Document.Items.Count - 1);

        switch (e.Key)
        {
            case Key.Space:
                if (_checkboxFocusIndex >= 0 && _checkboxFocusIndex < Document.Items.Count)
                {
                    SaveUndoState();
                    ToggleItemCore(_checkboxFocusIndex);
                }
                e.Handled = true;
                return true;

            case Key.Up:
                if (_checkboxFocusIndex > 0)
                {
                    _checkboxFocusIndex--;
                    InvalidateVisual();
                }
                e.Handled = true;
                return true;

            case Key.Down:
                if (_checkboxFocusIndex < Document.Items.Count - 1)
                {
                    _checkboxFocusIndex++;
                    InvalidateVisual();
                }
                e.Handled = true;
                return true;

            case Key.PageUp:
                _checkboxFocusIndex = Math.Max(0, _checkboxFocusIndex - VisibleItemCount());
                InvalidateVisual();
                e.Handled = true;
                return true;

            case Key.PageDown:
                _checkboxFocusIndex = Math.Min(Document.Items.Count - 1,
                    _checkboxFocusIndex + VisibleItemCount());
                InvalidateVisual();
                e.Handled = true;
                return true;

            case Key.Home:
                _checkboxFocusIndex = 0;
                InvalidateVisual();
                e.Handled = true;
                return true;

            case Key.End:
                _checkboxFocusIndex = Document.Items.Count - 1;
                InvalidateVisual();
                e.Handled = true;
                return true;

            case Key.Right:
                _checkboxFocused = false;
                Caret = new CursorPosition(_checkboxFocusIndex, 0);
                SelectionAnchor = Caret;
                InvalidateMeasure();
                e.Handled = true;
                return true;

            case Key.Escape:
                _checkboxFocused = false;
                InvalidateVisual();
                e.Handled = true;
                return true;

            default:
                _checkboxFocused = false;
                InvalidateVisual();
                return false;
        }
    }

    private int VisibleItemCount()
    {
        if (Bounds.Height <= 0 || _itemHeights.Count == 0) return 5;
        double avgHeight = 0;
        for (int i = 0; i < _itemHeights.Count; i++)
            avgHeight += _itemHeights[i];
        avgHeight /= _itemHeights.Count;
        return Math.Max(1, (int)(Bounds.Height / (avgHeight + LineSpacing)));
    }

    // ---- Undo / Redo ----

    private UndoSnapshot CaptureSnapshot()
    {
        var items = new List<(bool, List<ContentElement>)>();
        foreach (var item in Document.Items)
        {
            var elements = new List<ContentElement>();
            foreach (var el in item.Elements) elements.Add(el.Clone());
            items.Add((item.IsChecked, elements));
        }
        return new UndoSnapshot(items, Caret, SelectionAnchor);
    }

    private void RestoreSnapshot(UndoSnapshot snapshot)
    {
        using (SuppressSync())
        {
            Document.Items.Clear();
            foreach (var (isChecked, elements) in snapshot.Items)
            {
                var item = new TodoItem { IsChecked = isChecked };
                foreach (var el in elements) item.Elements.Add(el.Clone());
                Document.Items.Add(item);
            }
            if (Document.Items.Count == 0)
                Document.Items.Add(new TodoItem());

            Caret = ClampCursorPosition(snapshot.Caret);
            SelectionAnchor = ClampCursorPosition(snapshot.Anchor);
        }
        InvalidateMeasure();
        NotifyDocumentChanged(ChangeKind.StructureChanged);
    }

    internal void SaveUndoState(UndoActionKind action = UndoActionKind.Other)
    {
        long now = Environment.TickCount64;
        bool coalesce = action != UndoActionKind.Other
            && action == _lastUndoAction
            && (now - _lastUndoTimestamp) < CoalesceThresholdMs
            && _undoStack.Count > 0;

        _lastUndoAction = action;
        _lastUndoTimestamp = now;

        if (coalesce) { _redoStack.Clear(); return; }

        _undoStack.Add(CaptureSnapshot());
        if (_undoStack.Count > MaxUndoSteps)
            _undoStack.RemoveAt(0);
        _redoStack.Clear();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        _redoStack.Add(CaptureSnapshot());
        var snapshot = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        RestoreSnapshot(snapshot);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        _undoStack.Add(CaptureSnapshot());
        var snapshot = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        RestoreSnapshot(snapshot);
    }

    // ---- Helpers ----

    private CursorPosition ClampCursorPosition(CursorPosition pos)
    {
        int maxItem = Document.Items.Count - 1;
        int itemIdx = Math.Clamp(pos.ItemIndex, 0, maxItem);
        int offset = Math.Clamp(pos.Offset, 0, Document.Items[itemIdx].TextLength);
        return new CursorPosition(itemIdx, offset);
    }

    private void EnsureValidCaret()
    {
        EnsureNonEmpty();
        if (Caret.ItemIndex >= Document.Items.Count)
            Caret = new CursorPosition(Document.Items.Count - 1, 0);
        if (Caret.ItemIndex < 0)
            Caret = CursorPosition.Start;
    }

    private void EnsureNonEmpty()
    {
        if (Document.Items.Count == 0)
            Document.Items.Add(new TodoItem());
    }

    // ---- MVVM synchronization ----

    private void NotifyDocumentChanged(ChangeKind kind = ChangeKind.TextChanged)
    {
        if (_suppressSyncCount == 0)
        {
            _changeGeneration++;
            SyncToItems(kind);
            UpdateDirtyState();
        }
    }

    private void OnItemsPropertyChanged(IList<TodoItemData>? oldItems, IList<TodoItemData>? newItems)
    {
        UnsubscribeItems(oldItems);

        if (newItems is INotifyCollectionChanged newNcc)
            newNcc.CollectionChanged += OnItemsCollectionChanged;
        if (newItems != null)
            foreach (var item in newItems)
                item.PropertyChanged += OnItemDataPropertyChanged;

        _checkboxFocusIndex = 0;

        ResetDocumentFromItems();

        if (MoveCheckedToEnd)
            SortCheckedToEnd();

        SyncMarkdownTextFromItems();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_suppressSyncCount > 0) return;

        if (e.OldItems != null)
            foreach (TodoItemData item in e.OldItems)
                item.PropertyChanged -= OnItemDataPropertyChanged;
        if (e.NewItems != null)
            foreach (TodoItemData item in e.NewItems)
                item.PropertyChanged += OnItemDataPropertyChanged;

        ApplyItemsCollectionChange();
    }

    private void ApplyItemsCollectionChange()
    {
        LoadDocumentFromItems(isFullLoad: false);
        SyncMarkdownTextFromItems();
    }

    private void OnItemDataPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressSyncCount > 0 || sender is not TodoItemData data) return;

        var items = Items;
        if (items == null) return;

        int idx = -1;
        for (int i = 0; i < items.Count; i++)
        {
            if (ReferenceEquals(items[i], data)) { idx = i; break; }
        }
        if (idx < 0 || idx >= Document.Items.Count) return;

        using (SuppressSync())
        {
            if (e.PropertyName == nameof(TodoItemData.IsChecked))
            {
                Document.Items[idx].IsChecked = data.IsChecked;
            }
            else if (e.PropertyName == nameof(TodoItemData.Text))
            {
                var newItem = ParseTextToItem(data.Text);
                newItem.IsChecked = data.IsChecked;
                Document.Items[idx] = newItem;
            }
        }

        _changeGeneration++;
        UpdateDirtyState();
        InvalidateMeasure();
        SyncMarkdownTextFromItems();
    }

    private void ResetDocumentFromItems()
    {
        LoadDocumentFromItems(isFullLoad: true);
    }

    private void LoadDocumentFromItems(bool isFullLoad)
    {
        var items = Items;
        if (items == null) return;

        if (isFullLoad)
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        using (SuppressSync())
        {
            Document.Items.Clear();

            foreach (var data in items)
            {
                var item = ParseTextToItem(data.Text);
                item.IsChecked = data.IsChecked;
                Document.Items.Add(item);
            }

            if (Document.Items.Count == 0)
                Document.Items.Add(new TodoItem());

            Caret = ClampCursorPosition(Caret);
            SelectionAnchor = ClampCursorPosition(SelectionAnchor);
        }

        if (isFullLoad)
        {
            InvalidateMeasure();
            MarkClean();
        }
        else
        {
            _changeGeneration++;
            UpdateDirtyState();
            InvalidateMeasure();
        }
    }

    private void SyncToItems(ChangeKind kind = ChangeKind.TextChanged)
    {
        var items = Items;
        if (items == null || _suppressSyncCount > 0) return;

        using (SuppressSync())
        {
            for (int i = 0; i < Document.Items.Count; i++)
            {
                var text = SerializeItemToText(Document.Items[i]);
                var isChecked = Document.Items[i].IsChecked;

                if (i < items.Count)
                {
                    if (items[i].Text != text) items[i].Text = text;
                    if (items[i].IsChecked != isChecked) items[i].IsChecked = isChecked;
                }
                else
                {
                    var newData = new TodoItemData(text, isChecked);
                    newData.PropertyChanged += OnItemDataPropertyChanged;
                    items.Add(newData);
                }
            }

            while (items.Count > Document.Items.Count)
            {
                items[items.Count - 1].PropertyChanged -= OnItemDataPropertyChanged;
                items.RemoveAt(items.Count - 1);
            }
        }
        SyncMarkdownTextFromItems();
        ItemsChanged?.Invoke(this, EventArgs.Empty);
        ItemsDetailChanged?.Invoke(this, new TodoItemsChangedEventArgs(kind));
    }

    // ---- Images collection ----

    private void OnImagesPropertyChanged(IEnumerable<TodoImageEntry>? oldImages, IEnumerable<TodoImageEntry>? newImages)
    {
        UnsubscribeImages(oldImages);
        RebuildImageCache();
        SubscribeImages(newImages);
        RefreshAllImageElements();
    }

    private void SubscribeImages(IEnumerable<TodoImageEntry>? images)
    {
        if (images is INotifyCollectionChanged ncc)
            ncc.CollectionChanged += OnImagesCollectionChanged;
        if (images != null)
            foreach (var entry in images)
                SubscribeImageEntry(entry);
    }

    private void UnsubscribeImages(IEnumerable<TodoImageEntry>? images)
    {
        UnsubscribeAllImageEntries();
        if (images is INotifyCollectionChanged ncc)
            ncc.CollectionChanged -= OnImagesCollectionChanged;
    }

    private void SubscribeImageEntry(TodoImageEntry entry)
    {
        entry.PropertyChanged += OnImageEntryPropertyChanged;
        _subscribedImageEntries.Add(entry);
    }

    private void UnsubscribeAllImageEntries()
    {
        foreach (var entry in _subscribedImageEntries)
            entry.PropertyChanged -= OnImageEntryPropertyChanged;
        _subscribedImageEntries.Clear();
    }

    private void RebuildImageCache()
    {
        _imageCache.Clear();
        var images = Images;
        if (images == null) return;
        foreach (var entry in images)
        {
            if (entry.Bitmap != null)
                _imageCache[entry.Key] = entry.Bitmap;
        }
    }

    private void SyncLegacyImageStoreFromCache()
    {
        _legacyImageStore.Clear();
        foreach (var kvp in _imageCache)
            _legacyImageStore[kvp.Key] = kvp.Value;
    }

    private void OnImagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            UnsubscribeAllImageEntries();
            _legacyImageStore.Clear();
            if (Images != null)
                foreach (var entry in Images)
                {
                    SubscribeImageEntry(entry);
                    if (entry.Bitmap != null)
                        _legacyImageStore[entry.Key] = entry.Bitmap;
                }
        }
        else
        {
            if (e.OldItems != null)
                foreach (TodoImageEntry entry in e.OldItems)
                {
                    entry.PropertyChanged -= OnImageEntryPropertyChanged;
                    _subscribedImageEntries.Remove(entry);
                    _legacyImageStore.Remove(entry.Key);
                }
            if (e.NewItems != null)
                foreach (TodoImageEntry entry in e.NewItems)
                {
                    SubscribeImageEntry(entry);
                    if (entry.Bitmap != null)
                        _legacyImageStore[entry.Key] = entry.Bitmap;
                }
        }

        RebuildImageCache();
        RefreshAllImageElements();
    }

    private void OnImageEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TodoImageEntry entry) return;

        if (e.PropertyName == nameof(TodoImageEntry.Bitmap))
        {
            if (entry.Bitmap != null)
                _imageCache[entry.Key] = entry.Bitmap;
            else
                _imageCache.Remove(entry.Key);

            RefreshImageElementsForKey(entry.Key);
        }
        else if (e.PropertyName == nameof(TodoImageEntry.Key))
        {
            RebuildImageCache();
            SyncLegacyImageStoreFromCache();
            RefreshAllImageElements();
        }
    }

    private Bitmap? ResolveImageByKey(string key)
    {
        if (_imageCache.TryGetValue(key, out var cached)) return cached;
        if (_legacyImageStore.TryGetValue(key, out var legacy)) return legacy;
        return null;
    }

    private void RefreshAllImageElements()
    {
        bool changed = false;
        using (SuppressSync())
        {
            for (int i = 0; i < Document.Items.Count; i++)
                changed |= RefreshItemImages(Document.Items[i]);
        }
        if (changed) InvalidateMeasure();
    }

    private void RefreshImageElementsForKey(string key)
    {
        bool changed = false;
        using (SuppressSync())
        {
            for (int i = 0; i < Document.Items.Count; i++)
                changed |= RefreshItemImages(Document.Items[i], key);
        }
        if (changed) InvalidateMeasure();
    }

    private bool RefreshItemImages(TodoItem item, string? filterKey = null)
    {
        bool changed = false;
        for (int j = 0; j < item.Elements.Count; j++)
        {
            var el = item.Elements[j];
            if (el.UnresolvedImageKey != null && (filterKey == null || el.UnresolvedImageKey == filterKey))
            {
                var bitmap = ResolveImageByKey(el.UnresolvedImageKey);
                if (bitmap != null)
                {
                    var imgEl = ContentElement.CreateImage(bitmap);
                    imgEl.ImageKey = el.UnresolvedImageKey;
                    imgEl.ImageAltText = el.ImageAltText;
                    item.Elements[j] = imgEl;
                    changed = true;
                }
            }
            else if (el.Type == ContentElementType.Image && el.ImageKey != null
                     && (filterKey == null || el.ImageKey == filterKey))
            {
                var bitmap = ResolveImageByKey(el.ImageKey);
                if (bitmap != null && !ReferenceEquals(el.Image, bitmap))
                {
                    el.Image = bitmap;
                    el.ImageWidth = bitmap.PixelSize.Width;
                    el.ImageHeight = bitmap.PixelSize.Height;
                    changed = true;
                }
            }
        }
        return changed;
    }

    // ---- IsDirty ----

    private void SetDirtyState(bool dirty)
    {
        if (SetAndRaise(IsDirtyProperty, ref _isDirty, dirty))
            DirtyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateDirtyState()
    {
        SetDirtyState(_changeGeneration != _cleanGeneration);
    }

    public void MarkClean()
    {
        _cleanGeneration = _changeGeneration;
        SetDirtyState(false);
    }

    // ---- Text parsing ----

    public TodoItem ParseTextToItem(string text)
    {
        var item = new TodoItem();
        int lastEnd = 0;

        foreach (Match match in ImagePattern.Matches(text))
        {
            if (match.Index > lastEnd)
                item.Elements.Add(ContentElement.CreateText(text[lastEnd..match.Index]));

            string altText = match.Groups[1].Value;
            string key = match.Groups[2].Value;

            var bitmap = ResolveImageByKey(key);
            if (bitmap != null)
            {
                var imgEl = ContentElement.CreateImage(bitmap);
                imgEl.ImageKey = key;
                imgEl.ImageAltText = altText;
                item.Elements.Add(imgEl);
            }
            else
            {
                var placeholder = ContentElement.CreateText(match.Value);
                placeholder.UnresolvedImageKey = key;
                placeholder.ImageAltText = altText;
                item.Elements.Add(placeholder);
            }

            lastEnd = match.Index + match.Length;
        }

        if (lastEnd < text.Length)
            item.Elements.Add(ContentElement.CreateText(text[lastEnd..]));

        return item;
    }

    internal string SerializeItemToText(TodoItem item)
    {
        var sb = new StringBuilder();
        foreach (var el in item.Elements)
        {
            if (el.Type == ContentElementType.Image)
            {
                string key = el.ImageKey ?? GetOrCreateImageKey(el.Image);
                string alt = el.ImageAltText ?? "image";
                sb.Append($"![{alt}]({key})");
            }
            else if (el.UnresolvedImageKey != null)
            {
                string alt = el.ImageAltText ?? "image";
                sb.Append($"![{alt}]({el.UnresolvedImageKey})");
            }
            else
            {
                sb.Append(el.Text);
            }
        }
        return sb.ToString();
    }

    private string GetOrCreateImageKey(Bitmap? bitmap)
    {
        if (bitmap == null) return "unknown";

        foreach (var kvp in _imageCache)
        {
            if (ReferenceEquals(kvp.Value, bitmap)) return kvp.Key;
        }

        foreach (var kvp in _legacyImageStore)
        {
            if (ReferenceEquals(kvp.Value, bitmap)) return kvp.Key;
        }

        string key = $"img_{Guid.NewGuid():N}";
        _imageCache[key] = bitmap;
        return key;
    }

    // ---- Public API ----

    public string GetText() => Document.GetAllText();

    public void SetText(string text)
    {
        _undoStack.Clear();
        _redoStack.Clear();

        using (SuppressSync())
        {
            Document.Items.Clear();
            foreach (var line in text.Split('\n'))
                Document.Items.Add(new TodoItem(line.TrimEnd('\r')));
            if (Document.Items.Count == 0)
                Document.Items.Add(new TodoItem());
            Caret = CursorPosition.Start;
            SelectionAnchor = CursorPosition.Start;
        }
        InvalidateMeasure();
        NotifyDocumentChanged(ChangeKind.StructureChanged);
    }

    public void AddItem(string text, bool isChecked = false)
    {
        Document.Items.Add(new TodoItem(text, isChecked));
        InvalidateMeasure();
        NotifyDocumentChanged(ChangeKind.StructureChanged);
    }

    public void ToggleItem(int index)
    {
        if (index >= 0 && index < Document.Items.Count)
            ToggleItemCore(index);
    }

    private void ToggleItemCore(int index)
    {
        var item = Document.Items[index];
        item.IsChecked = !item.IsChecked;

        if (MoveCheckedToEnd)
        {
            int newIndex = index;

            if (item.IsChecked && index < Document.Items.Count - 1)
            {
                int lastUnchecked = Document.Items.Count - 1;
                while (lastUnchecked > index && Document.Items[lastUnchecked].IsChecked)
                    lastUnchecked--;

                if (lastUnchecked > index)
                    newIndex = lastUnchecked;
            }
            else if (!item.IsChecked)
            {
                int firstChecked = 0;
                while (firstChecked < Document.Items.Count && !Document.Items[firstChecked].IsChecked)
                    firstChecked++;

                if (firstChecked < index)
                    newIndex = firstChecked;
            }

            if (newIndex != index)
            {
                Document.Items.RemoveAt(index);
                Document.Items.Insert(newIndex, item);

                var mvvmItems = Items;
                if (mvvmItems != null && index < mvvmItems.Count)
                {
                    using (SuppressSync())
                    {
                        var mvvmItem = mvvmItems[index];
                        mvvmItems.RemoveAt(index);
                        mvvmItems.Insert(Math.Min(newIndex, mvvmItems.Count), mvvmItem);
                    }
                }

                if (Caret.ItemIndex == index)
                    Caret = new CursorPosition(newIndex, Caret.Offset);
                else if (index < newIndex && Caret.ItemIndex > index && Caret.ItemIndex <= newIndex)
                    Caret = new CursorPosition(Caret.ItemIndex - 1, Caret.Offset);
                else if (index > newIndex && Caret.ItemIndex >= newIndex && Caret.ItemIndex < index)
                    Caret = new CursorPosition(Caret.ItemIndex + 1, Caret.Offset);

                SelectionAnchor = Caret;

                if (_checkboxFocused)
                    _checkboxFocusIndex = Math.Clamp(_checkboxFocusIndex, 0, Document.Items.Count - 1);
            }
        }

        InvalidateMeasure();
        NotifyDocumentChanged(ChangeKind.CheckedChanged);
    }

    private void SortCheckedToEnd()
    {
        var items = Items;
        if (items == null || items.Count <= 1) return;

        var uncheckedItems = new List<TodoItemData>();
        var checkedItems = new List<TodoItemData>();
        foreach (var item in items)
            (item.IsChecked ? checkedItems : uncheckedItems).Add(item);

        if (checkedItems.Count == 0 || uncheckedItems.Count == 0) return;

        using (SuppressSync())
        {
            items.Clear();
            foreach (var item in uncheckedItems) items.Add(item);
            foreach (var item in checkedItems) items.Add(item);
        }

        ResetDocumentFromItems();
        SyncMarkdownTextFromItems();
    }

    public IReadOnlyList<TodoItem> GetCheckedItems() =>
        Document.Items.Where(i => i.IsChecked).ToList();

    public IReadOnlyList<TodoItem> GetUncheckedItems() =>
        Document.Items.Where(i => !i.IsChecked).ToList();
}
