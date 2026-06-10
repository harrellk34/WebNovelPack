using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WebNovelPack.Core.Importing;

internal static partial class ChapterTitleDetector
{
    public static ChapterTitleDetectionResult Detect(string rawText, string path, string extension, HtmlDocument? sanitizedHtmlDocument = null)
    {
        string? title = extension switch
        {
            ".txt" => DetectPlainTextTitle(rawText),
            ".md" or ".markdown" => DetectMarkdownTitle(rawText),
            ".html" or ".htm" => DetectHtmlTitle(sanitizedHtmlDocument),
            _ => null
        };

        return string.IsNullOrWhiteSpace(title)
            ? new ChapterTitleDetectionResult(FileNameTitleFormatter.Format(path), true)
            : new ChapterTitleDetectionResult(NormalizeWhitespace(title), false);
    }

    private static string? DetectPlainTextTitle(string rawText)
    {
        string? firstLine = rawText
            .Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));

        if (firstLine is null || firstLine.Length > 120)
        {
            return null;
        }

        return PlainTextHeadingRegex().IsMatch(firstLine)
            ? firstLine
            : null;
    }

    private static string? DetectMarkdownTitle(string rawText)
    {
        foreach (string line in rawText.Split('\n'))
        {
            var match = MarkdownHeadingRegex().Match(line.Trim());

            if (match.Success)
            {
                return match.Groups["title"].Value.Trim();
            }
        }

        return null;
    }

    private static string? DetectHtmlTitle(HtmlDocument? sanitizedHtmlDocument)
    {
        string? heading = sanitizedHtmlDocument?
            .DocumentNode
            .SelectSingleNode("//h1 | //h2 | //h3")
            ?.InnerText;

        return string.IsNullOrWhiteSpace(heading)
            ? null
            : WebUtility.HtmlDecode(heading);
    }

    private static string NormalizeWhitespace(string value)
    {
        return string.Join(" ", value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }

    [GeneratedRegex(@"^(chapter|ch\.?|prologue|epilogue|interlude|side story|part|book|volume|act|prelude)\b|^\d+\s*[\.:)\-]", RegexOptions.IgnoreCase)]
    private static partial Regex PlainTextHeadingRegex();

    [GeneratedRegex(@"^#{1,6}\s+(?<title>.+?)\s*#*$")]
    private static partial Regex MarkdownHeadingRegex();
}

internal sealed record ChapterTitleDetectionResult(string Title, bool UsedFilename);
