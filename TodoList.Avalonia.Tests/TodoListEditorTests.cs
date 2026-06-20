using System.Collections.Generic;
using System.Collections.ObjectModel;
using global::Avalonia.Headless.NUnit;
using global::Avalonia.Media;
using global::Avalonia.Media.Imaging;
using NUnit.Framework;
using TodoList.Avalonia.Controls;
using TodoList.Avalonia.Model;

namespace TodoList.Avalonia.Tests;

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
        Assert.That(el.Font, Is.EqualTo(FontFamily.Default));
        Assert.That(el.FontSize, Is.EqualTo(0));
        Assert.That(editor.Document.DefaultFont.Name, Is.EqualTo("Courier New"));
        Assert.That(editor.Document.DefaultFontSize, Is.EqualTo(18));
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

    // ---- Text wrapping tests ----

    [AvaloniaTest]
    public void LongTextWrapsWithinNarrowEditor()
    {
        var editor = new TodoListEditor();
        editor.DefaultFontSize = 14;
        editor.Measure(new global::Avalonia.Size(150, 600));

        editor.InsertTextAtCaret("This is a very long text that should wrap to multiple lines in a narrow editor");

        editor.Measure(new global::Avalonia.Size(150, 600));

        var item = editor.Document.Items[0];
        Assert.That(item.PlainText.Length, Is.GreaterThan(20));
        Assert.That(editor.GetText(), Does.Contain("very long text"));
    }

    [AvaloniaTest]
    public void LongTextRemainsOneItemAfterWrapping()
    {
        var editor = new TodoListEditor();
        editor.DefaultFontSize = 14;
        editor.InsertTextAtCaret("AAAA BBBB CCCC DDDD EEEE FFFF GGGG HHHH");
        editor.Measure(new global::Avalonia.Size(150, 600));

        Assert.That(editor.Document.Items.Count, Is.EqualTo(1));
        Assert.That(editor.Caret.ItemIndex, Is.EqualTo(0));
        Assert.That(editor.Caret.Offset, Is.EqualTo(39));
    }

    [AvaloniaTest]
    public void SelectionAcrossWrappedLinesWorks()
    {
        var editor = new TodoListEditor();
        editor.DefaultFontSize = 14;
        editor.InsertTextAtCaret("AAAA BBBB CCCC DDDD EEEE FFFF");
        editor.Measure(new global::Avalonia.Size(150, 600));

        editor.SelectionAnchor = new CursorPosition(0, 5);
        editor.Caret = new CursorPosition(0, 25);

        Assert.That(editor.HasSelection, Is.True);
        Assert.That(editor.GetSelectedText(), Is.EqualTo("BBBB CCCC DDDD EEEE "));
    }

    [AvaloniaTest]
    public void DeleteSelectionInWrappedTextWorks()
    {
        var editor = new TodoListEditor();
        editor.DefaultFontSize = 14;
        editor.InsertTextAtCaret("AAAA BBBB CCCC DDDD");
        editor.Measure(new global::Avalonia.Size(150, 600));

        editor.SelectionAnchor = new CursorPosition(0, 5);
        editor.Caret = new CursorPosition(0, 15);
        editor.DeleteSelection();

        Assert.That(editor.GetText(), Is.EqualTo("AAAA DDDD"));
    }

    [AvaloniaTest]
    public void InsertTextInWrappedItemWorks()
    {
        var editor = new TodoListEditor();
        editor.DefaultFontSize = 14;
        editor.InsertTextAtCaret("AAAA CCCC");
        editor.Measure(new global::Avalonia.Size(150, 600));

        editor.Caret = new CursorPosition(0, 5);
        editor.SelectionAnchor = editor.Caret;
        editor.InsertTextAtCaret("BBBB ");

        Assert.That(editor.GetText(), Is.EqualTo("AAAA BBBB CCCC"));
    }

    [AvaloniaTest]
    public void SplitItemInWrappedTextWorks()
    {
        var editor = new TodoListEditor();
        editor.DefaultFontSize = 14;
        editor.InsertTextAtCaret("AAAA BBBB CCCC DDDD");
        editor.Measure(new global::Avalonia.Size(150, 600));

        editor.Caret = new CursorPosition(0, 10);
        editor.SelectionAnchor = editor.Caret;
        editor.SplitItemAtCaret();

        Assert.That(editor.Document.Items.Count, Is.EqualTo(2));
        Assert.That(editor.Document.Items[0].PlainText, Is.EqualTo("AAAA BBBB "));
        Assert.That(editor.Document.Items[1].PlainText, Is.EqualTo("CCCC DDDD"));
    }

    [AvaloniaTest]
    public void ImageInWrappedLineDoesNotCrash()
    {
        var editor = new TodoListEditor();
        editor.DefaultFontSize = 14;
        editor.InsertTextAtCaret("Before image ");
        editor.InsertImageAtCaret(CreateTestBitmap());
        editor.InsertTextAtCaret(" After image text that is long enough to wrap");
        editor.Measure(new global::Avalonia.Size(150, 600));

        Assert.That(editor.Document.Items[0].Elements.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(editor.GetText(), Does.Contain("Before image"));
    }

    [AvaloniaTest]
    public void MoveCaretDownNavigatesWrappedLines()
    {
        var editor = new TodoListEditor();
        editor.DefaultFontSize = 14;
        editor.InsertTextAtCaret("AAAA BBBB CCCC DDDD EEEE FFFF");
        editor.Measure(new global::Avalonia.Size(150, 600));

        editor.Caret = new CursorPosition(0, 2);
        editor.SelectionAnchor = editor.Caret;

        editor.MoveCaretVertical(1, false);

        Assert.That(editor.Caret.ItemIndex, Is.EqualTo(0));
        Assert.That(editor.Caret.Offset, Is.GreaterThan(2));
    }

    [AvaloniaTest]
    public void MoveCaretUpNavigatesWrappedLines()
    {
        var editor = new TodoListEditor();
        editor.DefaultFontSize = 14;
        editor.InsertTextAtCaret("AAAA BBBB CCCC DDDD EEEE FFFF");
        editor.Measure(new global::Avalonia.Size(150, 600));

        int endOffset = editor.Document.Items[0].TextLength;
        editor.Caret = new CursorPosition(0, endOffset);
        editor.SelectionAnchor = editor.Caret;

        editor.MoveCaretVertical(-1, false);

        Assert.That(editor.Caret.ItemIndex, Is.EqualTo(0));
        Assert.That(editor.Caret.Offset, Is.LessThan(endOffset));
    }

    [AvaloniaTest]
    public void MoveCaretDownFromLastWrappedLineGoesToNextItem()
    {
        var editor = new TodoListEditor();
        editor.DefaultFontSize = 14;
        editor.SetText("AAAA BBBB CCCC DDDD EEEE FFFF\nSecond");
        editor.Measure(new global::Avalonia.Size(150, 600));

        int lastOffset = editor.Document.Items[0].TextLength;
        editor.Caret = new CursorPosition(0, lastOffset);
        editor.SelectionAnchor = editor.Caret;

        editor.MoveCaretVertical(1, false);

        Assert.That(editor.Caret.ItemIndex, Is.EqualTo(1));
    }

    [AvaloniaTest]
    public void MoveCaretUpFromFirstWrappedLineGoesToPreviousItem()
    {
        var editor = new TodoListEditor();
        editor.DefaultFontSize = 14;
        editor.SetText("First\nAAAA BBBB CCCC DDDD EEEE FFFF");
        editor.Measure(new global::Avalonia.Size(150, 600));

        editor.Caret = new CursorPosition(1, 2);
        editor.SelectionAnchor = editor.Caret;

        editor.MoveCaretVertical(-1, false);

        Assert.That(editor.Caret.ItemIndex, Is.EqualTo(0));
    }

    [AvaloniaTest]
    public void MoveCaretVerticalWithShiftExtendsSelection()
    {
        var editor = new TodoListEditor();
        editor.DefaultFontSize = 14;
        editor.InsertTextAtCaret("AAAA BBBB CCCC DDDD EEEE FFFF");
        editor.Measure(new global::Avalonia.Size(150, 600));

        editor.Caret = new CursorPosition(0, 2);
        editor.SelectionAnchor = new CursorPosition(0, 2);

        editor.MoveCaretVertical(1, true);

        Assert.That(editor.HasSelection, Is.True);
        Assert.That(editor.SelectionAnchor.Offset, Is.EqualTo(2));
        Assert.That(editor.Caret.Offset, Is.GreaterThan(2));
    }

    // ---- Undo / Redo tests ----

    [AvaloniaTest]
    public void UndoRevertsTextInsert()
    {
        var editor = new TodoListEditor();
        editor.SaveUndoState();
        editor.InsertTextAtCaret("Hello");

        Assert.That(editor.GetText(), Is.EqualTo("Hello"));

        editor.Undo();

        Assert.That(editor.GetText(), Is.EqualTo(""));
    }

    [AvaloniaTest]
    public void RedoRestoresUndoneChange()
    {
        var editor = new TodoListEditor();
        editor.SaveUndoState();
        editor.InsertTextAtCaret("Hello");
        editor.Undo();

        Assert.That(editor.GetText(), Is.EqualTo(""));

        editor.Redo();

        Assert.That(editor.GetText(), Is.EqualTo("Hello"));
    }

    [AvaloniaTest]
    public void UndoRevertsSplitItem()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("AAAA BBBB");
        editor.Caret = new CursorPosition(0, 5);
        editor.SelectionAnchor = editor.Caret;

        editor.SaveUndoState();
        editor.SplitItemAtCaret();

        Assert.That(editor.Document.Items.Count, Is.EqualTo(2));

        editor.Undo();

        Assert.That(editor.Document.Items.Count, Is.EqualTo(1));
        Assert.That(editor.GetText(), Is.EqualTo("AAAA BBBB"));
    }

    [AvaloniaTest]
    public void UndoRevertsDeleteSelection()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("Hello World");
        editor.SelectionAnchor = new CursorPosition(0, 0);
        editor.Caret = new CursorPosition(0, 5);

        editor.SaveUndoState();
        editor.DeleteSelection();

        Assert.That(editor.GetText(), Is.EqualTo(" World"));

        editor.Undo();

        Assert.That(editor.GetText(), Is.EqualTo("Hello World"));
    }

    [AvaloniaTest]
    public void UndoRevertsCheckboxToggle()
    {
        var editor = new TodoListEditor();
        editor.SetText("Task 1");

        Assert.That(editor.Document.Items[0].IsChecked, Is.False);

        editor.SaveUndoState();
        editor.Document.Items[0].IsChecked = true;

        editor.Undo();

        Assert.That(editor.Document.Items[0].IsChecked, Is.False);
    }

    [AvaloniaTest]
    public void MultipleUndosWorkInOrder()
    {
        var editor = new TodoListEditor();

        editor.SaveUndoState();
        editor.InsertTextAtCaret("A");
        editor.SaveUndoState();
        editor.InsertTextAtCaret("B");
        editor.SaveUndoState();
        editor.InsertTextAtCaret("C");

        Assert.That(editor.GetText(), Is.EqualTo("ABC"));

        editor.Undo();
        Assert.That(editor.GetText(), Is.EqualTo("AB"));

        editor.Undo();
        Assert.That(editor.GetText(), Is.EqualTo("A"));

        editor.Undo();
        Assert.That(editor.GetText(), Is.EqualTo(""));
    }

    [AvaloniaTest]
    public void NewEditAfterUndoClearsRedoStack()
    {
        var editor = new TodoListEditor();

        editor.SaveUndoState();
        editor.InsertTextAtCaret("A");
        editor.SaveUndoState();
        editor.InsertTextAtCaret("B");

        editor.Undo();
        Assert.That(editor.GetText(), Is.EqualTo("A"));

        editor.SaveUndoState();
        editor.InsertTextAtCaret("C");
        Assert.That(editor.GetText(), Is.EqualTo("AC"));

        editor.Redo();
        Assert.That(editor.GetText(), Is.EqualTo("AC"));
    }

    [AvaloniaTest]
    public void UndoOnEmptyStackDoesNothing()
    {
        var editor = new TodoListEditor();
        editor.InsertTextAtCaret("Hello");

        editor.Undo();

        Assert.That(editor.GetText(), Is.EqualTo("Hello"));
    }

    // ---- MVVM API Tests ----

    [AvaloniaTest]
    public void SetItemsPopulatesDocument()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Buy milk"),
            new("Walk dog", true),
        };
        editor.Items = items;

        Assert.That(editor.Document.Items.Count, Is.EqualTo(2));
        Assert.That(editor.Document.Items[0].PlainText, Is.EqualTo("Buy milk"));
        Assert.That(editor.Document.Items[0].IsChecked, Is.False);
        Assert.That(editor.Document.Items[1].PlainText, Is.EqualTo("Walk dog"));
        Assert.That(editor.Document.Items[1].IsChecked, Is.True);
    }

    [AvaloniaTest]
    public void EditSyncsBackToItems()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Hello"),
        };
        editor.Items = items;

        editor.Caret = new CursorPosition(0, 5);
        editor.SelectionAnchor = new CursorPosition(0, 5);
        editor.InsertTextAtCaret(" World");

        Assert.That(items[0].Text, Is.EqualTo("Hello World"));
    }

    [AvaloniaTest]
    public void ItemsChangedEventFires()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Test"),
        };
        editor.Items = items;

        int fireCount = 0;
        editor.ItemsChanged += (_, _) => fireCount++;

        editor.Caret = new CursorPosition(0, 4);
        editor.SelectionAnchor = new CursorPosition(0, 4);
        editor.InsertTextAtCaret("!");

        Assert.That(fireCount, Is.GreaterThan(0));
    }

    [AvaloniaTest]
    public void ViewModelTextChangeUpdatesDocument()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Original"),
        };
        editor.Items = items;

        items[0].Text = "Changed";

        Assert.That(editor.Document.Items[0].PlainText, Is.EqualTo("Changed"));
    }

    [AvaloniaTest]
    public void ViewModelCheckedChangeUpdatesDocument()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Task", false),
        };
        editor.Items = items;

        items[0].IsChecked = true;

        Assert.That(editor.Document.Items[0].IsChecked, Is.True);
    }

    [AvaloniaTest]
    public void CollectionAddSyncsToDocument()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("First"),
        };
        editor.Items = items;

        items.Add(new TodoItemData("Second"));

        Assert.That(editor.Document.Items.Count, Is.EqualTo(2));
        Assert.That(editor.Document.Items[1].PlainText, Is.EqualTo("Second"));
    }

    [AvaloniaTest]
    public void CollectionRemoveSyncsToDocument()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("First"),
            new("Second"),
        };
        editor.Items = items;

        items.RemoveAt(0);

        Assert.That(editor.Document.Items.Count, Is.EqualTo(1));
        Assert.That(editor.Document.Items[0].PlainText, Is.EqualTo("Second"));
    }

    [AvaloniaTest]
    public void ImageMarkdownParsedCorrectly()
    {
        var editor = new TodoListEditor();
        var bmp = CreateTestBitmap();
        editor.ImageStore["photo1"] = bmp;

        var items = new ObservableCollection<TodoItemData>
        {
            new("Before ![alt](photo1) after"),
        };
        editor.Items = items;

        Assert.That(editor.Document.Items[0].Elements.Count, Is.EqualTo(3));
        Assert.That(editor.Document.Items[0].Elements[0].Type, Is.EqualTo(ContentElementType.Text));
        Assert.That(editor.Document.Items[0].Elements[0].Text, Is.EqualTo("Before "));
        Assert.That(editor.Document.Items[0].Elements[1].Type, Is.EqualTo(ContentElementType.Image));
        Assert.That(editor.Document.Items[0].Elements[1].ImageKey, Is.EqualTo("photo1"));
        Assert.That(editor.Document.Items[0].Elements[1].ImageAltText, Is.EqualTo("alt"));
        Assert.That(editor.Document.Items[0].Elements[2].Type, Is.EqualTo(ContentElementType.Text));
        Assert.That(editor.Document.Items[0].Elements[2].Text, Is.EqualTo(" after"));
    }

    [AvaloniaTest]
    public void ImageSerializedBackToMarkdown()
    {
        var editor = new TodoListEditor();
        var bmp = CreateTestBitmap();
        editor.ImageStore["pic1"] = bmp;

        var items = new ObservableCollection<TodoItemData>
        {
            new("Check ![receipt](pic1) done"),
        };
        editor.Items = items;

        Assert.That(items[0].Text, Is.EqualTo("Check ![receipt](pic1) done"));

        editor.Caret = new CursorPosition(0, 0);
        editor.SelectionAnchor = new CursorPosition(0, 0);
        editor.InsertTextAtCaret("X");

        Assert.That(items[0].Text, Is.EqualTo("XCheck ![receipt](pic1) done"));
    }

    [AvaloniaTest]
    public void UnknownImageKeyKeptAsText()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Text ![alt](missing_key) more"),
        };
        editor.Items = items;

        Assert.That(editor.Document.Items[0].Elements.All(
            e => e.Type == ContentElementType.Text), Is.True);
        Assert.That(editor.Document.Items[0].PlainText, Is.EqualTo("Text ![alt](missing_key) more"));
    }

    [AvaloniaTest]
    public void SplitItemSyncsNewItemToCollection()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("HelloWorld"),
        };
        editor.Items = items;

        editor.Caret = new CursorPosition(0, 5);
        editor.SelectionAnchor = new CursorPosition(0, 5);
        editor.SplitItemAtCaret();

        Assert.That(items.Count, Is.EqualTo(2));
        Assert.That(items[0].Text, Is.EqualTo("Hello"));
        Assert.That(items[1].Text, Is.EqualTo("World"));
    }

    [AvaloniaTest]
    public void ToggleItemSyncsToItems()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Task", false),
        };
        editor.Items = items;

        editor.ToggleItem(0);

        Assert.That(items[0].IsChecked, Is.True);
    }

    [AvaloniaTest]
    public void NullItemsWorksLikeBeforeMvvm()
    {
        var editor = new TodoListEditor();
        Assert.That(editor.Items, Is.Null);

        editor.InsertTextAtCaret("No MVVM");
        Assert.That(editor.GetText(), Is.EqualTo("No MVVM"));
    }

    [AvaloniaTest]
    public void PastedImageGetsAutoRegisteredInImageStore()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Before"),
        };
        editor.Items = items;

        var bmp = CreateTestBitmap();
        editor.Caret = new CursorPosition(0, 6);
        editor.SelectionAnchor = new CursorPosition(0, 6);
        editor.InsertImageAtCaret(bmp);

        Assert.That(items[0].Text, Does.Contain("!["));
        Assert.That(items[0].Text, Does.Contain("]("));
        Assert.That(editor.ImageStore.Count, Is.EqualTo(1));
    }

    [AvaloniaTest]
    public void MultipleImagesInSingleItem()
    {
        var editor = new TodoListEditor();
        var bmp1 = CreateTestBitmap();
        var bmp2 = CreateTestBitmap();
        editor.ImageStore["a"] = bmp1;
        editor.ImageStore["b"] = bmp2;

        var items = new ObservableCollection<TodoItemData>
        {
            new("![x](a) mid ![y](b)"),
        };
        editor.Items = items;

        Assert.That(editor.Document.Items[0].Elements.Count, Is.EqualTo(3));
        Assert.That(editor.Document.Items[0].Elements[0].Type, Is.EqualTo(ContentElementType.Image));
        Assert.That(editor.Document.Items[0].Elements[1].Type, Is.EqualTo(ContentElementType.Text));
        Assert.That(editor.Document.Items[0].Elements[1].Text, Is.EqualTo(" mid "));
        Assert.That(editor.Document.Items[0].Elements[2].Type, Is.EqualTo(ContentElementType.Image));
    }

    [AvaloniaTest]
    public void DeleteSelectionAcrossItemsMergesAndSyncs()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("First"),
            new("Second"),
        };
        editor.Items = items;

        editor.Caret = new CursorPosition(0, 5);
        editor.SelectionAnchor = new CursorPosition(1, 0);
        editor.DeleteSelection();

        Assert.That(items.Count, Is.EqualTo(1));
        Assert.That(items[0].Text, Is.EqualTo("FirstSecond"));
    }

    // ---- DeleteRange bug regression test ----

    [AvaloniaTest]
    public void DeleteRange_DoesNotDeleteAdjacentImage()
    {
        var editor = new TodoListEditor();
        editor.Document.Items.Clear();

        var item = new TodoItem();
        item.Elements.Add(ContentElement.CreateText("AB"));
        item.Elements.Add(ContentElement.CreateImage(CreateTestBitmap()));
        item.Elements.Add(ContentElement.CreateText("CD"));
        editor.Document.Items.Add(item);

        // Delete "A" from "AB" — image and "CD" should survive
        editor.Caret = new CursorPosition(0, 1);
        editor.SelectionAnchor = new CursorPosition(0, 0);
        editor.DeleteSelection();

        var resultItem = editor.Document.Items[0];
        Assert.That(resultItem.Elements.Count, Is.EqualTo(3));
        Assert.That(resultItem.Elements[0].Text, Is.EqualTo("B"));
        Assert.That(resultItem.Elements[1].Type, Is.EqualTo(ContentElementType.Image));
        Assert.That(resultItem.Elements[2].Text, Is.EqualTo("CD"));
    }

    [AvaloniaTest]
    public void DeleteRange_SpanningTextAndImage()
    {
        var editor = new TodoListEditor();
        editor.Document.Items.Clear();

        var item = new TodoItem();
        item.Elements.Add(ContentElement.CreateText("AB"));
        item.Elements.Add(ContentElement.CreateImage(CreateTestBitmap()));
        item.Elements.Add(ContentElement.CreateText("CD"));
        editor.Document.Items.Add(item);

        // Delete "B" + image (offsets 1..3)
        editor.Caret = new CursorPosition(0, 3);
        editor.SelectionAnchor = new CursorPosition(0, 1);
        editor.DeleteSelection();

        var resultItem = editor.Document.Items[0];
        Assert.That(resultItem.PlainText, Is.EqualTo("ACD"));
    }

    // ---- Ctrl+Arrow word navigation tests ----

    [AvaloniaTest]
    public void CtrlRight_MovesToNextWordBoundary()
    {
        var editor = new TodoListEditor();
        editor.Document.Items.Clear();
        editor.Document.Items.Add(new TodoItem("Hello World"));
        editor.Caret = new CursorPosition(0, 0);
        editor.SelectionAnchor = editor.Caret;

        editor.MoveCaretByWord(1, false);

        Assert.That(editor.Caret.Offset, Is.EqualTo(6));
    }

    [AvaloniaTest]
    public void CtrlLeft_MovesToPreviousWordBoundary()
    {
        var editor = new TodoListEditor();
        editor.Document.Items.Clear();
        editor.Document.Items.Add(new TodoItem("Hello World"));
        editor.Caret = new CursorPosition(0, 11);
        editor.SelectionAnchor = editor.Caret;

        editor.MoveCaretByWord(-1, false);

        Assert.That(editor.Caret.Offset, Is.EqualTo(6));
    }

    [AvaloniaTest]
    public void CtrlShiftRight_ExtendsSelection()
    {
        var editor = new TodoListEditor();
        editor.Document.Items.Clear();
        editor.Document.Items.Add(new TodoItem("Hello World"));
        editor.Caret = new CursorPosition(0, 0);
        editor.SelectionAnchor = editor.Caret;

        editor.MoveCaretByWord(1, true);

        Assert.That(editor.Caret.Offset, Is.EqualTo(6));
        Assert.That(editor.SelectionAnchor.Offset, Is.EqualTo(0));
        Assert.That(editor.HasSelection, Is.True);
    }

    // ---- Images collection tests ----

    [AvaloniaTest]
    public void ImagesCollectionResolvesImageInText()
    {
        var editor = new TodoListEditor();
        var bmp = CreateTestBitmap();
        var images = new ObservableCollection<TodoImageEntry>
        {
            new("photo1", bmp)
        };
        var items = new ObservableCollection<TodoItemData>
        {
            new("Before ![alt](photo1) after"),
        };
        editor.Images = images;
        editor.Items = items;

        Assert.That(editor.Document.Items[0].Elements.Count, Is.EqualTo(3));
        Assert.That(editor.Document.Items[0].Elements[1].Type, Is.EqualTo(ContentElementType.Image));
        Assert.That(editor.Document.Items[0].Elements[1].ImageKey, Is.EqualTo("photo1"));
    }

    [AvaloniaTest]
    public void ImagesCollectionOrderIndependent_ItemsFirst()
    {
        var editor = new TodoListEditor();
        var bmp = CreateTestBitmap();

        var items = new ObservableCollection<TodoItemData>
        {
            new("Text ![alt](pic1) end"),
        };
        editor.Items = items;

        Assert.That(editor.Document.Items[0].Elements.All(
            e => e.Type == ContentElementType.Text), Is.True);

        var images = new ObservableCollection<TodoImageEntry>
        {
            new("pic1", bmp)
        };
        editor.Images = images;

        Assert.That(editor.Document.Items[0].Elements[1].Type, Is.EqualTo(ContentElementType.Image));
        Assert.That(editor.Document.Items[0].Elements[1].ImageKey, Is.EqualTo("pic1"));
    }

    [AvaloniaTest]
    public void DeferredImageLoad_NullBitmapThenSet()
    {
        var editor = new TodoListEditor();
        var images = new ObservableCollection<TodoImageEntry>();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Before ![alt](lazy) after"),
        };
        editor.Images = images;
        editor.Items = items;

        var entry = new TodoImageEntry("lazy");
        images.Add(entry);

        Assert.That(editor.Document.Items[0].Elements.All(
            e => e.Type == ContentElementType.Text), Is.True);

        entry.Bitmap = CreateTestBitmap();

        Assert.That(editor.Document.Items[0].Elements[1].Type, Is.EqualTo(ContentElementType.Image));
        Assert.That(editor.Document.Items[0].Elements[1].ImageKey, Is.EqualTo("lazy"));
    }

    [AvaloniaTest]
    public void MissingImageKeyStaysAsText_NoImagesCollection()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Text ![alt](missing) more"),
        };
        editor.Items = items;

        Assert.That(editor.Document.Items[0].Elements.All(
            e => e.Type == ContentElementType.Text), Is.True);
        Assert.That(editor.Document.Items[0].PlainText, Is.EqualTo("Text ![alt](missing) more"));
    }

    [AvaloniaTest]
    public void PastedImageAutoAddsToImagesCollection()
    {
        var editor = new TodoListEditor();
        var images = new ObservableCollection<TodoImageEntry>();
        editor.Images = images;

        var items = new ObservableCollection<TodoItemData>
        {
            new("Before"),
        };
        editor.Items = items;

        editor.Caret = new CursorPosition(0, 6);
        editor.SelectionAnchor = new CursorPosition(0, 6);
        editor.InsertImageAtCaret(CreateTestBitmap());

        Assert.That(images.Count, Is.EqualTo(1));
        Assert.That(items[0].Text, Does.Contain($"![image]({images[0].Key})"));
    }

    // ---- TodoMarkdown tests ----

    [Test]
    public void ParseMarkdown_CheckedAndUnchecked()
    {
        var items = TodoMarkdown.ParseMarkdown("- [ ] Buy milk\n- [x] Walk the dog");

        Assert.That(items.Count, Is.EqualTo(2));
        Assert.That(items[0].Text, Is.EqualTo("Buy milk"));
        Assert.That(items[0].IsChecked, Is.False);
        Assert.That(items[1].Text, Is.EqualTo("Walk the dog"));
        Assert.That(items[1].IsChecked, Is.True);
    }

    [Test]
    public void ParseMarkdown_CapitalX()
    {
        var items = TodoMarkdown.ParseMarkdown("- [X] Done");

        Assert.That(items[0].IsChecked, Is.True);
        Assert.That(items[0].Text, Is.EqualTo("Done"));
    }

    [Test]
    public void ParseMarkdown_LineWithoutCheckbox()
    {
        var items = TodoMarkdown.ParseMarkdown("Plain line\n- [ ] With checkbox");

        Assert.That(items.Count, Is.EqualTo(2));
        Assert.That(items[0].Text, Is.EqualTo("Plain line"));
        Assert.That(items[0].IsChecked, Is.False);
        Assert.That(items[1].Text, Is.EqualTo("With checkbox"));
    }

    [Test]
    public void ParseMarkdown_EmptyString()
    {
        var items = TodoMarkdown.ParseMarkdown("");
        Assert.That(items.Count, Is.EqualTo(0));
    }

    [Test]
    public void ParseMarkdown_NullString()
    {
        var items = TodoMarkdown.ParseMarkdown(null!);
        Assert.That(items.Count, Is.EqualTo(0));
    }

    [Test]
    public void ToMarkdown_RoundTrip()
    {
        var original = "- [ ] Buy milk\n- [x] Walk the dog\n- [ ] Code review";
        var items = TodoMarkdown.ParseMarkdown(original);
        var serialized = TodoMarkdown.ToMarkdown(items);

        Assert.That(serialized, Is.EqualTo(original));
    }

    [Test]
    public void ToMarkdown_WithImageSyntax()
    {
        var items = new List<TodoItemData>
        {
            new("Text with ![star](star) image", false)
        };
        var md = TodoMarkdown.ToMarkdown(items);

        Assert.That(md, Is.EqualTo("- [ ] Text with ![star](star) image"));
    }

    // ---- IsDirty tests ----

    [AvaloniaTest]
    public void IsDirty_CleanAfterLoad()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Item 1"),
            new("Item 2", true),
        };
        editor.Items = items;

        Assert.That(editor.IsDirty, Is.False);
    }

    [AvaloniaTest]
    public void IsDirty_DirtyAfterEdit()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Item 1"),
        };
        editor.Items = items;

        editor.Caret = new CursorPosition(0, 6);
        editor.SelectionAnchor = editor.Caret;
        editor.InsertTextAtCaret("X");

        Assert.That(editor.IsDirty, Is.True);
    }

    [AvaloniaTest]
    public void IsDirty_CleanAfterMarkClean()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Item 1"),
        };
        editor.Items = items;

        editor.InsertTextAtCaret("X");
        Assert.That(editor.IsDirty, Is.True);

        editor.MarkClean();
        Assert.That(editor.IsDirty, Is.False);
    }

    [AvaloniaTest]
    public void IsDirty_DirtyAfterUndo()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Original"),
        };
        editor.Items = items;

        editor.Caret = new CursorPosition(0, 8);
        editor.SelectionAnchor = editor.Caret;
        editor.SaveUndoState();
        editor.InsertTextAtCaret("X");
        editor.MarkClean();

        Assert.That(editor.IsDirty, Is.False);

        editor.Undo();
        Assert.That(editor.IsDirty, Is.True);
    }

    [AvaloniaTest]
    public void IsDirty_DirtyChangedEventFires()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new("Item 1"),
        };
        editor.Items = items;

        int fireCount = 0;
        editor.DirtyChanged += (_, _) => fireCount++;

        editor.InsertTextAtCaret("X");
        Assert.That(fireCount, Is.EqualTo(1));

        editor.MarkClean();
        Assert.That(fireCount, Is.EqualTo(2));
    }

    [AvaloniaTest]
    public void MvvmTextChangeSetsIsDirty()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new TodoItemData("Hello")
        };
        editor.Items = items;

        Assert.That(editor.IsDirty, Is.False);

        items[0].Text = "Changed";

        Assert.That(editor.IsDirty, Is.True);
    }

    [AvaloniaTest]
    public void MvvmIsCheckedChangeSetsIsDirty()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new TodoItemData("Task", false)
        };
        editor.Items = items;

        Assert.That(editor.IsDirty, Is.False);

        items[0].IsChecked = true;

        Assert.That(editor.IsDirty, Is.True);
    }

    [AvaloniaTest]
    public void MvvmAddItemSetsIsDirty()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new TodoItemData("First")
        };
        editor.Items = items;
        editor.MarkClean();

        Assert.That(editor.IsDirty, Is.False);

        items.Add(new TodoItemData("Second"));

        Assert.That(editor.IsDirty, Is.True);
    }

    [AvaloniaTest]
    public void MvvmRemoveItemSetsIsDirty()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new TodoItemData("First"),
            new TodoItemData("Second")
        };
        editor.Items = items;
        editor.MarkClean();

        Assert.That(editor.IsDirty, Is.False);

        items.RemoveAt(1);

        Assert.That(editor.IsDirty, Is.True);
    }

    [AvaloniaTest]
    public void DirtyChangedFiresOnMvvmChange()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new TodoItemData("Hello")
        };
        editor.Items = items;

        int firedCount = 0;
        editor.DirtyChanged += (_, _) => firedCount++;

        items[0].Text = "Changed";

        Assert.That(firedCount, Is.EqualTo(1));
        Assert.That(editor.IsDirty, Is.True);
    }

    [AvaloniaTest]
    public void DefaultFontNameSyncsToDefaultFont()
    {
        var editor = new TodoListEditor();
        editor.DefaultFontName = "Consolas";
        Assert.That(editor.DefaultFont.Name, Is.EqualTo("Consolas"));
    }

    [AvaloniaTest]
    public void DefaultFontSyncsToDefaultFontName()
    {
        var editor = new TodoListEditor();
        editor.DefaultFont = new FontFamily("Consolas");
        Assert.That(editor.DefaultFontName, Is.EqualTo("Consolas"));
    }

    [AvaloniaTest]
    public void ImagePastedKeyOverrideUpdatesCorrectly()
    {
        var editor = new TodoListEditor();
        var images = new ObservableCollection<TodoImageEntry>();
        editor.Images = images;
        editor.Items = new ObservableCollection<TodoItemData> { new TodoItemData("test") };

        editor.ImagePasted += (_, args) => { args.NewKey = "custom_key"; };
        editor.InsertImageAtCaret(CreateTestBitmap());

        Assert.That(images.Count, Is.EqualTo(1));
        Assert.That(images[0].Key, Is.EqualTo("custom_key"));
    }

    [Test]
    public void ToMarkdownEmptyListReturnsEmpty()
    {
        var result = TodoMarkdown.ToMarkdown(new List<TodoItemData>());
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ToMarkdownNullReturnsEmpty()
    {
        var result = TodoMarkdown.ToMarkdown(null);
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void TodoImageEntryNullKeyThrows()
    {
        Assert.Throws<System.ArgumentNullException>(() => new TodoImageEntry(null!));
    }

    [Test]
    public void ParseMarkdownSkipsEmptyLines()
    {
        var items = TodoMarkdown.ParseMarkdown("- [ ] First\n\n\n- [x] Second\n");
        Assert.That(items.Count, Is.EqualTo(2));
        Assert.That(items[0].Text, Is.EqualTo("First"));
        Assert.That(items[1].Text, Is.EqualTo("Second"));
        Assert.That(items[1].IsChecked, Is.True);
    }

    [Test]
    public void ParseMarkdownMalformedCheckboxTreatedAsPlainText()
    {
        var items = TodoMarkdown.ParseMarkdown("- [z] text\n- [✓] done\n- [] empty");
        Assert.That(items.Count, Is.EqualTo(3));
        Assert.That(items[0].Text, Is.EqualTo("- [z] text"));
        Assert.That(items[0].IsChecked, Is.False);
        Assert.That(items[1].Text, Is.EqualTo("- [✓] done"));
        Assert.That(items[2].Text, Is.EqualTo("- [] empty"));
    }

    [AvaloniaTest]
    public void ImagePastedKeyOverrideUpdatesLegacyImageStore()
    {
        var editor = new TodoListEditor();
        var images = new ObservableCollection<TodoImageEntry>();
        editor.Images = images;
        editor.Items = new ObservableCollection<TodoItemData> { new TodoItemData("test") };

        editor.ImagePasted += (_, args) => { args.NewKey = "remapped"; };
        editor.InsertImageAtCaret(CreateTestBitmap());

#pragma warning disable CS0618
        Assert.That(editor.ImageStore.ContainsKey("remapped"), Is.True);
        Assert.That(editor.ImageStore.Count, Is.EqualTo(1));
#pragma warning restore CS0618
    }

    // ---- ParseMarkdown: formats without "- " prefix ----

    [Test]
    public void ParseMarkdown_WithoutDashPrefix()
    {
        var items = TodoMarkdown.ParseMarkdown("[x] Done\n[ ] Not done");
        Assert.That(items.Count, Is.EqualTo(2));
        Assert.That(items[0].Text, Is.EqualTo("Done"));
        Assert.That(items[0].IsChecked, Is.True);
        Assert.That(items[1].Text, Is.EqualTo("Not done"));
        Assert.That(items[1].IsChecked, Is.False);
    }

    [Test]
    public void ParseMarkdown_WithoutDashCapitalX()
    {
        var items = TodoMarkdown.ParseMarkdown("[X] Task");
        Assert.That(items.Count, Is.EqualTo(1));
        Assert.That(items[0].IsChecked, Is.True);
        Assert.That(items[0].Text, Is.EqualTo("Task"));
    }

    [Test]
    public void ParseMarkdown_MixedFormats()
    {
        var items = TodoMarkdown.ParseMarkdown("- [x] With dash\n[x] Without dash\n- [ ] Unchecked dash\n[ ] Unchecked no dash");
        Assert.That(items.Count, Is.EqualTo(4));
        Assert.That(items[0].Text, Is.EqualTo("With dash"));
        Assert.That(items[0].IsChecked, Is.True);
        Assert.That(items[1].Text, Is.EqualTo("Without dash"));
        Assert.That(items[1].IsChecked, Is.True);
        Assert.That(items[2].Text, Is.EqualTo("Unchecked dash"));
        Assert.That(items[2].IsChecked, Is.False);
        Assert.That(items[3].Text, Is.EqualTo("Unchecked no dash"));
        Assert.That(items[3].IsChecked, Is.False);
    }

    [Test]
    public void ParseMarkdown_WithoutDashNoSpaceAfterBracket()
    {
        var items = TodoMarkdown.ParseMarkdown("[x]Task without space");
        Assert.That(items.Count, Is.EqualTo(1));
        Assert.That(items[0].IsChecked, Is.True);
        Assert.That(items[0].Text, Is.EqualTo("Task without space"));
    }

    // ---- MarkdownText property tests ----

    [AvaloniaTest]
    public void MarkdownText_SetPopulatesItems()
    {
        var editor = new TodoListEditor();
        editor.MarkdownText = "- [x] Done\n- [ ] Todo";

        Assert.That(editor.Items, Is.Not.Null);
        Assert.That(editor.Items!.Count, Is.EqualTo(2));
        Assert.That(editor.Items[0].Text, Is.EqualTo("Done"));
        Assert.That(editor.Items[0].IsChecked, Is.True);
        Assert.That(editor.Items[1].Text, Is.EqualTo("Todo"));
        Assert.That(editor.Items[1].IsChecked, Is.False);
    }

    [AvaloniaTest]
    public void MarkdownText_SetWithNoDashFormat()
    {
        var editor = new TodoListEditor();
        editor.MarkdownText = "[x] Done\n[ ] Todo";

        Assert.That(editor.Items!.Count, Is.EqualTo(2));
        Assert.That(editor.Items[0].IsChecked, Is.True);
        Assert.That(editor.Items[1].IsChecked, Is.False);
    }

    [AvaloniaTest]
    public void MarkdownText_SyncsBackAfterEdit()
    {
        var editor = new TodoListEditor();
        editor.Items = new ObservableCollection<TodoItemData>
        {
            new TodoItemData("Buy milk", false),
            new TodoItemData("Walk dog", true)
        };
        editor.InsertTextAtCaret("extra");

        Assert.That(editor.MarkdownText, Does.Contain("- [ ] extraBuy milk"));
        Assert.That(editor.MarkdownText, Does.Contain("- [x] Walk dog"));
    }

    [AvaloniaTest]
    public void MarkdownText_SyncsWhenItemsSetDirectly()
    {
        var editor = new TodoListEditor();
        editor.Items = new ObservableCollection<TodoItemData>
        {
            new TodoItemData("Alpha", true),
            new TodoItemData("Beta", false)
        };

        Assert.That(editor.MarkdownText, Is.Not.Null);
        Assert.That(editor.MarkdownText, Does.Contain("- [x] Alpha"));
        Assert.That(editor.MarkdownText, Does.Contain("- [ ] Beta"));
    }

    [AvaloniaTest]
    public void MarkdownText_SetNullClearsItems()
    {
        var editor = new TodoListEditor();
        editor.MarkdownText = "- [x] Something";
        Assert.That(editor.Items!.Count, Is.EqualTo(1));

        editor.MarkdownText = null;
        Assert.That(editor.Items!.Count, Is.EqualTo(0));
    }

    [AvaloniaTest]
    public void MarkdownText_SetEmptyClearsItems()
    {
        var editor = new TodoListEditor();
        editor.MarkdownText = "- [x] Something";
        editor.MarkdownText = "";
        Assert.That(editor.Items!.Count, Is.EqualTo(0));
    }

    [AvaloniaTest]
    public void MarkdownText_SyncsOnCollectionAdd()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new TodoItemData("First", false)
        };
        editor.Items = items;

        items.Add(new TodoItemData("Second", true));

        Assert.That(editor.MarkdownText, Does.Contain("- [ ] First"));
        Assert.That(editor.MarkdownText, Does.Contain("- [x] Second"));
    }

    [AvaloniaTest]
    public void MarkdownText_SyncsOnCollectionRemove()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new TodoItemData("First", false),
            new TodoItemData("Second", true)
        };
        editor.Items = items;

        items.RemoveAt(0);

        Assert.That(editor.MarkdownText, Is.EqualTo("- [x] Second"));
    }

    [AvaloniaTest]
    public void MarkdownText_SyncsOnItemPropertyChange()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new TodoItemData("Task", false)
        };
        editor.Items = items;

        items[0].IsChecked = true;

        Assert.That(editor.MarkdownText, Is.EqualTo("- [x] Task"));
    }

    [AvaloniaTest]
    public void MarkdownText_NoDashFormatPreservedUntilNextSync()
    {
        var editor = new TodoListEditor();
        editor.MarkdownText = "[x] Done\n[ ] Todo";

        // Getter returns original value (no extra sync cycle)
        Assert.That(editor.MarkdownText, Is.EqualTo("[x] Done\n[ ] Todo"));

        // After an edit, MarkdownText normalizes to canonical dash format
        editor.Items![0].IsChecked = false;
        Assert.That(editor.MarkdownText, Is.EqualTo("- [ ] Done\n- [ ] Todo"));
    }

    [AvaloniaTest]
    public void MarkdownText_RoundtripPreservesDashFormat()
    {
        var editor = new TodoListEditor();
        editor.MarkdownText = "- [x] Done\n- [ ] Todo";

        Assert.That(editor.MarkdownText, Is.EqualTo("- [x] Done\n- [ ] Todo"));
    }

    [AvaloniaTest]
    public void MarkdownText_SyncsOnItemTextChange()
    {
        var editor = new TodoListEditor();
        var items = new ObservableCollection<TodoItemData>
        {
            new TodoItemData("Original", false)
        };
        editor.Items = items;

        items[0].Text = "Modified";

        Assert.That(editor.MarkdownText, Is.EqualTo("- [ ] Modified"));
    }

    [Test]
    public void ParseMarkdown_DashFormatNoSpaceAfterBracket()
    {
        var items = TodoMarkdown.ParseMarkdown("- [x]Task no space");
        Assert.That(items.Count, Is.EqualTo(1));
        Assert.That(items[0].IsChecked, Is.True);
        Assert.That(items[0].Text, Is.EqualTo("Task no space"));
    }

    private static Bitmap CreateTestBitmap()
    {
        return new WriteableBitmap(
            new global::Avalonia.PixelSize(10, 10),
            new global::Avalonia.Vector(96, 96),
            global::Avalonia.Platform.PixelFormat.Bgra8888,
            global::Avalonia.Platform.AlphaFormat.Premul);
    }
}
