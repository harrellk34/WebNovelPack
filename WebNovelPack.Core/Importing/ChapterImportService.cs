using System.Net;
using HtmlAgilityPack;
using Markdig;
using WebNovelPack.Core.Models;

namespace WebNovelPack.Core.Importing;

public sealed class ChapterImportService
{
    private static readonly string[] SupportedExtensions =
    [
        ".txt",
        ".md",
        ".markdown",
        ".html",
        ".htm"
    ];

    public BookProject ImportFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path cannot be empty.", nameof(folderPath));
        }

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder does not exist: {folderPath}");
        }

        var files = Directory
            .EnumerateFiles(folderPath)
            .Where(IsSupportedFile)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var project = new BookProject();

        for (int i = 0; i < files.Count; i++)
        {
            project.Chapters.Add(ImportChapter(files[i], i + 1));
        }

        return project;
    }

    private static bool IsSupportedFile(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    private static Chapter ImportChapter(string path, int order)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        string rawText = File.ReadAllText(path);

        string title = DetectTitle(rawText, path, extension);
        string htmlBody = extension switch
        {
            ".txt" => PlainTextToHtml(rawText),
            ".md" or ".markdown" => Markdown.ToHtml(rawText),
            ".html" or ".htm" => ExtractReadableHtml(rawText),
            _ => PlainTextToHtml(rawText)
        };

        return new Chapter
        {
            Title = title,
            SourcePath = path,
            HtmlBody = htmlBody,
            Order = order
        };
    }

    private static string DetectTitle(string rawText, string path, string extension)
    {
        if (extension is ".html" or ".htm")
        {
            var document = new HtmlDocument();
            document.LoadHtml(rawText);

            string? heading = document.DocumentNode
                .SelectSingleNode("//h1 | //h2 | //title")
                ?.InnerText;

            if (!string.IsNullOrWhiteSpace(heading))
            {
                return NormalizeWhitespace(WebUtility.HtmlDecode(heading));
            }
        }

        string? firstMeaningfulLine = rawText
            .Split('\n')
            .Select(line => line.Trim().TrimStart('#').Trim())
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));

        if (!string.IsNullOrWhiteSpace(firstMeaningfulLine) && firstMeaningfulLine.Length <= 120)
        {
            return NormalizeWhitespace(firstMeaningfulLine);
        }

        return Path.GetFileNameWithoutExtension(path);
    }

    private static string PlainTextToHtml(string text)
    {
        var paragraphs = text
            .Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(paragraph => paragraph.Trim())
            .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
            .Select(paragraph => $"<p>{WebUtility.HtmlEncode(paragraph)}</p>");

        return string.Join(Environment.NewLine, paragraphs);
    }

    private static string ExtractReadableHtml(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        RemoveNodes(document, "//script|//style|//nav|//footer|//header|//form|//button|//noscript");

        HtmlNode? bestNode = FindBestContentNode(document);

        string cleaned = bestNode?.InnerHtml ?? document.DocumentNode.InnerHtml;

        return NormalizeHtmlFragment(cleaned);
    }

    private static void RemoveNodes(HtmlDocument document, string xpath)
    {
        var nodes = document.DocumentNode.SelectNodes(xpath);

        if (nodes is null)
        {
            return;
        }

        foreach (var node in nodes.ToList())
        {
            node.Remove();
        }
    }

    private static HtmlNode? FindBestContentNode(HtmlDocument document)
    {
        var candidates = document.DocumentNode
            .SelectNodes("//article|//main|//section|//div|//body");

        if (candidates is null)
        {
            return document.DocumentNode.SelectSingleNode("//body");
        }

        return candidates
            .OrderByDescending(ScoreNode)
            .FirstOrDefault();
    }

    private static int ScoreNode(HtmlNode node)
    {
        int paragraphCount = node.SelectNodes(".//p")?.Count ?? 0;
        int headingCount = node.SelectNodes(".//h1|.//h2|.//h3")?.Count ?? 0;
        int linkCount = node.SelectNodes(".//a")?.Count ?? 0;
        int textLength = NormalizeWhitespace(node.InnerText).Length;

        return textLength + (paragraphCount * 100) + (headingCount * 50) - (linkCount * 25);
    }

    private static string NormalizeHtmlFragment(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        RemoveNodes(document, "//script|//style|//nav|//footer|//header|//form|//button|//noscript");

        return document.DocumentNode.InnerHtml.Trim();
    }

    private static string NormalizeWhitespace(string value)
    {
        return string.Join(" ", value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }
}