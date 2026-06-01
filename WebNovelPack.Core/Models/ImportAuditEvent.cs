namespace WebNovelPack.Core.Models;

public sealed class ImportAuditEvent
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public ImportAuditEventType EventType { get; set; }
    public ImportAuditSeverity Severity { get; set; } = ImportAuditSeverity.Info;
    public string Message { get; set; } = "";
    public string? SourcePath { get; set; }
    public string? FileName => SourcePath is null ? null : Path.GetFileName(SourcePath);
}
