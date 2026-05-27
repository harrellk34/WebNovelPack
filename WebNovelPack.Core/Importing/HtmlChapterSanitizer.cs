using HtmlAgilityPack;

namespace WebNovelPack.Core.Importing;

internal static class HtmlChapterSanitizer
{
    private static readonly HashSet<string> RemovedElementNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "script",
        "iframe",
        "object",
        "embed",
        "style",
        "link",
        "meta",
        "form",
        "input",
        "button"
    };

    private static readonly HashSet<string> RemovedAttributeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "style",
        "srcdoc"
    };

    public static HtmlSanitizationResult Sanitize(HtmlDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        int removedNodeCount = RemoveUnsafeElements(document.DocumentNode);
        int removedAttributeCount = RemoveUnsafeAttributes(document.DocumentNode);

        return new HtmlSanitizationResult(removedNodeCount, removedAttributeCount);
    }

    private static int RemoveUnsafeElements(HtmlNode root)
    {
        var nodes = root
            .Descendants()
            .Where(node => RemovedElementNames.Contains(node.Name))
            .ToList();

        foreach (var node in nodes)
        {
            node.Remove();
        }

        return nodes.Count;
    }

    private static int RemoveUnsafeAttributes(HtmlNode root)
    {
        int removedCount = 0;

        foreach (var node in root.DescendantsAndSelf().Where(node => node.HasAttributes))
        {
            foreach (var attribute in node.Attributes.ToList())
            {
                if (ShouldRemoveAttribute(attribute))
                {
                    node.Attributes.Remove(attribute);
                    removedCount++;
                }
            }
        }

        return removedCount;
    }

    private static bool ShouldRemoveAttribute(HtmlAttribute attribute)
    {
        if (attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (RemovedAttributeNames.Contains(attribute.Name))
        {
            return true;
        }

        return attribute.Name.Equals("href", StringComparison.OrdinalIgnoreCase)
            && IsUnsafeHref(attribute.Value);
    }

    private static bool IsUnsafeHref(string value)
    {
        string trimmed = value.Trim();

        if (trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && uri.Scheme is not ("http" or "https");
    }
}

internal sealed record HtmlSanitizationResult(int RemovedNodeCount, int RemovedAttributeCount)
{
    public int TotalRemoved => RemovedNodeCount + RemovedAttributeCount;
}
