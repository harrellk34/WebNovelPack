namespace WebNovelPack.Core.Models;

public sealed class BookMetadataValidationResult
{
    private BookMetadataValidationResult(bool isValid, BookMetadata? metadata, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Metadata = metadata;
        Errors = errors;
    }

    public bool IsValid { get; }
    public BookMetadata? Metadata { get; }
    public IReadOnlyList<string> Errors { get; }
    public string Message => string.Join(" ", Errors);

    public static BookMetadataValidationResult Valid(BookMetadata metadata)
    {
        return new BookMetadataValidationResult(true, metadata, []);
    }

    public static BookMetadataValidationResult Invalid(IReadOnlyList<string> errors)
    {
        return new BookMetadataValidationResult(false, null, errors);
    }
}
