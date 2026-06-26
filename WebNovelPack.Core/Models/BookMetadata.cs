namespace WebNovelPack.Core.Models;

public sealed class BookMetadata
{
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Language { get; set; } = "en";
    public string Description { get; set; } = "";
    public string? Identifier { get; set; }

    public static BookMetadataValidationResult Create(
        string? title,
        string? author,
        string? language,
        string? description,
        string? identifier)
    {
        string normalizedTitle = NormalizeRequired(title);
        string normalizedAuthor = NormalizeRequired(author);
        string normalizedLanguage = NormalizeRequired(language);
        string normalizedDescription = NormalizeOptional(description) ?? "";
        string? normalizedIdentifier = NormalizeOptional(identifier);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedAuthor))
        {
            errors.Add("Author is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedLanguage))
        {
            normalizedLanguage = "en";
        }

        if (errors.Count > 0)
        {
            return BookMetadataValidationResult.Invalid(errors);
        }

        return BookMetadataValidationResult.Valid(new BookMetadata
        {
            Title = normalizedTitle,
            Author = normalizedAuthor,
            Language = normalizedLanguage,
            Description = normalizedDescription,
            Identifier = normalizedIdentifier
        });
    }

    public BookMetadataValidationResult Validate()
    {
        return Create(Title, Author, Language, Description, Identifier);
    }

    private static string NormalizeRequired(string? value)
    {
        return value?.Trim() ?? "";
    }

    private static string? NormalizeOptional(string? value)
    {
        string normalized = value?.Trim() ?? "";

        return normalized.Length == 0
            ? null
            : normalized;
    }
}
