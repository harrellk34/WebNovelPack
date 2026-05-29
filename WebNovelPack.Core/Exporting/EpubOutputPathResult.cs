namespace WebNovelPack.Core.Exporting;

public sealed class EpubOutputPathResult
{
    public EpubOutputPathStatus Status { get; init; }
    public bool IsValid => Status == EpubOutputPathStatus.Valid;
    public string? OutputPath { get; init; }
    public string? FileName { get; init; }
    public string? Message { get; init; }
    public bool WasFileNameSanitized { get; init; }

    public static EpubOutputPathResult Valid(string outputPath, string fileName, bool wasFileNameSanitized)
    {
        return new()
        {
            Status = EpubOutputPathStatus.Valid,
            OutputPath = outputPath,
            FileName = fileName,
            WasFileNameSanitized = wasFileNameSanitized
        };
    }

    internal static EpubOutputPathResult ValidFolder(string outputPath)
    {
        return new()
        {
            Status = EpubOutputPathStatus.Valid,
            OutputPath = outputPath
        };
    }

    internal static EpubOutputPathResult ValidFileName(string fileName, bool wasFileNameSanitized)
    {
        return new()
        {
            Status = EpubOutputPathStatus.Valid,
            FileName = fileName,
            WasFileNameSanitized = wasFileNameSanitized
        };
    }

    public static EpubOutputPathResult Invalid(
        EpubOutputPathStatus status,
        string message,
        string? outputPath = null,
        string? fileName = null,
        bool wasFileNameSanitized = false)
    {
        return new()
        {
            Status = status,
            OutputPath = outputPath,
            FileName = fileName,
            Message = message,
            WasFileNameSanitized = wasFileNameSanitized
        };
    }
}
