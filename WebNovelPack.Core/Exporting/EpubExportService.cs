using System.IO.Compression;
using System.Text;
using System.Xml;
using WebNovelPack.Core.Models;

namespace WebNovelPack.Core.Exporting;

public sealed class EpubExportService
{
    public EpubExportResult Export(BookProject project, string? outputFolder, string? outputFileName)
    {
        var metadataValidation = project.Metadata.Validate();

        if (!metadataValidation.IsValid || metadataValidation.Metadata is null)
        {
            return EpubExportResult.Failure(
                EpubExportStatus.InvalidMetadata,
                string.IsNullOrWhiteSpace(metadataValidation.Message)
                    ? "Book metadata is invalid."
                    : metadataValidation.Message);
        }

        var chapters = project.Chapters
            .OrderBy(chapter => chapter.Order)
            .ThenBy(chapter => chapter.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(chapter => chapter.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (chapters.Count == 0)
        {
            return EpubExportResult.Failure(
                EpubExportStatus.NoChapters,
                "Export failed because there are no imported chapters.");
        }

        var pathBuilder = new EpubOutputPathBuilder();
        var pathResult = pathBuilder.Build(outputFolder, outputFileName ?? metadataValidation.Metadata.Title);

        if (!pathResult.IsValid)
        {
            return EpubExportResult.Failure(
                EpubExportStatus.InvalidOutputPath,
                pathResult.Message ?? "Output path validation failed.");
        }

        try
        {
            var outputPath = pathResult.OutputPath!;
            var exportDirectory = Path.GetDirectoryName(outputPath)!;
            Directory.CreateDirectory(exportDirectory);

            using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);

            var mimetypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using (var writer = new StreamWriter(mimetypeEntry.Open(), new UTF8Encoding(false)))
            {
                writer.Write("application/epub+zip");
            }

            var containerEntry = archive.CreateEntry("META-INF/container.xml");
            using (var writer = new StreamWriter(containerEntry.Open(), new UTF8Encoding(false)))
            {
                writer.Write(GetContainerXml());
            }

            var contentEntries = new List<(string Path, string Content)>();
            var chapterEntries = new List<(string FileName, string Content)>();

            for (int index = 0; index < chapters.Count; index++)
            {
                var chapter = chapters[index];
                var chapterFileName = $"chapter-{index + 1}.xhtml";
                var chapterContent = BuildChapterXhtml(chapter);
                chapterEntries.Add((chapterFileName, chapterContent));
                contentEntries.Add((chapterFileName, chapterContent));
            }

            var navigationContent = BuildNavigationXhtml(chapters.Select(chapter => chapter.Title).ToList());
            var packageContent = BuildPackageDocument(metadataValidation.Metadata, chapters, chapterEntries);

            var navigationEntry = archive.CreateEntry("EPUB/nav.xhtml");
            using (var writer = new StreamWriter(navigationEntry.Open(), new UTF8Encoding(false)))
            {
                writer.Write(navigationContent);
            }

            var packageEntry = archive.CreateEntry("EPUB/content.opf");
            using (var writer = new StreamWriter(packageEntry.Open(), new UTF8Encoding(false)))
            {
                writer.Write(packageContent);
            }

            foreach (var (fileName, content) in chapterEntries)
            {
                var entry = archive.CreateEntry($"EPUB/{fileName}");
                using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                {
                    writer.Write(content);
                }
            }

            return EpubExportResult.Success(outputPath, chapters.Count);
        }
        catch (Exception ex)
        {
            return EpubExportResult.Failure(
                EpubExportStatus.ExportFailed,
                $"Export failed: {ex.Message}");
        }
    }

    private static string BuildPackageDocument(BookMetadata metadata, IReadOnlyList<Chapter> chapters, IReadOnlyList<(string FileName, string Content)> chapterEntries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        builder.AppendLine("<package version=\"3.0\" xmlns=\"http://www.idpf.org/2007/opf\" unique-identifier=\"bookid\">\n");
        builder.AppendLine("  <metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">");
        builder.AppendLine($"    <dc:identifier id=\"bookid\">{EscapeXml(metadata.Identifier ?? metadata.Title)}</dc:identifier>");
        builder.AppendLine($"    <dc:title>{EscapeXml(metadata.Title)}</dc:title>");
        builder.AppendLine($"    <dc:creator>{EscapeXml(metadata.Author)}</dc:creator>");
        builder.AppendLine($"    <dc:language>{EscapeXml(metadata.Language)}</dc:language>");
        builder.AppendLine($"    <dc:description>{EscapeXml(metadata.Description)}</dc:description>");
        builder.AppendLine("  </metadata>");
        builder.AppendLine("  <manifest>");
        builder.AppendLine("    <item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\"/>");

        for (int index = 0; index < chapterEntries.Count; index++)
        {
            var (fileName, _) = chapterEntries[index];
            builder.AppendLine($"    <item id=\"chapter-{index + 1}\" href=\"{fileName}\" media-type=\"application/xhtml+xml\"/>");
        }

        builder.AppendLine("  </manifest>");
        builder.AppendLine("  <spine>");
        for (int index = 0; index < chapterEntries.Count; index++)
        {
            builder.AppendLine($"    <itemref idref=\"chapter-{index + 1}\"/>");
        }
        builder.AppendLine("  </spine>");
        builder.AppendLine("</package>");
        return builder.ToString();
    }

    private static string BuildNavigationXhtml(IReadOnlyList<string> chapterTitles)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        builder.AppendLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" lang=\"en\">");
        builder.AppendLine("  <head><title>Table of Contents</title></head>");
        builder.AppendLine("  <body>");
        builder.AppendLine("    <h1>Contents</h1>");
        builder.AppendLine("    <nav epub:type=\"toc\">");
        builder.AppendLine("      <ol>");

        for (int index = 0; index < chapterTitles.Count; index++)
        {
            string title = chapterTitles[index];
            builder.AppendLine($"        <li><a href=\"chapter-{index + 1}.xhtml\">{EscapeXml(title)}</a></li>");
        }

        builder.AppendLine("      </ol>");
        builder.AppendLine("    </nav>");
        builder.AppendLine("  </body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static string BuildChapterXhtml(Chapter chapter)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        builder.AppendLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" lang=\"en\">");
        builder.AppendLine("  <head><title>" + EscapeXml(chapter.Title) + "</title></head>");
        builder.AppendLine("  <body>");
        builder.AppendLine("    <h1>" + EscapeXml(chapter.Title) + "</h1>");
        builder.AppendLine("    " + chapter.HtmlBody);
        builder.AppendLine("  </body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static string EscapeXml(string value)
    {
        return System.Security.SecurityElement.Escape(value);
    }

    private static string GetContainerXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
               "<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">\n" +
               "  <rootfiles>\n" +
               "    <rootfile full-path=\"EPUB/content.opf\" media-type=\"application/oebps-package+xml\"/>\n" +
               "  </rootfiles>\n" +
               "</container>\n";
    }
}
