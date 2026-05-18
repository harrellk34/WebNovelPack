namespace WebNovelPack.Core.Models;

public sealed class BookProject
{
    public BookMetadata Metadata { get; set; } = new();
    public List<Chapter> Chapters { get; set; } = [];
}