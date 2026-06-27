namespace WebNovelPack.Core.Exporting;

public sealed class EpubExportResult
{
    public EpubExportStatus Status { get; init; }
    public bool IsSuccess => Status == EpubExportStatus.Success;
    public string? OutputPath { get; init; }
    public string? Message { get; init; }
    public int ExportedChapterCount { get; init; }

    public static EpubExportResult Success(string outputPath, int exportedChapterCount)
    {
        return new()
        {
            Status = EpubExportStatus.Success,
            OutputPath = outputPath,
            ExportedChapterCount = exportedChapterCount
        };
    }

    public static EpubExportResult Failure(EpubExportStatus status, string message, string? outputPath = null)
    {
        return new()
        {
            Status = status,
            Message = message,
            OutputPath = outputPath
        };
    }
}
