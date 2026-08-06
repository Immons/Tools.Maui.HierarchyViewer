namespace Immons.Tools.Maui.Inspector.Sync;

/// <summary>
/// Minimal XML-shaped text scanning for element operations: end of an element (self-closing or
/// matching close tag, nesting-aware), comment skipping, qualified-name parsing. Deliberately
/// plain text — the patcher never reformats a file, so it never parses one either.
/// </summary>
internal static class XamlTagScanner
{
    /// <summary>Index just past the opening tag's closing '>', quote-aware; -1 when unterminated.</summary>
    public static int FindTagEnd(string text, int start)
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

    public static bool IsSelfClosing(string text, int tagEnd) =>
        tagEnd >= 2 && text[tagEnd - 2] == '/';

    /// <summary>
    /// Given the offset of the element name (just past '&lt;'), returns the index of the matching
    /// closing tag's '&lt;' and the index just past its '&gt;'. Counts nested same-named elements;
    /// property elements ("Grid.RowDefinitions") do not match "Grid". (-1, -1) when not found.
    /// </summary>
    public static (int CloseStart, int End) FindClosingTag(string text, int nameOffset, string localName)
    {
        var tagEnd = FindTagEnd(text, nameOffset);
        if (tagEnd < 0 || IsSelfClosing(text, tagEnd))
            return (-1, -1);

        var depth = 1;
        var i = tagEnd;
        while (i < text.Length)
        {
            if (text[i] != '<')
            {
                i++;
                continue;
            }

            if (Matches(text, i, "<!--"))
            {
                var endComment = text.IndexOf("-->", i, StringComparison.Ordinal);
                if (endComment < 0)
                    return (-1, -1);
                i = endComment + 3;
                continue;
            }

            var closing = i + 1 < text.Length && text[i + 1] == '/';
            var nameStart = i + (closing ? 2 : 1);
            var qname = ReadQName(text, nameStart);
            var next = FindTagEnd(text, nameStart);
            if (next < 0)
                return (-1, -1);

            // Property elements ("Grid.RowDefinitions") read as their full dotted name and
            // therefore never equal a control's local name.
            if (LocalNameOf(qname) == localName)
            {
                if (closing)
                {
                    depth--;
                    if (depth == 0)
                        return (i, next);
                }
                else if (!IsSelfClosing(text, next))
                {
                    depth++;
                }
            }

            i = next;
        }

        return (-1, -1);
    }

    /// <summary>Reads a qualified element name (letters, digits, '_', '.', ':', '-').</summary>
    public static string ReadQName(string text, int start)
    {
        var end = start;
        while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] is '_' or '.' or ':' or '-'))
            end++;
        return text[start..end];
    }

    public static string LocalNameOf(string qname)
    {
        var colon = qname.LastIndexOf(':');
        return colon >= 0 ? qname[(colon + 1)..] : qname;
    }

    static bool Matches(string text, int index, string token) =>
        index + token.Length <= text.Length && text.AsSpan(index, token.Length).SequenceEqual(token);
}
