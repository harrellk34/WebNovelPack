namespace WebNovelPack.Core.Models;

public sealed class Chapter
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "Untitled Chapter";
    public string SourcePath { get; set; } = "";
    public string HtmlBody { get; set; } = "";
    public int Order { get; set; }
    public ChapterTitleSource TitleSource { get; set; } = ChapterTitleSource.DetectedContent;
}
