namespace WebNovelPack.Core.Models;

public enum ImportAuditEventType
{
    ImportStarted,
    FileValidated,
    FileSkipped,
    ChapterTitleDetected,
    ChapterImported,
    HtmlSanitized,
    ImportCompleted,
    OutputPathValidated
}
