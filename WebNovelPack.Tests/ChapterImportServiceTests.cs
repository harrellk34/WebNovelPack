using WebNovelPack.Core.Importing;
using WebNovelPack.Core.Models;

namespace WebNovelPack.Tests;

public sealed class ChapterImportServiceTests
{
    [Fact]
    public void ImportFolder_ShouldImportSupportedChapterFilesInFilenameOrder()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            File.WriteAllText(
                Path.Combine(tempFolder, "001.html"),
                """
                <html>
                    <head><title>Chapter 1: Start</title></head>
                    <body>
                        <nav>Previous | Next</nav>
                        <main>
                            <h1>Chapter 1: Start</h1>
                            <p>The story begins here.</p>
                        </main>
                    </body>
                </html>
                """
            );

            File.WriteAllText(
                Path.Combine(tempFolder, "002.md"),
                """
                # Chapter 2: Road

                The road continues.
                """
            );

            File.WriteAllText(
                Path.Combine(tempFolder, "003.txt"),
                """
                Chapter 3: Night

                Night falls over the city.
                """
            );

            var service = new ChapterImportService();

            var result = service.ImportFolderWithResult(tempFolder);
            var project = result.Project;

            Assert.Equal(3, project.Chapters.Count);
            Assert.Equal(3, result.Report.TotalFilesDiscovered);
            Assert.Equal(3, result.Report.SuccessfullyProcessed);
            Assert.Equal(0, result.Report.SkippedCount);
            Assert.Contains("Chapter 1", project.Chapters[0].Title);
            Assert.Contains("Chapter 2", project.Chapters[1].Title);
            Assert.Contains("Chapter 3", project.Chapters[2].Title);

            Assert.Contains("The story begins here.", project.Chapters[0].HtmlBody);
            Assert.DoesNotContain("Previous | Next", project.Chapters[0].HtmlBody);
            Assert.Contains("<h1", project.Chapters[1].HtmlBody);
            Assert.Contains("<p>", project.Chapters[2].HtmlBody);
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    [Fact]
    public void ImportFolderWithResult_ShouldSkipUnsupportedFiles()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            File.WriteAllText(Path.Combine(tempFolder, "001.txt"), "Chapter 1\n\nThis chapter has enough text to avoid the short content warning.");
            File.WriteAllText(Path.Combine(tempFolder, "cover.jpg"), "not a chapter");

            var service = new ChapterImportService();

            var result = service.ImportFolderWithResult(tempFolder);

            Assert.Single(result.Project.Chapters);
            Assert.Equal(1, result.SupportedFileCount);
            Assert.Equal(1, result.SkippedFileCount);
            Assert.Equal(2, result.Report.TotalFilesDiscovered);
            Assert.Equal("cover.jpg", result.Report.SkippedFiles.Single().FileName);
            Assert.Contains("Unsupported file extension", result.Report.SkippedFiles.Single().Reason);
            Assert.Contains(result.Warnings, warning => warning.Message.Contains("Unsupported file"));
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    [Fact]
    public void ImportFolderWithResult_ShouldSkipEmptyFiles()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            File.WriteAllText(Path.Combine(tempFolder, "empty.txt"), "");

            var service = new ChapterImportService();

            var result = service.ImportFolderWithResult(tempFolder);

