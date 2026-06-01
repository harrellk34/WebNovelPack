namespace WebNovelPack.Core.Models;

public enum ImportAuditEventType
{
    ImportStarted,
    FileValidated,
    FileSkipped,
    ChapterImported,
    HtmlSanitized,
    ImportCompleted,
    OutputPathValidated
}
