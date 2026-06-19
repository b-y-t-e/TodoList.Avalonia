using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using TodoListControl.Model;

namespace TodoListControl.Controls;

public class TodoListEditor : Control
{
    public static readonly StyledProperty<FontFamily> DefaultFontProperty =
        AvaloniaProperty.Register<TodoListEditor, FontFamily>(nameof(DefaultFont), FontFamily.Default);

    public static readonly StyledProperty<double> DefaultFontSizeProperty =
        AvaloniaProperty.Register<TodoListEditor, double>(nameof(DefaultFontSize), 14.0);

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

    public TodoDocument Document { get; } = new();
    public CursorPosition Caret { get; set; } = CursorPosition.Start;
    public CursorPosition SelectionAnchor { get; set; } = CursorPosition.Start;
    public bool HasSelection => !CurrentSelection.IsEmpty;

    private bool _mouseSelecting;

    private const double PaddingLeft = 32;
    private const double PaddingTop = 8;
    private const double PaddingRight = 8;
    private const double CheckboxSize = 16;
    private const double CheckboxMarginRight = 8;
    private const double LineSpacing = 6;
    private const double InlineImageMaxHeight = 48;
    private const double WrapLineSpacing = 2;

    private readonly List<double> _itemYPositions = new();
    private readonly List<double> _itemHeights = new();
    private readonly List<List<WrappedLine>> _itemWrapping = new();
    private double _desiredHeight;
    private double _desiredWidth;

    private static readonly SolidColorBrush SelectionBrush =
        new(Color.FromArgb(80, 30, 144, 255));

