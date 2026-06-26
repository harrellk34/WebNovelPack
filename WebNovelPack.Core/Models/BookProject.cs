namespace WebNovelPack.Core.Models;

public sealed class BookProject
{
    public BookMetadata Metadata { get; set; } = new();
    public List<Chapter> Chapters { get; set; } = [];

    public BookMetadataValidationResult UpdateMetadata(BookMetadata metadata)
    {
        var result = metadata.Validate();

        if (result.IsValid && result.Metadata is not null)
        {
            Metadata = result.Metadata;
        }

        return result;
    }
}
