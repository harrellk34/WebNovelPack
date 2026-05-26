using WebNovelPack.Core.Importing;

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
}