            Assert.Empty(result.Project.Chapters);
            Assert.Equal(1, result.Report.TotalFilesDiscovered);
            Assert.Equal(1, result.Report.SkippedCount);
            Assert.Contains("empty", result.Report.SkippedFiles.Single().Reason, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    [Fact]
    public void ImportFilesWithResult_ShouldReportMissingFileWithoutThrowing()
    {
        string missingFile = Path.Combine(Path.GetTempPath(), $"webnovelpack-missing-{Guid.NewGuid():N}.txt");
        var service = new ChapterImportService();

        var result = service.ImportFilesWithResult([missingFile]);

        Assert.Empty(result.Project.Chapters);
        Assert.Equal(1, result.Report.TotalFilesDiscovered);
        Assert.Equal(1, result.Report.SkippedCount);
        Assert.Equal(Path.GetFileName(missingFile), result.Report.SkippedFiles.Single().FileName);
        Assert.Contains("does not exist", result.Report.SkippedFiles.Single().Reason);
    }

    [Fact]
    public void ImportFolderWithResult_ShouldProcessValidFilesWhenOtherFilesAreInvalid()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            File.WriteAllText(Path.Combine(tempFolder, "001.txt"), "Chapter 1\n\nThis is a valid chapter body for an imported story.");
            File.WriteAllText(Path.Combine(tempFolder, "002.txt"), "");
            File.WriteAllText(Path.Combine(tempFolder, "notes.csv"), "not a chapter");

            var service = new ChapterImportService();

            var result = service.ImportFolderWithResult(tempFolder);

            Assert.Single(result.Project.Chapters);
            Assert.Equal(3, result.Report.TotalFilesDiscovered);
            Assert.Equal(1, result.Report.SuccessfullyProcessed);
            Assert.Equal(2, result.Report.SkippedCount);
            Assert.Contains(result.Report.SkippedFiles, skipped => skipped.FileName == "002.txt");
            Assert.Contains(result.Report.SkippedFiles, skipped => skipped.FileName == "notes.csv");
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    [Fact]
    public void ImportFilesWithResult_ShouldRecordImportStartedAndCompletedAuditEvents()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            string filePath = Path.Combine(tempFolder, "001.txt");
            File.WriteAllText(filePath, "Chapter One\n\nThis is a complete enough chapter for import.");

            var service = new ChapterImportService();

            var result = service.ImportFilesWithResult([filePath]);

            Assert.Equal(ImportAuditEventType.ImportStarted, result.AuditLog.First().EventType);
            Assert.Equal(ImportAuditSeverity.Info, result.AuditLog.First().Severity);
            Assert.Equal(ImportAuditEventType.ImportCompleted, result.AuditLog.Last().EventType);
            Assert.Equal(ImportAuditSeverity.Info, result.AuditLog.Last().Severity);
            Assert.All(result.AuditLog, auditEvent => Assert.NotEqual(default, auditEvent.Timestamp));
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    [Fact]
    public void ImportFolderWithResult_ShouldRecordWarningAuditEventForSkippedFiles()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            File.WriteAllText(Path.Combine(tempFolder, "001.txt"), "Chapter One\n\nThis chapter should import cleanly.");
            File.WriteAllText(Path.Combine(tempFolder, "cover.jpg"), "not a chapter");

            var service = new ChapterImportService();

            var result = service.ImportFolderWithResult(tempFolder);

            var skippedEvent = Assert.Single(
                result.AuditLog,
                auditEvent => auditEvent.EventType == ImportAuditEventType.FileSkipped);
            Assert.Equal(ImportAuditSeverity.Warning, skippedEvent.Severity);
            Assert.Equal("cover.jpg", skippedEvent.FileName);
            Assert.Contains("Unsupported file extension", skippedEvent.Message);
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    [Fact]
    public void ImportFilesWithResult_ShouldRecordInfoAuditEventForSuccessfulImports()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            string filePath = Path.Combine(tempFolder, "001.txt");
            File.WriteAllText(filePath, "Chapter One\n\nThis chapter should import cleanly.");

            var service = new ChapterImportService();

            var result = service.ImportFilesWithResult([filePath]);

            var importedEvent = Assert.Single(
                result.AuditLog,
                auditEvent => auditEvent.EventType == ImportAuditEventType.ChapterImported);
            Assert.Equal(ImportAuditSeverity.Info, importedEvent.Severity);
            Assert.Equal("001.txt", importedEvent.FileName);
            Assert.Contains("imported", importedEvent.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    [Fact]
    public void ImportFilesWithResult_ShouldRecordHtmlSanitizationAuditEvent()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            string filePath = Path.Combine(tempFolder, "001.html");
            File.WriteAllText(
                filePath,
                """
                <main>
                    <h1>Chapter One</h1>
                    <p>The chapter text remains.</p>
                    <script>alert('unsafe');</script>
                </main>
                """);

            var service = new ChapterImportService();

            var result = service.ImportFilesWithResult([filePath]);

            var sanitizedEvent = Assert.Single(
                result.AuditLog,
                auditEvent => auditEvent.EventType == ImportAuditEventType.HtmlSanitized);
            Assert.Equal(ImportAuditSeverity.Info, sanitizedEvent.Severity);
            Assert.Equal("001.html", sanitizedEvent.FileName);
            Assert.Contains("HTML sanitized", sanitizedEvent.Message);
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    [Fact]
    public void ImportFolderWithResult_ShouldRecordUsefulAuditTrailForMixedImports()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            File.WriteAllText(Path.Combine(tempFolder, "001.txt"), "Chapter One\n\nThis is a valid chapter body for an imported story.");
            File.WriteAllText(Path.Combine(tempFolder, "002.txt"), "");
            File.WriteAllText(Path.Combine(tempFolder, "003.html"), "<main><h1>Chapter Three</h1><p>HTML chapter.</p><script>bad()</script></main>");
            File.WriteAllText(Path.Combine(tempFolder, "notes.csv"), "not a chapter");

            var service = new ChapterImportService();

            var result = service.ImportFolderWithResult(tempFolder);

            Assert.Equal(2, result.Project.Chapters.Count);
            Assert.Equal(2, result.Report.SkippedCount);
            Assert.Contains(result.AuditLog, auditEvent => auditEvent.EventType == ImportAuditEventType.ImportStarted);
            Assert.Contains(result.AuditLog, auditEvent => auditEvent.EventType == ImportAuditEventType.FileValidated && auditEvent.FileName == "001.txt" && auditEvent.Severity == ImportAuditSeverity.Info);
            Assert.Contains(result.AuditLog, auditEvent => auditEvent.EventType == ImportAuditEventType.FileSkipped && auditEvent.FileName == "002.txt" && auditEvent.Severity == ImportAuditSeverity.Warning);
            Assert.Contains(result.AuditLog, auditEvent => auditEvent.EventType == ImportAuditEventType.FileSkipped && auditEvent.FileName == "notes.csv" && auditEvent.Severity == ImportAuditSeverity.Warning);
            Assert.Contains(result.AuditLog, auditEvent => auditEvent.EventType == ImportAuditEventType.ChapterImported && auditEvent.FileName == "003.html");
            Assert.Contains(result.AuditLog, auditEvent => auditEvent.EventType == ImportAuditEventType.HtmlSanitized && auditEvent.FileName == "003.html");
            Assert.Equal(ImportAuditEventType.ImportCompleted, result.AuditLog.Last().EventType);
            Assert.Contains("2 imported and 2 skipped", result.AuditLog.Last().Message);
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    [Fact]
    public void ImportFilesWithResult_ShouldSkipDuplicateFileNames()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        string firstFolder = Path.Combine(tempFolder, "first");
        string secondFolder = Path.Combine(tempFolder, "second");
        Directory.CreateDirectory(firstFolder);
        Directory.CreateDirectory(secondFolder);

        try
        {
            string firstFile = Path.Combine(firstFolder, "chapter.txt");
            string secondFile = Path.Combine(secondFolder, "chapter.txt");
            File.WriteAllText(firstFile, "Chapter One\n\nThis is the chapter retained for processing.");
            File.WriteAllText(secondFile, "Chapter Two\n\nThis chapter has a colliding filename.");

            var service = new ChapterImportService();

            var result = service.ImportFilesWithResult([firstFile, secondFile]);

            Assert.Single(result.Project.Chapters);
            Assert.Equal(1, result.Report.SkippedCount);
            Assert.Contains("Duplicate file name", result.Report.SkippedFiles.Single().Reason);
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    [Fact]
    public void ImportFolderWithResult_ShouldWarnWhenFolderHasNoSupportedFiles()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            var service = new ChapterImportService();

            var result = service.ImportFolderWithResult(tempFolder);

            Assert.Empty(result.Project.Chapters);
            Assert.Equal(0, result.SupportedFileCount);
            Assert.Contains(result.Warnings, warning => warning.Message.Contains("No supported chapter files"));
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    [Fact]
    public void ImportFolderWithResult_ShouldWarnWhenChapterContentIsVeryShort()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            File.WriteAllText(Path.Combine(tempFolder, "001.txt"), "Tiny.");

            var service = new ChapterImportService();

            var result = service.ImportFolderWithResult(tempFolder);

            Assert.Single(result.Project.Chapters);
            Assert.Contains(result.Warnings, warning => warning.Message.Contains("very short content"));
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    [Fact]
    public void ImportFilesWithResult_ShouldRemoveScriptTagsFromHtmlImports()
    {
        string htmlBody = ImportSingleChapterBody(
            ".html",
            """
            <main>
                <h1>Chapter One</h1>
                <p>The chapter text remains.</p>
                <script>alert('unsafe');</script>
            </main>
            """
        );

        Assert.Contains("The chapter text remains.", htmlBody);
        Assert.DoesNotContain("<script", htmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert('unsafe')", htmlBody);
    }

    [Fact]
    public void ImportFilesWithResult_ShouldRemoveEmbeddedHtmlContent()
    {
        string htmlBody = ImportSingleChapterBody(
            ".html",
            """
            <main>
                <h1>Chapter One</h1>
                <p>Only the story should remain.</p>
                <iframe src="https://example.com/embed">frame text</iframe>
                <embed src="clip.swf">
                <object data="clip.swf">object text</object>
                <style>p { display: none; }</style>
            </main>
            """
        );

        Assert.Contains("Only the story should remain.", htmlBody);
        Assert.DoesNotContain("<iframe", htmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<embed", htmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<object", htmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<style", htmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("frame text", htmlBody);
        Assert.DoesNotContain("object text", htmlBody);
    }

    [Fact]
    public void ImportFilesWithResult_ShouldRemoveUnsafeHtmlAttributes()
    {
        string htmlBody = ImportSingleChapterBody(
            ".html",
            """
            <main>
                <h1 onmouseover="hover()">Chapter One</h1>
                <p onclick="openDoor()" onerror="fail()" onload="ready()" style="color:red" srcdoc="<p>bad</p>">
                    The paragraph stays readable.
                </p>
            </main>
            """
        );

        Assert.Contains("The paragraph stays readable.", htmlBody);
        Assert.DoesNotContain("onclick", htmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", htmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onload", htmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onmouseover", htmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", htmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("srcdoc", htmlBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportFilesWithResult_ShouldPreserveNormalHtmlReadingContent()
    {
        string htmlBody = ImportSingleChapterBody(
            ".html",
            """
            <main>
                <h1>Chapter One</h1>
                <p>The <strong>chapter</strong> keeps <em>emphasis</em><br>and breaks.</p>
                <blockquote>A quoted line.</blockquote>
                <ol><li>First step</li></ol>
                <ul><li>Second step</li></ul>
                <p><a href="https://example.com">Source</a></p>
            </main>
            """
        );

        Assert.Contains("<h1>Chapter One</h1>", htmlBody);
        Assert.Contains("<p>The <strong>chapter</strong> keeps <em>emphasis</em><br>and breaks.</p>", htmlBody);
        Assert.Contains("<blockquote>A quoted line.</blockquote>", htmlBody);
        Assert.Contains("<ol><li>First step</li></ol>", htmlBody);
        Assert.Contains("<ul><li>Second step</li></ul>", htmlBody);
        Assert.Contains("""<a href="https://example.com">Source</a>""", htmlBody);
    }

    [Fact]
    public void ImportFilesWithResult_ShouldRemoveUnsafeJavascriptLinksFromHtmlImports()
    {
        string htmlBody = ImportSingleChapterBody(
            ".html",
            """
            <main>
                <h1>Chapter One</h1>
                <p><a href="javascript:alert('unsafe')">Bad link</a></p>
                <p><a href="https://example.com/story">Good link</a></p>
            </main>
            """
        );

        Assert.Contains(">Bad link</a>", htmlBody);
        Assert.DoesNotContain("javascript:", htmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("""<a href="https://example.com/story">Good link</a>""", htmlBody);
    }

    [Fact]
    public void ImportFilesWithResult_ShouldNotApplyHtmlSanitizationToTxtOrMarkdownImports()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            string txtPath = Path.Combine(tempFolder, "001.txt");
            string mdPath = Path.Combine(tempFolder, "002.md");

            File.WriteAllText(txtPath, "Chapter One\n\n<script>alert('kept as text');</script>");
            File.WriteAllText(mdPath, "<p onclick=\"read()\">Markdown raw HTML keeps existing behavior.</p>");

            var service = new ChapterImportService();

            var result = service.ImportFilesWithResult([txtPath, mdPath]);

            Assert.Contains("&lt;script&gt;alert(&#39;kept as text&#39;);&lt;/script&gt;", result.Project.Chapters[0].HtmlBody);
            Assert.Contains("onclick=\"read()\"", result.Project.Chapters[1].HtmlBody);
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    private static string ImportSingleChapterBody(string extension, string content)
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            string filePath = Path.Combine(tempFolder, $"001{extension}");
            File.WriteAllText(filePath, content);

            var service = new ChapterImportService();

            return service.ImportFilesWithResult([filePath]).Project.Chapters.Single().HtmlBody;
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }
}
