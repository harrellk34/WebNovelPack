using Avalonia.Controls;
using Avalonia.Interactivity;
using WebNovelPack.Core.Importing;
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
            var project = _chapterImportService.ImportFolder(folderPath);

            ChapterList.ItemsSource = project.Chapters
                .OrderBy(chapter => chapter.Order)
                .Select(chapter => $"{chapter.Order}. {chapter.Title}")
                .ToList();

            StatusText.Text = $"Imported {project.Chapters.Count} chapter(s).";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Import failed: {ex.Message}";
        }
    }
}