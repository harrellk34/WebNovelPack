using WebNovelPack.Core.Importing;
using WebNovelPack.Core.Models;

namespace WebNovelPack.Tests;

public sealed class BookMetadataTests
{
    [Fact]
    public void Create_WithValidMetadata_ShouldAcceptMetadata()
    {
        var result = BookMetadata.Create(
            "The Lantern Door",
            "Mira Vale",
            "en",
            "A serialized fantasy novel.",
            "urn:isbn:1234567890");

        Assert.True(result.IsValid);
        Assert.NotNull(result.Metadata);
        Assert.Equal("The Lantern Door", result.Metadata.Title);
        Assert.Equal("Mira Vale", result.Metadata.Author);
        Assert.Equal("en", result.Metadata.Language);
        Assert.Equal("A serialized fantasy novel.", result.Metadata.Description);
        Assert.Equal("urn:isbn:1234567890", result.Metadata.Identifier);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Create_WithMissingTitle_ShouldRejectMetadata()
    {
        var result = BookMetadata.Create("", "Mira Vale", "en", "", null);

        Assert.False(result.IsValid);
        Assert.Null(result.Metadata);
        Assert.Contains("Title is required.", result.Errors);
    }

    [Fact]
    public void Create_WithMissingAuthor_ShouldRejectMetadata()
    {
        var result = BookMetadata.Create("The Lantern Door", " ", "en", "", null);

        Assert.False(result.IsValid);
        Assert.Null(result.Metadata);
        Assert.Contains("Author is required.", result.Errors);
    }

    [Fact]
    public void Create_WithBlankLanguage_ShouldDefaultToEnglish()
    {
        var result = BookMetadata.Create("The Lantern Door", "Mira Vale", " ", "", null);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Metadata);
        Assert.Equal("en", result.Metadata.Language);
    }

    [Fact]
    public void Create_ShouldTrimMetadataValues()
    {
        var result = BookMetadata.Create(
            "  The Lantern Door  ",
            "  Mira Vale  ",
            "  fr  ",
            "  A serialized fantasy novel.  ",
            "  story-001  ");

        Assert.True(result.IsValid);
        Assert.NotNull(result.Metadata);
        Assert.Equal("The Lantern Door", result.Metadata.Title);
        Assert.Equal("Mira Vale", result.Metadata.Author);
        Assert.Equal("fr", result.Metadata.Language);
        Assert.Equal("A serialized fantasy novel.", result.Metadata.Description);
        Assert.Equal("story-001", result.Metadata.Identifier);
    }

    [Fact]
    public void Create_WithEmptyIdentifier_ShouldAcceptMetadata()
    {
        var result = BookMetadata.Create("The Lantern Door", "Mira Vale", "en", "", " ");

        Assert.True(result.IsValid);
        Assert.NotNull(result.Metadata);
        Assert.Null(result.Metadata.Identifier);
    }

    [Fact]
    public void UpdateMetadata_ShouldPreserveImportedChapters()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            File.WriteAllText(Path.Combine(tempFolder, "001.txt"), "Chapter One\n\nThe first chapter imports.");
            File.WriteAllText(Path.Combine(tempFolder, "002.txt"), "Chapter Two\n\nThe second chapter imports.");

            var service = new ChapterImportService();
            var importResult = service.ImportFolderWithResult(tempFolder);
            var originalChapters = importResult.Project.Chapters.ToList();

            var result = importResult.Project.UpdateMetadata(new BookMetadata
            {
                Title = "The Lantern Door",
                Author = "Mira Vale",
                Language = "en",
                Description = "A serialized fantasy novel."
            });

            Assert.True(result.IsValid);
            Assert.Equal(originalChapters, importResult.Project.Chapters);
            Assert.Equal(["Chapter One", "Chapter Two"], importResult.Project.Chapters.Select(chapter => chapter.Title));
            Assert.Equal("The Lantern Door", importResult.Project.Metadata.Title);
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }
}
