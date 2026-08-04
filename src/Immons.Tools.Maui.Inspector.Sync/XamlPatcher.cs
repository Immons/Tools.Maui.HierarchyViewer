using System.Text.RegularExpressions;

namespace Immons.Tools.Maui.Inspector.Sync;

/// <summary>A single edit reported by the app: XAML location + attribute + new value (or removal).</summary>
public sealed record XamlChange(string Source, int Line, int Column, string Element, string Attribute, string Value, bool Remove = false);

/// <summary>
/// Applies changes to XAML files by plain-text edits (no reformatting): finds the opening tag
/// at the reported line/column, verifies the element name matches, then replaces or inserts
/// the attribute. Already-applied changes are naturally idempotent.
/// </summary>
public sealed class XamlPatcher
{
    readonly string _root;
    readonly bool _dryRun;
    readonly Dictionary<string, string?> _resolvedFiles = [];
    readonly HashSet<string> _appliedKeys = [];

    public XamlPatcher(string root, bool dryRun)
    {
        _root = root;
        _dryRun = dryRun;
    }

    public void Apply(XamlChange change)
    {
        // "Views/Foo.xaml;assembly=MyApp" → relative path
        var relativePath = change.Source.Split(';')[0].TrimStart('/');
        var key = $"{relativePath}:{change.Line}:{change.Column}|{change.Attribute}={(change.Remove ? "\0removed" : change.Value)}";
        if (!_appliedKeys.Add(key))
            return; // same value already applied this session

        var file = ResolveFile(relativePath);
        if (file == null)
        {
            Warn($"{relativePath}: file not found under {_root}");
            return;
        }

        string text;
        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception ex)
        {
            Warn($"{relativePath}: {ex.Message}");
            return;
        }

        var patched = Patch(text, change, out var message);
        if (patched == null)
        {
            Warn($"{relativePath}:{change.Line} {message}");
            return;
        }

        if (patched == text)
        {
            Info($"{relativePath}:{change.Line} {change.Element}.{change.Attribute} already = {change.Value}");
            return;
        }

        if (!_dryRun)
        {
            try
            {
                File.WriteAllText(file, patched);
            }
            catch (Exception ex)
            {
                Warn($"{relativePath}: write failed: {ex.Message}");
                return;
            }
        }

        Console.ForegroundColor = ConsoleColor.Green;
        var what = change.Remove ? "removed" : $"= \"{change.Value}\"";
        Console.WriteLine($"✔ {relativePath}:{change.Line}  {change.Element}.{change.Attribute} {what}{(_dryRun ? "  (dry run)" : "")}");
        Console.ResetColor();
    }

    /// <summary>Returns the patched text, null with a message when the change cannot be applied safely.</summary>
    internal static string? Patch(string text, XamlChange change, out string message)
    {
        message = "";

        var offset = OffsetOf(text, change.Line, change.Column);
        if (offset < 0)
        {
            message = "line/column out of range (file changed since the app was built? restart the app)";
            return null;
        }

        // Line info points at the element name (just after '<'). Verify it to catch stale locations.
        var nameMatch = Regex.Match(text[offset..Math.Min(text.Length, offset + 160)],
            @"^([A-Za-z_][\w]*:)?([A-Za-z_][\w.]*)");
        if (!nameMatch.Success || nameMatch.Groups[2].Value != change.Element)
        {
            message = $"expected <{change.Element}> here but found \"{Snippet(text, offset)}\" — restart the app after editing XAML by hand";
            return null;
        }

        var tagEnd = FindTagEnd(text, offset);
        if (tagEnd < 0)
        {
            message = "could not find the end of the opening tag";
            return null;
        }

        var tag = text[offset..tagEnd];
        var value = EscapeAttributeValue(change.Value);
        var attrPattern = new Regex($@"(\s{Regex.Escape(change.Attribute)}\s*=\s*)(""[^""]*""|'[^']*')");

        if (change.Remove)
        {
            // Attribute on its own line disappears with the whole line; inline only with its spacing.
            var removal = Regex.Replace(tag,
                $@"(\r?\n[ \t]*|[ \t]+){Regex.Escape(change.Attribute)}\s*=\s*(""[^""]*""|'[^']*')", "");
            return text[..offset] + removal + text[tagEnd..];
        }

        string newTag;
        var existing = attrPattern.Match(tag);
        if (existing.Success)
        {
            var quote = existing.Groups[2].Value[0];
            newTag = tag[..existing.Groups[2].Index] + quote + value + quote
                     + tag[(existing.Groups[2].Index + existing.Groups[2].Length)..];
        }
        else
        {
            // Insert right after the element name — always safe, keeps the tag's own formatting.
            var nameLength = nameMatch.Length;
            newTag = tag[..nameLength] + $" {change.Attribute}=\"{value}\"" + tag[nameLength..];
        }

        return text[..offset] + newTag + text[tagEnd..];
    }

    static int OffsetOf(string text, int line, int column)
    {
        var currentLine = 1;
        var offset = 0;
        while (currentLine < line)
        {
            var next = text.IndexOf('\n', offset);
            if (next < 0)
                return -1;
            offset = next + 1;
            currentLine++;
        }
        var result = offset + column - 1;
        return result <= text.Length ? result : -1;
    }

    /// <summary>Index just past the opening tag's closing '>', quote-aware.</summary>
    static int FindTagEnd(string text, int start)
    {
        char? quote = null;
        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];
            if (quote != null)
            {
                if (c == quote)
                    quote = null;
            }
            else if (c is '"' or '\'')
            {
                quote = c;
            }
            else if (c == '>')
            {
                return i + 1;
            }
        }
        return -1;
    }

    static string EscapeAttributeValue(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace("\"", "&quot;");

    static string Snippet(string text, int offset)
    {
        var end = Math.Min(text.Length, offset + 24);
        return text[offset..end].Replace('\n', ' ').Replace('\r', ' ').Trim();
    }

    string? ResolveFile(string relativePath)
    {
        if (_resolvedFiles.TryGetValue(relativePath, out var cached))
            return cached;

        string? result = null;
        var direct = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(direct))
        {
            result = direct;
        }
        else
        {
            var fileName = Path.GetFileName(relativePath);
            var matches = Directory.EnumerateFiles(_root, fileName, SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                            && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .Where(p => p.Replace('\\', '/').EndsWith(relativePath, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();

            if (matches.Count == 1)
                result = matches[0];
            else if (matches.Count > 1)
                Warn($"{relativePath}: multiple matches found, skipping (narrow --src)");
        }

        _resolvedFiles[relativePath] = result;
        return result;
    }

    static void Info(string message) => Console.WriteLine($"  {message}");

    static void Warn(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {message}");
        Console.ResetColor();
    }
}
