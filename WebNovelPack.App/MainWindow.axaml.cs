using Avalonia.Controls;
using Avalonia.Interactivity;
using WebNovelPack.Core.Importing;
using WebNovelPack.Core.Models;
using System;
using System.Linq;

namespace WebNovelPack.App;

public partial class MainWindow : Window
{
    private readonly ChapterImportService _chapterImportService = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void ImportFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new()
        {
            Title = "Select folder containing chapter files",
            AllowMultiple = false
        });

        if (folders.Count == 0)
        {
            return;
        }

        string? folderPath = folders[0].Path.LocalPath;

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            StatusText.Text = "Could not read selected folder path.";
            return;
        }

        try
        {
            var result = _chapterImportService.ImportFolderWithResult(folderPath);

            ChapterList.ItemsSource = result.Preview.ImportedChapters
                .OrderBy(chapter => chapter.Order)
                .Select(FormatPreviewItem)
                .ToList();

            SkippedFilesList.ItemsSource = result.Preview.SkippedFiles
                .Select(FormatSkippedFile)
                .ToList();

            WarningsList.ItemsSource = result.Warnings
                .Select(warning => warning.SourcePath is null
                    ? warning.Message
                    : $"{warning.Message} ({System.IO.Path.GetFileName(warning.SourcePath)})")
                .ToList();

            StatusText.Text = $"Discovered {result.Report.TotalFilesDiscovered} file(s); imported {result.Report.SuccessfullyProcessed}; skipped {result.Report.SkippedCount}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Import failed: {ex.Message}";
            ChapterList.ItemsSource = null;
            SkippedFilesList.ItemsSource = null;
            WarningsList.ItemsSource = null;
        }
    }

    private static string FormatPreviewItem(ImportPreviewItem item)
    {
        string format = string.IsNullOrWhiteSpace(item.SourceFormat)
            ? ""
            : $" [{item.SourceFormat}]";

        return $"{item.Order}. {item.Title} - {item.OriginalFileName}{format}";
    }

    private static string FormatSkippedFile(SkippedFileReport skippedFile)
    {
        return $"{skippedFile.FileName} - {skippedFile.Reason}";
    }
}