    private List<List<ContentElement>>? _internalClipboard;
    private string? _internalClipboardText;

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DefaultFontProperty || change.Property == DefaultFontSizeProperty)
        {
            SyncDocumentDefaults();
            InvalidateMeasure();
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

        double availWidth = Math.Max((_desiredWidth > 0 ? _desiredWidth : 400) - PaddingLeft - PaddingRight, 50);
        double y = PaddingTop;

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

        _desiredHeight = y + PaddingTop;
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
        context.FillRectangle(Brushes.White, new Rect(Bounds.Size));

        if (_itemWrapping.Count != Document.Items.Count)
            ComputeLayout();

        var (selFirst, selLast) = CurrentSelection.Ordered();
        var selBrush = SelectionBrush;

        for (int i = 0; i < Document.Items.Count; i++)
        {
            var item = Document.Items[i];
            double itemY = _itemYPositions[i];
            var wrappedLines = _itemWrapping[i];

            DrawCheckbox(context, 8, itemY + 1, item.IsChecked);

            if (item.Elements.Count == 0)
            {
                if (i == Caret.ItemIndex && !HasSelection)
                    context.FillRectangle(Brushes.Black,
                        new Rect(PaddingLeft, itemY, 1.5, Document.DefaultFontSize + 4));
                continue;
            }

            foreach (var wLine in wrappedLines)
            {
                double lineY = itemY + wLine.YOffset;

                foreach (var seg in wLine.Segments)
                {
                    var el = item.Elements[seg.ElementIndex];
                    double segX = PaddingLeft + seg.X;
                    int segGlobalStart = item.GlobalOffset(seg.ElementIndex, seg.LocalStart);

                    if (el.Type == ContentElementType.Image && el.Image != null)
                    {
                        bool inSel = IsOffsetInSelection(i, segGlobalStart, selFirst, selLast);
                        if (inSel)
                            context.FillRectangle(selBrush,
                                new Rect(segX, lineY, seg.Width - 2, seg.Height));

                        context.DrawImage(el.Image, new Rect(segX, lineY, seg.Width - 2, seg.Height));
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
                            context.FillRectangle(selBrush, new Rect(segX + sX, lineY, sW, seg.Height));
                        }

                        var fmt = new FormattedText(segText,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight, typeface, fs,
                            item.IsChecked ? Brushes.Gray : Brushes.Black);

                        if (item.IsChecked)
                        {
                            double textY = lineY + fmt.Height / 2;
                            context.DrawLine(new Pen(Brushes.Gray, 1),
                                new Point(segX, textY), new Point(segX + fmt.Width, textY));
                        }

                        context.DrawText(fmt, new Point(segX, lineY));
                    }
                }
            }

            if (i == Caret.ItemIndex && !HasSelection)
            {
                var (caretX, caretYOff, caretLineH) = CalculateCaretPosition(i, Caret.Offset);
                context.FillRectangle(Brushes.Black,
                    new Rect(PaddingLeft + caretX, itemY + caretYOff, 1.5, caretLineH));
            }
        }
    }

    private void DrawCheckbox(DrawingContext ctx, double x, double y, bool isChecked)
    {
        var rect = new Rect(x, y, CheckboxSize, CheckboxSize);
        ctx.FillRectangle(isChecked ? Brushes.DodgerBlue : Brushes.White, rect);
        ctx.DrawRectangle(new Pen(Brushes.Gray, 1.5), rect);

        if (isChecked)
        {
            var pen = new Pen(Brushes.White, 2);
            ctx.DrawLine(pen, new Point(x + 3, y + 8), new Point(x + 6, y + 12));
            ctx.DrawLine(pen, new Point(x + 6, y + 12), new Point(x + 13, y + 4));
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
        if (itemIndex >= _itemWrapping.Count)
            return (0, 0, Document.DefaultFontSize + 4);

        var wrappedLines = _itemWrapping[itemIndex];
        var item = Document.Items[itemIndex];

        for (int li = 0; li < wrappedLines.Count; li++)
        {
            var wLine = wrappedLines[li];
            bool isLastLine = li == wrappedLines.Count - 1;

            if (globalOffset < wLine.EndGlobalOffset
                || (isLastLine && globalOffset <= wLine.EndGlobalOffset))
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

                        return (x, wLine.YOffset, wLine.Height);
                    }
                }

                double endX = 0;
                if (wLine.Segments.Count > 0)
                {
                    var lastSeg = wLine.Segments[^1];
                    endX = lastSeg.X + lastSeg.Width;
                }
                return (endX, wLine.YOffset, wLine.Height);
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
            return (x, lastLine.YOffset, lastLine.Height);
        }

        return (0, 0, Document.DefaultFontSize + 4);
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
                    double cx = seg.X;
                    for (int ci = 0; ci < segText.Length; ci++)
                    {
                        double cw = MeasureTextWidth(segText[ci].ToString(), typeface, fs);
                        if (targetX < cx + cw / 2) return segGlobalStart + ci;
                        cx += cw;
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
            FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
        return fmt.Width;
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

                if (lineX > 0 && lineX + imgW > availWidth)
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

        int lastSpace = -1;
        double currentWidth = 0;
        for (int i = start; i < text.Length; i++)
        {
            currentWidth += MeasureTextWidth(text[i].ToString(), typeface, fontSize);
            if (currentWidth > availWidth)
            {
                if (lastSpace > start) return lastSpace + 1;
                return i > start ? i : start;
            }
            if (text[i] == ' ') lastSpace = i;
        }
        return text.Length;
    }

    // ---- Mouse input ----

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var pos = e.GetPosition(this);

        if (pos.X < PaddingLeft - 4)
        {
            int itemIdx = HitTestItem(pos.Y);
            if (itemIdx >= 0 && itemIdx < Document.Items.Count)
            {
                Document.Items[itemIdx].IsChecked = !Document.Items[itemIdx].IsChecked;
                InvalidateMeasure();
            }
            return;
        }

        var cursor = HitTestCursor(pos);
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
        double relX = pos.X - PaddingLeft;
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

        DeleteSelection();
        InsertTextAtCaret(e.Text);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        switch (e.Key)
        {
            case Key.Enter:
                DeleteSelection();
                SplitItemAtCaret();
                e.Handled = true;
                break;

            case Key.Back:
                if (HasSelection)
                    DeleteSelection();
                else
                    HandleBackspace();
                e.Handled = true;
                break;

            case Key.Delete:
                if (HasSelection)
                    DeleteSelection();
                else
                    HandleDelete();
                e.Handled = true;
                break;

            case Key.Left:
                MoveCaret(-1, shift);
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

            case Key.Home:
                var home = new CursorPosition(Caret.ItemIndex, 0);
                if (!shift) SelectionAnchor = home;
                Caret = home;
                InvalidateMeasure();
                e.Handled = true;
                break;

            case Key.End:
                var end = new CursorPosition(Caret.ItemIndex, CurrentItem.TextLength);
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
                CopyToClipboard();
                DeleteSelection();
                e.Handled = true;
                break;

            case Key.V when ctrl:
                _ = PasteFromClipboard();
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
            item.Elements.Add(ContentElement.CreateText(text, Document.DefaultFont, Document.DefaultFontSize));
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
                var newEl = ContentElement.CreateText(text, Document.DefaultFont, Document.DefaultFontSize);
                item.Elements.Insert(elIdx + (localOff > 0 ? 1 : 0), newEl);
                Caret = new CursorPosition(Caret.ItemIndex, offset + text.Length);
            }
        }

        ClearSelectionNoDelete();
        InvalidateMeasure();
    }

    public void InsertImageAtCaret(Bitmap bitmap)
    {
        EnsureValidCaret();
        DeleteSelection();
        var item = Document.Items[Caret.ItemIndex];
        int offset = Math.Min(Caret.Offset, item.TextLength);

        var imgEl = ContentElement.CreateImage(bitmap);

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

        InvalidateMeasure();
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
    }

    private void DeleteRange(TodoItem item, int fromOffset, int toOffset)
    {
        if (fromOffset >= toOffset) return;

        int pos = 0;
        for (int i = 0; i < item.Elements.Count; i++)
        {
            var el = item.Elements[i];
            int elLen = el.Type == ContentElementType.Text ? el.Text.Length : 1;
            int elStart = pos;
            int elEnd = pos + elLen;

            if (elEnd <= fromOffset) { pos += elLen; continue; }
            if (elStart >= toOffset) break;

            int delStart = Math.Max(fromOffset - elStart, 0);
            int delEnd = Math.Min(toOffset - elStart, elLen);

            if (el.Type == ContentElementType.Text)
            {
                el.Text = el.Text[..delStart] + el.Text[delEnd..];
                if (el.Text.Length == 0) { item.Elements.RemoveAt(i); i--; }
            }
            else
            {
                item.Elements.RemoveAt(i); i--;
            }

            pos = elStart;
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

        var sysText = await clipboard.GetTextAsync();

        if (_internalClipboard != null && _internalClipboardText != null
            && sysText == _internalClipboardText)
        {
            PasteRichContent(_internalClipboard);
            return;
        }

        var formats = await clipboard.GetFormatsAsync();

        string[] imageFormats = ["PNG", "image/png", "Bitmap", "image/bmp",
            "DeviceIndependentBitmap", "CF_DIB", "CF_DIBV5"];

        foreach (var imgFmt in imageFormats)
        {
            if (!formats.Contains(imgFmt)) continue;

            var data = await clipboard.GetDataAsync(imgFmt);
            if (TryLoadBitmap(data, out var bmp))
            {
                InsertImageAtCaret(bmp!);
                return;
            }
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
        DeleteSelection();
        for (int i = 0; i < lines.Count; i++)
        {
            foreach (var el in lines[i])
            {
                if (el.Type == ContentElementType.Image && el.Image != null)
                    InsertImageAtCaret(el.Image);
                else if (el.Type == ContentElementType.Text && el.Text.Length > 0)
                    InsertTextAtCaret(el.Text);
            }
            if (i < lines.Count - 1)
                SplitItemAtCaret();
        }
    }

    private static bool TryLoadBitmap(object? data, out Bitmap? bitmap)
    {
        bitmap = null;
        try
        {
            switch (data)
            {
                case byte[] bytes:
                    bitmap = new Bitmap(new MemoryStream(bytes));
                    return true;
                case Stream stream:
                    bitmap = new Bitmap(stream);
                    return true;
                case Bitmap bmp:
                    bitmap = bmp;
                    return true;
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    public void PasteMultilineText(string text)
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

    internal void MoveCaretVertical(int direction, bool extend)
    {
        EnsureValidCaret();
        int itemIdx = Caret.ItemIndex;

        if (itemIdx < _itemWrapping.Count)
        {
            var wrappedLines = _itemWrapping[itemIdx];

            int currentLineIdx = 0;
            for (int li = 0; li < wrappedLines.Count; li++)
            {
                if (Caret.Offset < wrappedLines[li].EndGlobalOffset
                    || li == wrappedLines.Count - 1)
                {
                    currentLineIdx = li;
                    break;
                }
            }

            int targetLineIdx = currentLineIdx + direction;

            if (targetLineIdx >= 0 && targetLineIdx < wrappedLines.Count)
            {
                var (caretX, _, _) = CalculateCaretPosition(itemIdx, Caret.Offset);
                int newOffset = HitTestLineOffset(
                    Document.Items[itemIdx], wrappedLines[targetLineIdx], caretX);
                Caret = new CursorPosition(itemIdx, newOffset);
                if (!extend) SelectionAnchor = Caret;
                InvalidateMeasure();
                return;
            }

            if (direction < 0 && itemIdx > 0)
            {
                var (caretX, _, _) = CalculateCaretPosition(itemIdx, Caret.Offset);
                int prevIdx = itemIdx - 1;
                if (prevIdx < _itemWrapping.Count && _itemWrapping[prevIdx].Count > 0)
                {
                    int newOffset = HitTestLineOffset(
                        Document.Items[prevIdx], _itemWrapping[prevIdx][^1], caretX);
                    Caret = new CursorPosition(prevIdx, newOffset);
                }
                else
                {
                    Caret = new CursorPosition(prevIdx, Document.Items[prevIdx].TextLength);
                }
            }
            else if (direction > 0 && itemIdx < Document.Items.Count - 1)
            {
                var (caretX, _, _) = CalculateCaretPosition(itemIdx, Caret.Offset);
                int nextIdx = itemIdx + 1;
                if (nextIdx < _itemWrapping.Count && _itemWrapping[nextIdx].Count > 0)
                {
                    int newOffset = HitTestLineOffset(
                        Document.Items[nextIdx], _itemWrapping[nextIdx][0], caretX);
                    Caret = new CursorPosition(nextIdx, newOffset);
                }
                else
                {
                    Caret = new CursorPosition(nextIdx, 0);
                }
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

    // ---- Helpers ----

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

    // ---- Public API ----

    public string GetText() => Document.GetAllText();

    public void SetText(string text)
    {
        Document.Items.Clear();
        foreach (var line in text.Split('\n'))
            Document.Items.Add(new TodoItem(line.TrimEnd('\r')));
        if (Document.Items.Count == 0)
            Document.Items.Add(new TodoItem());
        Caret = CursorPosition.Start;
        SelectionAnchor = CursorPosition.Start;
        InvalidateMeasure();
    }

    public void AddItem(string text, bool isChecked = false)
    {
        Document.Items.Add(new TodoItem(text, isChecked));
        InvalidateMeasure();
    }

    public void ToggleItem(int index)
    {
        if (index >= 0 && index < Document.Items.Count)
        {
            Document.Items[index].IsChecked = !Document.Items[index].IsChecked;
            InvalidateMeasure();
        }
    }

    public IReadOnlyList<TodoItem> GetCheckedItems() =>
        Document.Items.Where(i => i.IsChecked).ToList();

    public IReadOnlyList<TodoItem> GetUncheckedItems() =>
        Document.Items.Where(i => !i.IsChecked).ToList();
}
