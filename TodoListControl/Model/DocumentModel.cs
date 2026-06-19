using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace TodoListControl.Model;

public enum ContentElementType
{
    Text,
    Image
}

public class ContentElement
{
    public ContentElementType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public Bitmap? Image { get; set; }
    public double ImageWidth { get; set; }
    public double ImageHeight { get; set; }
    public FontFamily Font { get; set; } = FontFamily.Default;
    public double FontSize { get; set; } = 14;
    public bool Bold { get; set; }
    public bool Italic { get; set; }

    public static ContentElement CreateText(string text, FontFamily? font = null, double fontSize = 14)
    {
        return new ContentElement
        {
            Type = ContentElementType.Text,
            Text = text,
            Font = font ?? FontFamily.Default,
            FontSize = fontSize
        };
    }

    public static ContentElement CreateImage(Bitmap bitmap)
    {
        return new ContentElement
        {
            Type = ContentElementType.Image,
            Image = bitmap,
            ImageWidth = bitmap.PixelSize.Width,
            ImageHeight = bitmap.PixelSize.Height
        };
    }

    public ContentElement Clone()
    {
        return new ContentElement
        {
            Type = Type,
            Text = Text,
            Image = Image,
            ImageWidth = ImageWidth,
            ImageHeight = ImageHeight,
            Font = Font,
            FontSize = FontSize,
            Bold = Bold,
            Italic = Italic
        };
    }
}

public class TodoItem
{
    public bool IsChecked { get; set; }
    public List<ContentElement> Elements { get; } = new();

    public TodoItem() { }

    public TodoItem(string text, bool isChecked = false)
    {
        IsChecked = isChecked;
        if (text.Length > 0)
            Elements.Add(ContentElement.CreateText(text));
    }

    public string PlainText
    {
        get
        {
            var sb = new StringBuilder();
            foreach (var el in Elements)
            {
                if (el.Type == ContentElementType.Text)
                    sb.Append(el.Text);
                else
                    sb.Append('￼'); // object replacement character for images
            }
            return sb.ToString();
        }
    }

    public int TextLength
    {
        get
        {
            int len = 0;
            foreach (var el in Elements)
                len += el.Type == ContentElementType.Text ? el.Text.Length : 1;
            return len;
        }
    }

    public (int elementIndex, int offsetInElement) ResolveOffset(int globalOffset)
    {
        int pos = 0;
        for (int i = 0; i < Elements.Count; i++)
        {
            int elLen = Elements[i].Type == ContentElementType.Text ? Elements[i].Text.Length : 1;
            bool isLast = i == Elements.Count - 1;
            if (Elements[i].Type == ContentElementType.Text)
            {
                if (globalOffset <= pos + elLen)
                    return (i, globalOffset - pos);
            }
            else
            {
                if (globalOffset < pos + elLen || (isLast && globalOffset <= pos + elLen))
                    return (i, globalOffset - pos);
            }
            pos += elLen;
        }
        return (Math.Max(0, Elements.Count - 1), Elements.Count > 0
            ? (Elements[^1].Type == ContentElementType.Text ? Elements[^1].Text.Length : 1)
            : 0);
    }

    public int GlobalOffset(int elementIndex, int offsetInElement)
    {
        int pos = 0;
        for (int i = 0; i < elementIndex && i < Elements.Count; i++)
            pos += Elements[i].Type == ContentElementType.Text ? Elements[i].Text.Length : 1;
        return pos + offsetInElement;
    }
}

public class TodoDocument
{
    public ObservableCollection<TodoItem> Items { get; } = new();
    public FontFamily DefaultFont { get; set; } = FontFamily.Default;
    public double DefaultFontSize { get; set; } = 14;

    public TodoDocument()
    {
        Items.Add(new TodoItem());
    }

    public string GetAllText()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Items.Count; i++)
        {
            if (i > 0) sb.AppendLine();
            sb.Append(Items[i].PlainText);
        }
        return sb.ToString();
    }
}
