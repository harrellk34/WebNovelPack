namespace WebNovelPack.Core.Models;

public sealed class PackagingReport
{
    public int TotalFilesDiscovered { get; set; }
    public int SuccessfullyProcessed { get; set; }
    public List<SkippedFileReport> SkippedFiles { get; set; } = [];
    public int SkippedCount => SkippedFiles.Count;
    public string? OutputLocation { get; set; }
}
