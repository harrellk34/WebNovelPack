using WebNovelPack.Core.Exporting;

namespace WebNovelPack.Tests;

public sealed class EpubOutputPathBuilderTests
{
    [Fact]
    public void Build_WithValidFolderAndTitle_ShouldProduceEpubPath()
    {
        using var tempFolder = TempFolder.Create();
        var builder = new EpubOutputPathBuilder();

        var result = builder.Build(tempFolder.Path, "The First Volume");

        Assert.True(result.IsValid);
        Assert.Equal(EpubOutputPathStatus.Valid, result.Status);
        Assert.Equal("The First Volume.epub", result.FileName);
        Assert.Equal(Path.Combine(tempFolder.Path, "The First Volume.epub"), result.OutputPath);
    }

    [Fact]
    public void Build_WhenExtensionIsMissing_ShouldAddEpubExtension()
    {
        using var tempFolder = TempFolder.Create();
        var builder = new EpubOutputPathBuilder();

        var result = builder.Build(tempFolder.Path, "Volume One");

        Assert.True(result.IsValid);
        Assert.Equal("Volume One.epub", result.FileName);
        Assert.EndsWith(".epub", result.OutputPath);
    }

    [Fact]
    public void Build_WithInvalidFileNameCharacters_ShouldSanitizeFileName()
    {
        using var tempFolder = TempFolder.Create();
        var builder = new EpubOutputPathBuilder();

        var result = builder.Build(tempFolder.Path, "Volume: One?*");

        Assert.True(result.IsValid);
        Assert.True(result.WasFileNameSanitized);
        Assert.Equal("Volume_ One__.epub", result.FileName);
        Assert.Equal(Path.Combine(tempFolder.Path, "Volume_ One__.epub"), result.OutputPath);
    }

    [Theory]
    [InlineData("../Volume One")]
    [InlineData(@"..\Volume One")]
    public void Build_WithPathTraversalAttempt_ShouldRejectFileName(string titleOrFileName)
    {
        using var tempFolder = TempFolder.Create();
        var builder = new EpubOutputPathBuilder();

        var result = builder.Build(tempFolder.Path, titleOrFileName);

        Assert.False(result.IsValid);
        Assert.Equal(EpubOutputPathStatus.PathTraversalDetected, result.Status);
        Assert.Null(result.OutputPath);
    }

    [Fact]
    public void Build_WithAbsoluteFileName_ShouldRejectFileName()
    {
        using var tempFolder = TempFolder.Create();
        string absoluteFileName = Path.Combine(tempFolder.Path, "Volume One.epub");
        var builder = new EpubOutputPathBuilder();

        var result = builder.Build(tempFolder.Path, absoluteFileName);

        Assert.False(result.IsValid);
        Assert.Equal(EpubOutputPathStatus.AbsoluteFileNameNotAllowed, result.Status);
        Assert.Null(result.OutputPath);
    }

    [Fact]
    public void Build_WithMissingOutputFolder_ShouldReportMissingFolder()
    {
        string missingFolder = Path.Combine(Path.GetTempPath(), $"webnovelpack-missing-{Guid.NewGuid():N}");
        var builder = new EpubOutputPathBuilder();

        var result = builder.Build(missingFolder, "Volume One");

        Assert.False(result.IsValid);
        Assert.Equal(EpubOutputPathStatus.MissingOutputFolder, result.Status);
        Assert.Equal(Path.GetFullPath(missingFolder), result.OutputPath);
        Assert.Contains("does not exist", result.Message);
    }

    [Fact]
    public void Build_WhenOutputFileExistsAndOverwriteIsFalse_ShouldReportExistingFile()
    {
        using var tempFolder = TempFolder.Create();
        string outputFile = Path.Combine(tempFolder.Path, "Volume One.epub");
        File.WriteAllText(outputFile, "existing output");
        var builder = new EpubOutputPathBuilder();

        var result = builder.Build(tempFolder.Path, "Volume One");

        Assert.False(result.IsValid);
        Assert.Equal(EpubOutputPathStatus.OutputFileAlreadyExists, result.Status);
        Assert.Equal(outputFile, result.OutputPath);
        Assert.Equal("Volume One.epub", result.FileName);
        Assert.Contains("already exists", result.Message);
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
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"webnovelpack-test-{Guid.NewGuid():N}");
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
