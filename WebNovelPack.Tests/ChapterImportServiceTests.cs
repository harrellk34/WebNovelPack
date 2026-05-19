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

            var project = service.ImportFolder(tempFolder);

            Assert.Equal(3, project.Chapters.Count);
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
            Assert.Contains(result.Warnings, warning => warning.Message.Contains("Unsupported file"));
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
