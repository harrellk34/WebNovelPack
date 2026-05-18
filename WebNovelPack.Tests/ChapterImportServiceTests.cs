using FluentAssertions;
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

            project.Chapters.Should().HaveCount(3);
            project.Chapters[0].Title.Should().Contain("Chapter 1");
            project.Chapters[1].Title.Should().Contain("Chapter 2");
            project.Chapters[2].Title.Should().Contain("Chapter 3");

            project.Chapters[0].HtmlBody.Should().Contain("The story begins here.");
            project.Chapters[0].HtmlBody.Should().NotContain("Previous | Next");
            project.Chapters[1].HtmlBody.Should().Contain("<h1");
            project.Chapters[2].HtmlBody.Should().Contain("<p>");
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }
}