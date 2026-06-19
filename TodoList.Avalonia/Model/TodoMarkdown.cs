using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList.Avalonia.Model;

public static class TodoMarkdown
{
    private static readonly Regex CheckboxPattern =
        new(@"^- \[([ xX])\] (.*)", RegexOptions.Compiled);

    public static List<TodoItemData> ParseMarkdown(string? markdown)
    {
        var result = new List<TodoItemData>();
        if (string.IsNullOrEmpty(markdown)) return result;

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length == 0) continue;

            var match = CheckboxPattern.Match(line);
            if (match.Success)
            {
                bool isChecked = match.Groups[1].Value != " ";
                result.Add(new TodoItemData(match.Groups[2].Value, isChecked));
            }
            else
            {
                result.Add(new TodoItemData(line));
            }
        }
        return result;
    }

    public static string ToMarkdown(IEnumerable<TodoItemData>? items)
    {
        if (items == null) return string.Empty;
        var sb = new StringBuilder();
        bool first = true;
        foreach (var item in items)
        {
            if (!first) sb.Append('\n');
            first = false;
            sb.Append(item.IsChecked ? "- [x] " : "- [ ] ");
            sb.Append(item.Text);
        }
        return sb.ToString();
    }
}
