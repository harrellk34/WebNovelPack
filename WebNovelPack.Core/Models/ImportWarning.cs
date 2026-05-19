namespace WebNovelPack.Core.Models;

public sealed class ImportWarning
{
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "Warning";
    public string? SourcePath { get; set; }
}
