namespace WebNovelPack.Core.Models;

public sealed class SkippedFileReport
{
    public string SourcePath { get; set; } = "";
    public string FileName => Path.GetFileName(SourcePath);
    public string Reason { get; set; } = "";
}
