using System.IO.Compression;
using WebNovelPack.Core.Exporting;
using WebNovelPack.Core.Models;

namespace WebNovelPack.Tests;

public sealed class EpubExportServiceTests
{
    [Fact]
    public void Export_WithValidProject_ShouldSucceedAndCreateFile()
    {
        using var tempFolder = TempFolder.Create();
        var project = CreateProject();
        var service = new EpubExportService();

        var result = service.Export(project, tempFolder.Path, "Sample Book");

        Assert.True(result.IsSuccess);
        Assert.Equal(EpubExportStatus.Success, result.Status);
        Assert.Equal(Path.Combine(tempFolder.Path, "Sample Book.epub"), result.OutputPath);
        Assert.True(File.Exists(result.OutputPath));
        Assert.Equal(2, result.ExportedChapterCount);
    }

    [Fact]
    public void Export_WithValidProject_ShouldCreateMinimalEpubStructure()
    {
        using var tempFolder = TempFolder.Create();
        var project = CreateProject();
        var service = new EpubExportService();

        var result = service.Export(project, tempFolder.Path, "Sample Book");

        using var archive = ZipFile.OpenRead(result.OutputPath!);
        var entries = archive.Entries.Select(entry => entry.FullName).ToList();

        Assert.Contains("mimetype", entries);
        Assert.Contains("META-INF/container.xml", entries);
        Assert.Contains("EPUB/content.opf", entries);
        Assert.Contains("EPUB/nav.xhtml", entries);
        Assert.Contains("EPUB/chapter-1.xhtml", entries);
        Assert.Contains("EPUB/chapter-2.xhtml", entries);

        var mimetypeEntry = archive.GetEntry("mimetype");
        Assert.NotNull(mimetypeEntry);
        Assert.Equal("mimetype", mimetypeEntry!.FullName);
        Assert.True(mimetypeEntry.CompressedLength == mimetypeEntry.Length);
        Assert.Equal("mimetype", entries[0]);
    }

    [Fact]
    public void Export_WithValidProject_ShouldIncludeMetadataAndChapterContent()
    {
        using var tempFolder = TempFolder.Create();
        var project = CreateProject();
        var service = new EpubExportService();

        var result = service.Export(project, tempFolder.Path, "Sample Book");

        using var archive = ZipFile.OpenRead(result.OutputPath!);
        var contentOpf = ReadEntryText(archive, "EPUB/content.opf");
        var chapterOne = ReadEntryText(archive, "EPUB/chapter-1.xhtml");
        var chapterTwo = ReadEntryText(archive, "EPUB/chapter-2.xhtml");

        Assert.Contains("<dc:title>Sample Book</dc:title>", contentOpf);
        Assert.Contains("<dc:creator>Test Author</dc:creator>", contentOpf);
        Assert.Contains("<dc:language>en</dc:language>", contentOpf);
        Assert.Contains("<dc:description>Sample description</dc:description>", contentOpf);
        Assert.Contains("<h1>Chapter One</h1>", chapterOne);
        Assert.Contains("<p>First paragraph</p>", chapterOne);
        Assert.Contains("<h1>Chapter Two</h1>", chapterTwo);
        Assert.Contains("<p>Second paragraph</p>", chapterTwo);
    }

    [Fact]
    public void Export_WithInvalidMetadata_ShouldFailCleanly()
    {
        using var tempFolder = TempFolder.Create();
        var project = new BookProject
        {
            Metadata = new BookMetadata { Title = "", Author = "Test Author", Language = "en" },
            Chapters = [new Chapter { Title = "Chapter One", HtmlBody = "<p>Body</p>", Order = 1 }]
        };
        var service = new EpubExportService();

        var result = service.Export(project, tempFolder.Path, "Sample Book");

        Assert.False(result.IsSuccess);
        Assert.Equal(EpubExportStatus.InvalidMetadata, result.Status);
        Assert.Null(result.OutputPath);
        Assert.Contains("Title is required", result.Message);
    }

    [Fact]
    public void Export_WithNoChapters_ShouldFailCleanly()
    {
        using var tempFolder = TempFolder.Create();
        var project = new BookProject
        {
            Metadata = new BookMetadata { Title = "Sample Book", Author = "Test Author", Language = "en" }
        };
        var service = new EpubExportService();

        var result = service.Export(project, tempFolder.Path, "Sample Book");

        Assert.False(result.IsSuccess);
        Assert.Equal(EpubExportStatus.NoChapters, result.Status);
        Assert.Null(result.OutputPath);
        Assert.Contains("no imported chapters", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Export_WithUnsafeOutputPath_ShouldFailCleanly()
    {
        using var tempFolder = TempFolder.Create();
        var project = CreateProject();
        var service = new EpubExportService();

        var result = service.Export(project, tempFolder.Path, "../Unsafe Book");

        Assert.False(result.IsSuccess);
        Assert.Equal(EpubExportStatus.InvalidOutputPath, result.Status);
        Assert.Null(result.OutputPath);
        Assert.Contains("must not contain path segments", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static BookProject CreateProject()
    {
        var project = new BookProject
        {
            Metadata = new BookMetadata
            {
                Title = "Sample Book",
                Author = "Test Author",
                Language = "en",
                Description = "Sample description",
                Identifier = "sample-id"
            },
            Chapters =
            [
                new Chapter { Title = "Chapter Two", HtmlBody = "<p>Second paragraph</p>", Order = 2 },
                new Chapter { Title = "Chapter One", HtmlBody = "<p>First paragraph</p>", Order = 1 }
            ]
        };

        return project;
    }

    private static string ReadEntryText(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        Assert.NotNull(entry);

        using var stream = entry!.Open();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed class TempFolder : IDisposable
    {
        private TempFolder(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempFolder Create()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"webnovelpack-epub-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return new TempFolder(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
