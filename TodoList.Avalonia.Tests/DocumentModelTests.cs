using global::Avalonia.Headless.NUnit;
using NUnit.Framework;
using TodoList.Avalonia.Model;

namespace TodoList.Avalonia.Tests;

[TestFixture]
public class DocumentModelTests
{
    [Test]
    public void NewDocumentHasOneItem()
    {
        var doc = new TodoDocument();
        Assert.That(doc.Items.Count, Is.EqualTo(1));
    }

    [Test]
    public void GetAllTextReturnsEmptyForNewDocument()
    {
        var doc = new TodoDocument();
        Assert.That(doc.GetAllText(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void TodoItemPlainText()
    {
        var item = new TodoItem("Hello World");
        Assert.That(item.PlainText, Is.EqualTo("Hello World"));
    }

    [Test]
    public void TodoItemTextLength()
    {
        var item = new TodoItem("ABC");
        Assert.That(item.TextLength, Is.EqualTo(3));
    }

    [Test]
    public void TodoItemEmptyTextLength()
    {
        var item = new TodoItem();
        Assert.That(item.TextLength, Is.EqualTo(0));
    }

    [Test]
    public void TodoItemIsCheckedDefault()
    {
        var item = new TodoItem("task");
        Assert.That(item.IsChecked, Is.False);
    }

    [Test]
    public void TodoItemIsCheckedSet()
    {
        var item = new TodoItem("task", true);
        Assert.That(item.IsChecked, Is.True);
    }

    [Test]
    public void ContentElementCreateText()
    {
        var el = ContentElement.CreateText("hello");
        Assert.That(el.Type, Is.EqualTo(ContentElementType.Text));
        Assert.That(el.Text, Is.EqualTo("hello"));
    }

    [Test]
    public void ContentElementClone()
    {
        var el = ContentElement.CreateText("test");
        el.Bold = true;
        var clone = el.Clone();
        Assert.That(clone.Text, Is.EqualTo("test"));
        Assert.That(clone.Bold, Is.True);
        clone.Text = "changed";
        Assert.That(el.Text, Is.EqualTo("test"));
    }

    [Test]
    public void ResolveOffsetSimpleText()
    {
        var item = new TodoItem("ABCDE");
        var (elIdx, localOff) = item.ResolveOffset(3);
        Assert.That(elIdx, Is.EqualTo(0));
        Assert.That(localOff, Is.EqualTo(3));
    }

    [Test]
    public void ResolveOffsetZero()
    {
        var item = new TodoItem("Hello");
        var (elIdx, localOff) = item.ResolveOffset(0);
        Assert.That(elIdx, Is.EqualTo(0));
        Assert.That(localOff, Is.EqualTo(0));
    }

    [Test]
    public void ResolveOffsetEnd()
    {
        var item = new TodoItem("ABC");
        var (elIdx, localOff) = item.ResolveOffset(3);
        Assert.That(elIdx, Is.EqualTo(0));
        Assert.That(localOff, Is.EqualTo(3));
    }

    [Test]
    public void GlobalOffsetRoundTrip()
    {
        var item = new TodoItem("ABCDE");
        var (elIdx, localOff) = item.ResolveOffset(3);
        int global = item.GlobalOffset(elIdx, localOff);
        Assert.That(global, Is.EqualTo(3));
    }

    [Test]
    public void DocumentMultipleItems()
    {
        var doc = new TodoDocument();
        doc.Items.Clear();
        doc.Items.Add(new TodoItem("Line 1"));
        doc.Items.Add(new TodoItem("Line 2"));
        doc.Items.Add(new TodoItem("Line 3"));

        var text = doc.GetAllText();
        Assert.That(text, Does.Contain("Line 1"));
        Assert.That(text, Does.Contain("Line 2"));
        Assert.That(text, Does.Contain("Line 3"));
    }

    [Test]
    public void CursorPositionStart()
    {
        var pos = CursorPosition.Start;
        Assert.That(pos.ItemIndex, Is.EqualTo(0));
        Assert.That(pos.Offset, Is.EqualTo(0));
    }

    [Test]
    public void SelectionRangeIsEmpty()
    {
        var sel = new SelectionRange(CursorPosition.Start, CursorPosition.Start);
        Assert.That(sel.IsEmpty, Is.True);
    }

    [Test]
    public void SelectionRangeNotEmpty()
    {
        var sel = new SelectionRange(
            new CursorPosition(0, 0),
            new CursorPosition(0, 5));
        Assert.That(sel.IsEmpty, Is.False);
    }

    [Test]
    public void SelectionRangeOrdered()
    {
        var sel = new SelectionRange(
            new CursorPosition(1, 3),
            new CursorPosition(0, 1));
        var (first, last) = sel.Ordered();
        Assert.That(first.ItemIndex, Is.EqualTo(0));
        Assert.That(last.ItemIndex, Is.EqualTo(1));
    }

    [Test]
    public void DefaultFontSettings()
    {
        var doc = new TodoDocument();
        Assert.That(doc.DefaultFontSize, Is.EqualTo(14));
    }

    [AvaloniaTest]
    public void TodoItemWithMixedContent()
    {
        var item = new TodoItem();
        item.Elements.Add(ContentElement.CreateText("Hello "));
        var bmp = CreateTestBitmap();
        item.Elements.Add(ContentElement.CreateImage(bmp));
        item.Elements.Add(ContentElement.CreateText(" World"));

        Assert.That(item.TextLength, Is.EqualTo(13)); // 6 + 1 + 6
        Assert.That(item.PlainText, Does.Contain("Hello"));
        Assert.That(item.PlainText, Does.Contain("World"));
    }

    [AvaloniaTest]
    public void ResolveOffsetWithImage()
    {
        var item = new TodoItem();
        item.Elements.Add(ContentElement.CreateText("AB"));
        item.Elements.Add(ContentElement.CreateImage(CreateTestBitmap()));
        item.Elements.Add(ContentElement.CreateText("CD"));

        // offset 3 = past "AB" (2) + image (1) = start of "CD"
        var (elIdx, localOff) = item.ResolveOffset(3);
        Assert.That(elIdx, Is.EqualTo(2));
        Assert.That(localOff, Is.EqualTo(0));
    }

    private static global::Avalonia.Media.Imaging.Bitmap CreateTestBitmap()
    {
        return new global::Avalonia.Media.Imaging.WriteableBitmap(
            new global::Avalonia.PixelSize(10, 10),
            new global::Avalonia.Vector(96, 96),
            global::Avalonia.Platform.PixelFormat.Bgra8888,
            global::Avalonia.Platform.AlphaFormat.Premul);
    }
}
