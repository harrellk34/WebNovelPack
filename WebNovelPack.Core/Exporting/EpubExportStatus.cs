namespace WebNovelPack.Core.Exporting;

public enum EpubExportStatus
{
    Success,
    InvalidMetadata,
    NoChapters,
    InvalidOutputPath,
    ExportFailed
}
