using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using NUnit.Framework;
using TodoListControl.Controls;
using TodoListControl.Model;

namespace TodoListControl.Tests;

[TestFixture]
public class TodoListEditorTests
{
    [AvaloniaTest]
    public void NewEditorHasOneEmptyItem()
    {
        var editor = new TodoListEditor();
        Assert.That(editor.Document.Items.Count, Is.EqualTo(1));
        Assert.That(editor.GetText(), Is.EqualTo(string.Empty));
    }

    [AvaloniaTest]
    public void InsertTextAtCaret()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("Hello");
        Assert.That(editor.GetText(), Is.EqualTo("Hello"));
    }

    [AvaloniaTest]
    public void InsertMultipleTexts()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("Hello");
        editor.InsertTextAtCaret(" World");
        Assert.That(editor.GetText(), Is.EqualTo("Hello World"));
    }

    [AvaloniaTest]
    public void SplitItemCreatesNewTodoItem()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("Line1");
        editor.SplitItemAtCaret();
        editor.InsertTextAtCaret("Line2");

        Assert.That(editor.Document.Items.Count, Is.EqualTo(2));
        Assert.That(editor.Document.Items[0].PlainText, Is.EqualTo("Line1"));
        Assert.That(editor.Document.Items[1].PlainText, Is.EqualTo("Line2"));
    }

    [AvaloniaTest]
    public void SplitInMiddleOfText()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("HelloWorld");
        editor.Caret = new CursorPosition(0, 5);
        editor.SelectionAnchor = editor.Caret;
        editor.SplitItemAtCaret();

        Assert.That(editor.Document.Items.Count, Is.EqualTo(2));
        Assert.That(editor.Document.Items[0].PlainText, Is.EqualTo("Hello"));
        Assert.That(editor.Document.Items[1].PlainText, Is.EqualTo("World"));
    }

    [AvaloniaTest]
    public void SetTextCreatesMultipleItems()
    {
        var editor = new TodoListEditor();
        editor.SetText("Item 1\nItem 2\nItem 3");

        Assert.That(editor.Document.Items.Count, Is.EqualTo(3));
        Assert.That(editor.Document.Items[0].PlainText, Is.EqualTo("Item 1"));
        Assert.That(editor.Document.Items[1].PlainText, Is.EqualTo("Item 2"));
        Assert.That(editor.Document.Items[2].PlainText, Is.EqualTo("Item 3"));
    }

    [AvaloniaTest]
    public void SetTextWithCRLF()
    {
        var editor = new TodoListEditor();
        editor.SetText("a\r\nb\r\nc");
        Assert.That(editor.Document.Items.Count, Is.EqualTo(3));
    }

    [AvaloniaTest]
    public void PasteMultilineTextCreatesNewItems()
    {
        var editor = new TodoListEditor();
        editor.PasteMultilineText("Buy milk\nWalk the dog\nCode review");

        Assert.That(editor.Document.Items.Count, Is.EqualTo(3));
        Assert.That(editor.Document.Items[0].PlainText, Is.EqualTo("Buy milk"));
        Assert.That(editor.Document.Items[1].PlainText, Is.EqualTo("Walk the dog"));
        Assert.That(editor.Document.Items[2].PlainText, Is.EqualTo("Code review"));
    }

    [AvaloniaTest]
    public void PasteMultilineIntoExistingItems()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("Existing");
        editor.SplitItemAtCaret();
        editor.InsertTextAtCaret("Also existing");

        editor.Caret = new CursorPosition(0, 8);
        editor.SelectionAnchor = editor.Caret;
        editor.PasteMultilineText("\nNew item 1\nNew item 2");

        Assert.That(editor.Document.Items.Count, Is.EqualTo(4));
        Assert.That(editor.Document.Items[0].PlainText, Is.EqualTo("Existing"));
        Assert.That(editor.Document.Items[1].PlainText, Is.EqualTo("New item 1"));
    }

    [AvaloniaTest]
    public void InsertImageAtCaret()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("before");
        editor.InsertImageAtCaret(CreateTestBitmap());

        var item = editor.Document.Items[0];
        Assert.That(item.Elements.Count, Is.EqualTo(2));
        Assert.That(item.Elements[0].Type, Is.EqualTo(ContentElementType.Text));
        Assert.That(item.Elements[1].Type, Is.EqualTo(ContentElementType.Image));
    }

    [AvaloniaTest]
    public void InsertImageInMiddleOfText()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("ABCD");
        editor.Caret = new CursorPosition(0, 2);
        editor.SelectionAnchor = editor.Caret;
        editor.InsertImageAtCaret(CreateTestBitmap());

        var item = editor.Document.Items[0];
        Assert.That(item.Elements.Count, Is.EqualTo(3));
        Assert.That(item.Elements[0].Text, Is.EqualTo("AB"));
        Assert.That(item.Elements[1].Type, Is.EqualTo(ContentElementType.Image));
        Assert.That(item.Elements[2].Text, Is.EqualTo("CD"));
    }

    [AvaloniaTest]
    public void TextAndImageInSameLine()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("Photo: ");
        editor.InsertImageAtCaret(CreateTestBitmap());
        editor.InsertTextAtCaret(" caption");

        var item = editor.Document.Items[0];
        Assert.That(item.Elements.Count, Is.EqualTo(3));
        Assert.That(item.PlainText, Does.Contain("Photo:"));
        Assert.That(item.PlainText, Does.Contain("caption"));
        Assert.That(item.TextLength, Is.EqualTo(16)); // "Photo: " (7) + image (1) + " caption" (8)
    }

    [AvaloniaTest]
    public void CheckUncheckItem()
    {
        var editor = new TodoListEditor();
        editor.AddItem("Task 1");
        editor.AddItem("Task 2");

        editor.ToggleItem(1);
        Assert.That(editor.Document.Items[1].IsChecked, Is.True);

        editor.ToggleItem(1);
        Assert.That(editor.Document.Items[1].IsChecked, Is.False);
    }

    [AvaloniaTest]
    public void GetCheckedItems()
    {
        var editor = new TodoListEditor();
        editor.Document.Items.Clear();
        editor.Document.Items.Add(new TodoItem("A", true));
        editor.Document.Items.Add(new TodoItem("B", false));
        editor.Document.Items.Add(new TodoItem("C", true));

        var checked_ = editor.GetCheckedItems();
        Assert.That(checked_.Count, Is.EqualTo(2));
        Assert.That(checked_[0].PlainText, Is.EqualTo("A"));
        Assert.That(checked_[1].PlainText, Is.EqualTo("C"));
    }

    [AvaloniaTest]
    public void GetUncheckedItems()
    {
        var editor = new TodoListEditor();
        editor.Document.Items.Clear();
        editor.Document.Items.Add(new TodoItem("A", true));
        editor.Document.Items.Add(new TodoItem("B", false));

        var unchecked_ = editor.GetUncheckedItems();
        Assert.That(unchecked_.Count, Is.EqualTo(1));
        Assert.That(unchecked_[0].PlainText, Is.EqualTo("B"));
    }

    // ---- Selection tests ----

    [AvaloniaTest]
    public void SelectionWithinSingleItem()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("Hello World");

        editor.SelectionAnchor = new CursorPosition(0, 0);
        editor.Caret = new CursorPosition(0, 5);

        Assert.That(editor.HasSelection, Is.True);
        Assert.That(editor.GetSelectedText(), Is.EqualTo("Hello"));
    }

    [AvaloniaTest]
    public void SelectionAcrossMultipleItems()
    {
        var editor = new TodoListEditor();
        editor.SetText("First line\nSecond line\nThird line");

        editor.SelectionAnchor = new CursorPosition(0, 6);
        editor.Caret = new CursorPosition(2, 5);

        var selected = editor.GetSelectedText();
        Assert.That(selected, Does.Contain("line"));
        Assert.That(selected, Does.Contain("Second line"));
        Assert.That(selected, Does.Contain("Third"));
    }

    [AvaloniaTest]
    public void SelectionIncludingImages()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("AB");
        editor.InsertImageAtCaret(CreateTestBitmap());
        editor.InsertTextAtCaret("CD");

        editor.SelectionAnchor = new CursorPosition(0, 0);
        editor.Caret = new CursorPosition(0, 5); // AB + img + CD

        var text = editor.GetSelectedText();
        Assert.That(text.Length, Is.EqualTo(5)); // A B ￼ C D
    }

    [AvaloniaTest]
    public void SelectAllCoverEverything()
    {
        var editor = new TodoListEditor();
        editor.SetText("One\nTwo\nThree");
        editor.SelectAll();

        Assert.That(editor.HasSelection, Is.True);
        var text = editor.GetSelectedText();
        Assert.That(text, Does.Contain("One"));
        Assert.That(text, Does.Contain("Two"));
        Assert.That(text, Does.Contain("Three"));
    }

    [AvaloniaTest]
    public void DeleteSelectionWithinItem()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("Hello World");

        editor.SelectionAnchor = new CursorPosition(0, 5);
        editor.Caret = new CursorPosition(0, 11);
        editor.DeleteSelection();

        Assert.That(editor.GetText(), Is.EqualTo("Hello"));
    }

    [AvaloniaTest]
    public void DeleteSelectionAcrossItems()
    {
        var editor = new TodoListEditor();
        editor.SetText("First\nSecond\nThird");

        editor.SelectionAnchor = new CursorPosition(0, 3);
        editor.Caret = new CursorPosition(2, 3);
        editor.DeleteSelection();

        Assert.That(editor.Document.Items.Count, Is.EqualTo(1));
        Assert.That(editor.GetText(), Is.EqualTo("Firrd"));
    }

    [AvaloniaTest]
    public void DeleteSelectionWithImages()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("AB");
        editor.InsertImageAtCaret(CreateTestBitmap());
        editor.InsertTextAtCaret("CD");

        // Select image and "CD"
        editor.SelectionAnchor = new CursorPosition(0, 2);
        editor.Caret = new CursorPosition(0, 5);
        editor.DeleteSelection();

        Assert.That(editor.GetText(), Is.EqualTo("AB"));
    }

    // ---- Caret tests ----

    [AvaloniaTest]
    public void CaretStartsAtOrigin()
    {
        var editor = new TodoListEditor();
        Assert.That(editor.Caret.ItemIndex, Is.EqualTo(0));
        Assert.That(editor.Caret.Offset, Is.EqualTo(0));
    }

    [AvaloniaTest]
    public void CaretAdvancesAfterInsert()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("ABC");
        Assert.That(editor.Caret.Offset, Is.EqualTo(3));
    }

    [AvaloniaTest]
    public void CaretAfterSplit()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("Hello");
        editor.SplitItemAtCaret();
        Assert.That(editor.Caret.ItemIndex, Is.EqualTo(1));
        Assert.That(editor.Caret.Offset, Is.EqualTo(0));
    }

    [AvaloniaTest]
    public void CaretAfterImageInsert()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("AB");
        editor.InsertImageAtCaret(CreateTestBitmap());
        Assert.That(editor.Caret.Offset, Is.EqualTo(3)); // AB + image
    }

    // ---- Font property tests ----

    [AvaloniaTest]
    public void DefaultFontSizeProperty()
    {
        var editor = new TodoListEditor();
        Assert.That(editor.DefaultFontSize, Is.EqualTo(14.0));
        editor.DefaultFontSize = 20;
        Assert.That(editor.DefaultFontSize, Is.EqualTo(20.0));
    }

    [AvaloniaTest]
    public void DefaultFontFamilyProperty()
    {
        var editor = new TodoListEditor();
        var courier = new FontFamily("Courier New");
        editor.DefaultFont = courier;
        Assert.That(editor.DefaultFont.Name, Is.EqualTo("Courier New"));
    }

    [AvaloniaTest]
    public void InsertedTextUsesDefaultFont()
    {
        var editor = new TodoListEditor();
        editor.DefaultFont = new FontFamily("Courier New");
        editor.DefaultFontSize = 18;
        editor.InsertTextAtCaret("test");

        var el = editor.Document.Items[0].Elements[0];
        Assert.That(el.Font.Name, Is.EqualTo("Courier New"));
        Assert.That(el.FontSize, Is.EqualTo(18));
    }

    // ---- Rich copy/paste ----

    [AvaloniaTest]
    public void CopyPastePreservesImages()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("AB");
        editor.InsertImageAtCaret(CreateTestBitmap());
        editor.InsertTextAtCaret("CD");

        editor.SelectionAnchor = new CursorPosition(0, 0);
        editor.Caret = new CursorPosition(0, 5);
        editor.CopyToClipboard();

        editor.SplitItemAtCaret();

        editor.SelectionAnchor = editor.Caret;
        editor.PasteRichFromInternalClipboard();

        var pastedItem = editor.Document.Items[1];
        Assert.That(pastedItem.Elements.Count, Is.EqualTo(3));
        Assert.That(pastedItem.Elements[0].Type, Is.EqualTo(ContentElementType.Text));
        Assert.That(pastedItem.Elements[0].Text, Is.EqualTo("AB"));
        Assert.That(pastedItem.Elements[1].Type, Is.EqualTo(ContentElementType.Image));
        Assert.That(pastedItem.Elements[2].Type, Is.EqualTo(ContentElementType.Text));
        Assert.That(pastedItem.Elements[2].Text, Is.EqualTo("CD"));
    }

    [AvaloniaTest]
    public void CopyPasteAcrossItemsPreservesImages()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("Line1");
        editor.InsertImageAtCaret(CreateTestBitmap());
        editor.SplitItemAtCaret();
        editor.InsertTextAtCaret("Line2");

        editor.SelectionAnchor = new CursorPosition(0, 0);
        editor.Caret = new CursorPosition(1, 5);
        editor.CopyToClipboard();

        editor.Caret = new CursorPosition(1, 5);
        editor.SelectionAnchor = editor.Caret;
        editor.SplitItemAtCaret();

        editor.PasteRichFromInternalClipboard();

        Assert.That(editor.Document.Items.Count, Is.EqualTo(4));
        var pasted0 = editor.Document.Items[2];
        Assert.That(pasted0.Elements.Any(e => e.Type == ContentElementType.Image), Is.True);
    }

    // ---- AddItem API ----

    [AvaloniaTest]
    public void AddItemApi()
    {
        var editor = new TodoListEditor();
        editor.AddItem("Buy groceries");
        editor.AddItem("Clean house", true);

        Assert.That(editor.Document.Items.Count, Is.EqualTo(3)); // 1 default + 2 added
        Assert.That(editor.Document.Items[1].PlainText, Is.EqualTo("Buy groceries"));
        Assert.That(editor.Document.Items[2].IsChecked, Is.True);
    }

    private static Bitmap CreateTestBitmap()
    {
        return new WriteableBitmap(
            new Avalonia.PixelSize(10, 10),
            new Avalonia.Vector(96, 96),
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul);
    }
}
