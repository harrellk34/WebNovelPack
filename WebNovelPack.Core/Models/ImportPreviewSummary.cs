namespace WebNovelPack.Core.Models;

public sealed class ImportPreviewSummary
{
    public List<ImportPreviewItem> ImportedChapters { get; set; } = [];
    public List<SkippedFileReport> SkippedFiles { get; set; } = [];
    public int ImportedCount => ImportedChapters.Count;
    public int SkippedCount => SkippedFiles.Count;
}
