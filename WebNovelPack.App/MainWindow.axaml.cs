using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebNovelPack.Core.Exporting;
using WebNovelPack.Core.Importing;
using WebNovelPack.Core.Models;

namespace WebNovelPack.App;

public partial class MainWindow : Window
{
    private static readonly string[] EmptyChaptersMessage =
    [
        "No chapters imported yet. Imported chapters will appear here in export order."
    ];

    private static readonly string[] EmptySkippedFilesMessage =
    [
        "No skipped files yet."
    ];

    private static readonly string[] EmptyWarningsMessage =
    [
        "No import warnings yet."
    ];

    private readonly ChapterImportService _chapterImportService = new();
    private readonly EpubExportService _epubExportService = new();
    private BookProject _currentProject = new();
    private string? _selectedChapterId;

    public MainWindow()
    {
        InitializeComponent();
        ResetReviewLists();
        LoadMetadataFields(_currentProject.Metadata);
        RefreshExportReadiness();
    }

    private async void ImportFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        string? folderPath = await PickFolderPathAsync("Select folder containing chapter files");

        if (folderPath is null)
        {
            return;
        }

        if (folderPath.Length == 0)
        {
            SetStatus(ImportStatusText, "Could not read selected folder path.");
            return;
        }

        try
        {
            var result = _chapterImportService.ImportFolderWithResult(folderPath);
            result.Project.Metadata = _currentProject.Metadata;
            _currentProject = result.Project;
            LoadMetadataFields(_currentProject.Metadata);
            ResetChapterEditor();
            ShowImportResult(result);

            SetStatus(ImportStatusText, FormatImportSummary(result));
            SetStatus(ExportStatusText, "");
            RefreshExportReadiness();
        }
        catch (Exception ex)
        {
            _currentProject = new BookProject { Metadata = _currentProject.Metadata };
            SetStatus(ImportStatusText, $"Import failed: {ex.Message}");
            SetStatus(ExportStatusText, "");
            ResetReviewLists();
            RefreshExportReadiness();
        }
    }

    private async void ExportEpubButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentProject.Chapters.Count == 0)
        {
            SetStatus(ExportStatusText, "Cannot export EPUB because no chapters have been imported.");
            return;
        }

        var metadataValidation = _currentProject.Metadata.Validate();

        if (!metadataValidation.IsValid)
        {
            SetStatus(ExportStatusText, metadataValidation.Message);
            return;
        }

        string? outputFolder = await PickFolderPathAsync("Select EPUB output folder");

        if (outputFolder is null)
        {
            SetStatus(ExportStatusText, "Export canceled.");
            return;
        }

        if (outputFolder.Length == 0)
        {
            SetStatus(ExportStatusText, "Could not read selected output folder path.");
            return;
        }

        try
        {
            var exportResult = _epubExportService.Export(_currentProject, outputFolder, null);

            if (exportResult.IsSuccess)
            {
                SetStatus(ExportStatusText, $"Export successful: {exportResult.OutputPath}");
            }
            else
            {
                SetStatus(ExportStatusText, exportResult.Message ?? "Export failed.");
            }
        }
        catch (Exception ex)
        {
            SetStatus(ExportStatusText, $"Unexpected export failure: {ex.Message}");
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
            SetStatus(MetadataStatusText, result.Message);
            RefreshExportReadiness();
            return;
        }

        LoadMetadataFields(_currentProject.Metadata);
        SetStatus(MetadataStatusText, "Metadata saved.");
        RefreshExportReadiness();
    }

    private void ChapterList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ChapterList.SelectedItem is not ChapterListItem selectedItem)
        {
            ResetChapterEditor();
            return;
        }

        var chapter = FindChapter(selectedItem.ChapterId);

        if (chapter is null)
        {
            ResetChapterEditor();
            SetStatus(ChapterEditStatusText, "Selected chapter could not be found.");
            return;
        }

        _selectedChapterId = chapter.Id;
        ChapterTitleTextBox.Text = chapter.Title;
        ChapterContentTextBox.Text = chapter.HtmlBody;
        ChapterEditorFieldsPanel.IsVisible = true;
        ChapterEditorEmptyText.Text = "Edit the selected chapter title and export content.";
        SaveChapterChangesButton.IsEnabled = true;
        SetStatus(ChapterEditStatusText, "");
    }

    private void SaveChapterChangesButton_Click(object? sender, RoutedEventArgs e)
    {
        var chapter = GetSelectedChapter();

        if (chapter is null)
        {
            ResetChapterEditor();
            SetStatus(ChapterEditStatusText, "Select a chapter before saving changes.");
            return;
        }

        string title = ChapterTitleTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(title))
        {
            SetStatus(ChapterEditStatusText, "Chapter title is required.");
            return;
        }

        chapter.Title = title;
        chapter.HtmlBody = _chapterImportService.NormalizeEditedChapterContent(ChapterContentTextBox.Text ?? "");

        RefreshChapterPreview(chapter.Id);
        SetStatus(ChapterEditStatusText, "Chapter changes saved.");
    }

    private void LoadMetadataFields(BookMetadata metadata)
    {
        TitleTextBox.Text = metadata.Title;
        AuthorTextBox.Text = metadata.Author;
        LanguageTextBox.Text = metadata.Language;
        DescriptionTextBox.Text = metadata.Description;
        IdentifierTextBox.Text = metadata.Identifier ?? "";
        SetStatus(MetadataStatusText, "");
    }

    private async Task<string?> PickFolderPathAsync(string title)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new()
        {
            Title = title,
            AllowMultiple = false
        });

        if (folders.Count == 0)
        {
            return null;
        }

        string? folderPath = folders[0].Path.LocalPath;

        return string.IsNullOrWhiteSpace(folderPath)
            ? ""
            : folderPath;
    }

    private void ShowImportResult(ImportResult result)
    {
        RefreshChapterPreview();

        SkippedFilesList.ItemsSource = result.Preview.SkippedFiles.Count == 0
            ? EmptySkippedFilesMessage
            : result.Preview.SkippedFiles
                .Select(FormatSkippedFile)
                .ToList();

        WarningsList.ItemsSource = result.Warnings.Count == 0
            ? EmptyWarningsMessage
            : result.Warnings
                .Select(FormatWarning)
                .ToList();
    }

    private void ResetReviewLists()
    {
        ChapterList.ItemsSource = EmptyChaptersMessage;
        SkippedFilesList.ItemsSource = EmptySkippedFilesMessage;
        WarningsList.ItemsSource = EmptyWarningsMessage;
        ChapterCountText.Text = "0 chapters";
        ResetChapterEditor();
    }

    private void RefreshChapterPreview(string? chapterIdToSelect = null)
    {
        if (_currentProject.Chapters.Count == 0)
        {
            ChapterList.ItemsSource = EmptyChaptersMessage;
            ResetChapterEditor();
            return;
        }

        var items = _currentProject.Chapters
            .OrderBy(chapter => chapter.Order)
            .Select(chapter => new ChapterListItem(chapter.Id, FormatChapterItem(chapter)))
            .ToList();

        ChapterList.ItemsSource = items;

        if (chapterIdToSelect is not null)
        {
            ChapterList.SelectedItem = items.FirstOrDefault(item => item.ChapterId == chapterIdToSelect);
        }
    }

    private void ResetChapterEditor()
    {
        _selectedChapterId = null;
        ChapterList.SelectedItem = null;
        ChapterTitleTextBox.Text = "";
        ChapterContentTextBox.Text = "";
        ChapterEditorFieldsPanel.IsVisible = false;
        ChapterEditorEmptyText.Text = _currentProject.Chapters.Count == 0
            ? "Import chapters before editing."
            : "Select an imported chapter to edit its title and content.";
        SaveChapterChangesButton.IsEnabled = false;
        SetStatus(ChapterEditStatusText, "");
    }

    private void RefreshExportReadiness()
    {
        int chapterCount = _currentProject.Chapters.Count;
        ChapterCountText.Text = chapterCount == 1
            ? "1 chapter"
            : $"{chapterCount} chapters";

        var metadataValidation = _currentProject.Metadata.Validate();
        bool canExport = chapterCount > 0 && metadataValidation.IsValid;
        ExportEpubButton.IsEnabled = canExport;

        if (chapterCount == 0)
        {
            ExportReadinessText.Text = "Import chapters before exporting.";
        }
        else if (!metadataValidation.IsValid)
        {
            ExportReadinessText.Text = "Save title and author before exporting.";
        }
        else
        {
            ExportReadinessText.Text = $"Ready to export {chapterCount} chapter(s).";
        }
    }

    private static void SetStatus(TextBlock target, string message)
    {
        target.Text = message;
    }

    private static string FormatImportSummary(ImportResult result)
    {
        return $"Discovered {result.Report.TotalFilesDiscovered} file(s); imported {result.Report.SuccessfullyProcessed}; skipped {result.Report.SkippedCount}.";
    }

    private Chapter? GetSelectedChapter()
    {
        return _selectedChapterId is null
            ? null
            : FindChapter(_selectedChapterId);
    }

    private Chapter? FindChapter(string chapterId)
    {
        return _currentProject.Chapters.FirstOrDefault(chapter => chapter.Id == chapterId);
    }

    private static string FormatChapterItem(Chapter chapter)
    {
        string sourceFormat = System.IO.Path.GetExtension(chapter.SourcePath).ToLowerInvariant();
        string format = string.IsNullOrWhiteSpace(sourceFormat)
            ? ""
            : $" [{sourceFormat}]";
        string fileName = string.IsNullOrWhiteSpace(chapter.SourcePath)
            ? "edited chapter"
            : System.IO.Path.GetFileName(chapter.SourcePath);

        return $"{chapter.Order}. {chapter.Title} - {fileName}{format}";
    }

    private static string FormatSkippedFile(SkippedFileReport skippedFile)
    {
        return $"{skippedFile.FileName} - {skippedFile.Reason}";
    }

    private static string FormatWarning(ImportWarning warning)
    {
        return warning.SourcePath is null
            ? warning.Message
            : $"{warning.Message} ({System.IO.Path.GetFileName(warning.SourcePath)})";
    }

    private sealed record ChapterListItem(string ChapterId, string DisplayText)
    {
        public override string ToString()
        {
            return DisplayText;
        }
    }
}
