namespace WebNovelPack.Core.Models;

public sealed class ImportPreviewItem
{
    public int Order { get; set; }
    public string Title { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string SourceFormat { get; set; } = "";
    public ChapterTitleSource TitleSource { get; set; }
}
