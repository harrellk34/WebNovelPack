using System.Net;
using HtmlAgilityPack;
using Markdig;
using WebNovelPack.Core.Models;

namespace WebNovelPack.Core.Importing;

public sealed class ChapterImportService
{
    private const int ShortContentThreshold = 80;
    private const string ContentCleanupXPath = "//script|//style|//nav|//footer|//header";
    private const string ExtraCleanupXPath = "//form|//button|//noscript";

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
        return ImportFolderWithResult(folderPath).Project;
    }

    public ImportResult ImportFolderWithResult(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path cannot be empty.", nameof(folderPath));
        }

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder does not exist: {folderPath}");
        }

        var allFiles = Directory.EnumerateFiles(folderPath);

        return ImportFilesWithResult(allFiles, folderPath);
    }

    public ImportResult ImportFilesWithResult(IEnumerable<string> filePaths)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        return ImportFilesWithResult(filePaths, null);
    }

    private ImportResult ImportFilesWithResult(IEnumerable<string> filePaths, string? noProcessedFilesSourcePath)
    {
        var files = filePaths
            .Order(NaturalFileNameComparer.OrdinalIgnoreCase)
            .ToList();
        int supportedFileCount = files.Count(IsSupportedFile);
        var project = new BookProject();
        var warnings = new List<ImportWarning>();
        var auditLog = new List<ImportAuditEvent>();
        var report = new PackagingReport
        {
            TotalFilesDiscovered = files.Count
        };
        var processedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddAuditEvent(
            auditLog,
            ImportAuditEventType.ImportStarted,
            ImportAuditSeverity.Info,
            $"Import started for {files.Count} file(s).");

        foreach (string file in files)
        {
            var validation = ValidateFile(file, processedFileNames);

            if (!validation.IsValid)
            {
                AddAuditEvent(
                    auditLog,
                    ImportAuditEventType.FileValidated,
                    ImportAuditSeverity.Warning,
                    $"File validation failed: {validation.Reason}",
                    file);

                AddSkippedFile(report, warnings, auditLog, file, validation.Reason!, ImportAuditSeverity.Warning);
                continue;
            }

            AddAuditEvent(
                auditLog,
                ImportAuditEventType.FileValidated,
                ImportAuditSeverity.Info,
                "File validated successfully.",
                file);

            try
            {
                project.Chapters.Add(ImportChapter(file, project.Chapters.Count + 1, validation.RawText!, warnings, auditLog));
                processedFileNames.Add(Path.GetFileName(file));
                report.SuccessfullyProcessed++;

                AddAuditEvent(
                    auditLog,
                    ImportAuditEventType.ChapterImported,
                    ImportAuditSeverity.Info,
                    "Chapter imported successfully.",
                    file);
            }
            catch (Exception ex)
            {
                AddSkippedFile(
                    report,
                    warnings,
                    auditLog,
                    file,
                    $"File could not be processed: {ex.Message}",
                    ImportAuditSeverity.Error);
            }
        }

        if (report.SuccessfullyProcessed == 0)
        {
            warnings.Add(new ImportWarning
            {
                Message = supportedFileCount == 0
                    ? "No supported chapter files found."
                    : "No valid chapter files were processed.",
                SourcePath = noProcessedFilesSourcePath
            });
        }

        AddAuditEvent(
            auditLog,
            ImportAuditEventType.ImportCompleted,
            ImportAuditSeverity.Info,
            $"Import completed with {report.SuccessfullyProcessed} imported and {report.SkippedCount} skipped file(s).");

        var preview = BuildPreview(project, report);

        return new ImportResult
        {
            Project = project,
            Warnings = warnings,
            Report = report,
            Preview = preview,
            AuditLog = auditLog,
            SupportedFileCount = supportedFileCount,
            SkippedFileCount = report.SkippedCount
        };
    }

    private static FileValidationResult ValidateFile(string path, HashSet<string> processedFileNames)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return FileValidationResult.Invalid("File does not exist.");
        }

        if (!IsSupportedFile(path))
        {
            return FileValidationResult.Invalid("Unsupported file extension.");
        }

        if (processedFileNames.Contains(Path.GetFileName(path)))
        {
            return FileValidationResult.Invalid("Duplicate file name.");
        }

        try
        {
            string rawText = File.ReadAllText(path);

            if (string.IsNullOrWhiteSpace(rawText))
            {
                return FileValidationResult.Invalid("File is empty or contains only whitespace.");
            }

            return FileValidationResult.Valid(rawText);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            return FileValidationResult.Invalid($"File could not be read: {ex.Message}");
        }
    }

    private static void AddSkippedFile(
        PackagingReport report,
        List<ImportWarning> warnings,
        List<ImportAuditEvent> auditLog,
        string path,
        string reason,
        ImportAuditSeverity severity)
    {
        report.SkippedFiles.Add(new SkippedFileReport
        {
            SourcePath = path,
            Reason = reason
        });

        warnings.Add(new ImportWarning
        {
            Message = $"File skipped: {reason}",
            SourcePath = path
        });

        AddAuditEvent(
            auditLog,
            ImportAuditEventType.FileSkipped,
            severity,
            $"File skipped: {reason}",
            path);
    }

    private static bool IsSupportedFile(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    private static Chapter ImportChapter(
        string path,
        int order,
        string rawText,
        List<ImportWarning> warnings,
        List<ImportAuditEvent> auditLog)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();

        HtmlDocument? sanitizedHtmlDocument = null;
        string htmlBody = extension switch
        {
            ".txt" => PlainTextToHtml(rawText),
            ".md" or ".markdown" => Markdown.ToHtml(rawText),
            ".html" or ".htm" => ExtractReadableHtml(rawText, path, warnings, auditLog, out sanitizedHtmlDocument),
            _ => PlainTextToHtml(rawText)
        };
        var titleResult = ChapterTitleDetector.Detect(rawText, path, extension, sanitizedHtmlDocument);

        AddAuditEvent(
            auditLog,
            ImportAuditEventType.ChapterTitleDetected,
            ImportAuditSeverity.Info,
            titleResult.UsedFilename
                ? "No clear chapter title detected; used the filename."
                : "Chapter title detected.",
            path);

        if (GetReadableTextLength(rawText, extension) < ShortContentThreshold)
        {
            warnings.Add(new ImportWarning
            {
                Message = "Imported chapter has very short content.",
                SourcePath = path
            });
        }

        if (titleResult.UsedFilename)
        {
            warnings.Add(new ImportWarning
            {
                Message = "Imported chapter has no clear title; used the filename.",
                SourcePath = path
            });
        }

        return new Chapter
        {
            Title = titleResult.Title,
            SourcePath = path,
            HtmlBody = htmlBody,
            Order = order,
            TitleSource = titleResult.UsedFilename
                ? ChapterTitleSource.FilenameFallback
                : ChapterTitleSource.DetectedContent
        };
    }

    private static ImportPreviewSummary BuildPreview(BookProject project, PackagingReport report)
    {
        return new ImportPreviewSummary
        {
            ImportedChapters = project.Chapters
                .OrderBy(chapter => chapter.Order)
                .Select(chapter => new ImportPreviewItem
                {
                    Order = chapter.Order,
                    Title = chapter.Title,
                    SourcePath = chapter.SourcePath,
                    OriginalFileName = Path.GetFileName(chapter.SourcePath),
                    SourceFormat = Path.GetExtension(chapter.SourcePath).ToLowerInvariant(),
                    TitleSource = chapter.TitleSource
                })
                .ToList(),
            SkippedFiles = report.SkippedFiles.ToList()
        };
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

    private static string ExtractReadableHtml(
        string html,
        string path,
        List<ImportWarning> warnings,
        List<ImportAuditEvent> auditLog,
        out HtmlDocument sanitizedDocument)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        int removedNodeCount = RemoveNodes(document, ContentCleanupXPath);
        var sanitization = HtmlChapterSanitizer.Sanitize(document);
        RemoveNodes(document, ExtraCleanupXPath);
        sanitizedDocument = document;

        HtmlNode? bestNode = FindBestContentNode(document);

        string cleaned = bestNode?.InnerHtml ?? document.DocumentNode.InnerHtml;

        var normalized = NormalizeHtmlFragment(cleaned);
        removedNodeCount += normalized.RemovedNodeCount;
        removedNodeCount += sanitization.TotalRemoved;

        AddAuditEvent(
            auditLog,
            ImportAuditEventType.HtmlSanitized,
            ImportAuditSeverity.Info,
            removedNodeCount > 0
                ? $"HTML sanitized; removed {removedNodeCount} unsafe or non-reading item(s)."
                : "HTML sanitized; no unsafe or non-reading items were removed.",
            path);

        if (removedNodeCount > 0)
        {
            warnings.Add(new ImportWarning
            {
                Message = "HTML chapter had navigation/script/style/header/footer nodes removed.",
                SourcePath = path
            });
        }

        return normalized.Html;
    }

    private static void AddAuditEvent(
        List<ImportAuditEvent> auditLog,
        ImportAuditEventType eventType,
        ImportAuditSeverity severity,
        string message,
        string? sourcePath = null)
    {
        auditLog.Add(new ImportAuditEvent
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventType = eventType,
            Severity = severity,
            Message = message,
            SourcePath = sourcePath
        });
    }

    private static int RemoveNodes(HtmlDocument document, string xpath)
    {
        var nodes = document.DocumentNode.SelectNodes(xpath);

        if (nodes is null)
        {
            return 0;
        }

        int count = nodes.Count;

        foreach (var node in nodes.ToList())
        {
            node.Remove();
        }

        return count;
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

    private static HtmlExtractionResult NormalizeHtmlFragment(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        int removedNodeCount = RemoveNodes(document, ContentCleanupXPath);
        var sanitization = HtmlChapterSanitizer.Sanitize(document);
        RemoveNodes(document, ExtraCleanupXPath);

        return new HtmlExtractionResult(document.DocumentNode.InnerHtml.Trim(), removedNodeCount + sanitization.TotalRemoved);
    }

    private static string NormalizeWhitespace(string value)
    {
        return string.Join(" ", value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }

    private static int GetReadableTextLength(string rawText, string extension)
    {
        if (extension is ".html" or ".htm")
        {
            var document = new HtmlDocument();
            document.LoadHtml(rawText);

            RemoveNodes(document, ContentCleanupXPath);
            HtmlChapterSanitizer.Sanitize(document);
            RemoveNodes(document, ExtraCleanupXPath);

            return NormalizeWhitespace(WebUtility.HtmlDecode(document.DocumentNode.InnerText)).Length;
        }

        return NormalizeWhitespace(rawText).Length;
    }

    private sealed record HtmlExtractionResult(string Html, int RemovedNodeCount);

    private sealed record FileValidationResult(bool IsValid, string? RawText, string? Reason)
    {
        public static FileValidationResult Valid(string rawText) => new(true, rawText, null);

        public static FileValidationResult Invalid(string reason) => new(false, null, reason);
    }
}
