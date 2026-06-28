using Avalonia.Controls;
using Avalonia.Interactivity;
using WebNovelPack.Core.Exporting;
using WebNovelPack.Core.Importing;
using WebNovelPack.Core.Models;
using System;
using System.Linq;

namespace WebNovelPack.App;

public partial class MainWindow : Window
{
    private readonly ChapterImportService _chapterImportService = new();
    private readonly EpubExportService _epubExportService = new();
    private BookProject _currentProject = new();
    private ImportResult? _currentImportResult;

    public MainWindow()
    {
        InitializeComponent();
        LoadMetadataFields(_currentProject.Metadata);
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
            result.Project.Metadata = _currentProject.Metadata;
            _currentProject = result.Project;
            _currentImportResult = result;
            LoadMetadataFields(_currentProject.Metadata);

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
            ExportEpubButton.IsEnabled = _currentProject.Chapters.Count > 0;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Import failed: {ex.Message}";
            ExportStatusText.Text = string.Empty;
            ChapterList.ItemsSource = null;
            SkippedFilesList.ItemsSource = null;
            WarningsList.ItemsSource = null;
            _currentImportResult = null;
            ExportEpubButton.IsEnabled = false;
        }
    }

    private async void ExportEpubButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentProject.Chapters.Count == 0)
        {
            ExportStatusText.Text = "Cannot export EPUB because no chapters have been imported.";
            return;
        }

        var folders = await StorageProvider.OpenFolderPickerAsync(new()
        {
            Title = "Select EPUB output folder",
            AllowMultiple = false
        });

        if (folders.Count == 0)
        {
            ExportStatusText.Text = "Export canceled.";
            return;
        }

        string? outputFolder = folders[0].Path.LocalPath;

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            ExportStatusText.Text = "Could not read selected output folder path.";
            return;
        }

        try
        {
            var exportResult = _epubExportService.Export(_currentProject, outputFolder, null);

            if (exportResult.IsSuccess)
            {
                ExportStatusText.Text = $"Export successful: {exportResult.OutputPath}";
            }
            else
            {
                ExportStatusText.Text = exportResult.Message ?? "Export failed.";
            }
        }
        catch (Exception ex)
        {
            ExportStatusText.Text = $"Unexpected export failure: {ex.Message}";
        }
    }

    private void SaveMetadataButton_Click(object? sender, RoutedEventArgs e)
    {
        var candidate = new BookMetadata
        {
            Title = TitleTextBox.Text ?? "",
            Author = AuthorTextBox.Text ?? "",
            Language = LanguageTextBox.Text ?? "",
            Description = DescriptionTextBox.Text ?? "",
            Identifier = IdentifierTextBox.Text
        };

        var result = _currentProject.UpdateMetadata(candidate);

        if (!result.IsValid)
        {
            MetadataStatusText.Text = result.Message;
            return;
        }

        if (_currentImportResult is not null)
        {
            _currentImportResult.Project.Metadata = _currentProject.Metadata;
        }

        LoadMetadataFields(_currentProject.Metadata);
        MetadataStatusText.Text = "Metadata saved.";
    }

    private void LoadMetadataFields(BookMetadata metadata)
    {
        TitleTextBox.Text = metadata.Title;
        AuthorTextBox.Text = metadata.Author;
        LanguageTextBox.Text = metadata.Language;
        DescriptionTextBox.Text = metadata.Description;
        IdentifierTextBox.Text = metadata.Identifier ?? "";
        MetadataStatusText.Text = "";
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
