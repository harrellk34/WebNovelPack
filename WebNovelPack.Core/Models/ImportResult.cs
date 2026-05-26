namespace WebNovelPack.Core.Models;

public sealed class ImportResult
{
    public BookProject Project { get; set; } = new();
    public List<ImportWarning> Warnings { get; set; } = [];
    public PackagingReport Report { get; set; } = new();
    public int SupportedFileCount { get; set; }
    public int SkippedFileCount { get; set; }
}
