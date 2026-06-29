# WebNovelPack

WebNovelPack is a C#/.NET desktop app for packaging a folder of web novel or serial-fiction chapters into a single EPUB file.

It is designed for chapters saved as individual files, with import validation, readable chapter titles, metadata entry, and a simple Avalonia export workflow.

## v0.1 Status

WebNovelPack is prepared for a polished v0.1 release. The current version can import chapters, preview import results, edit book metadata, and export an EPUB.


## Features

- Import chapters from a folder.
- Support TXT, Markdown, and HTML chapter files.
- Sort imported chapters in natural filename order, such as `chapter2` before `chapter10`.
- Detect chapter titles from text, Markdown headings, and sanitized HTML headings.
- Fall back to readable filename-based titles when a clear title is not found.
- Validate imports and report missing, empty, unsupported, unreadable, or duplicate files.
- Preview imported chapters, skipped files, and import warnings.
- Edit book title, author, language, description, and identifier.
- Validate required metadata before export.
- Build a safe EPUB output path from the selected folder and book title.
- Generate a basic EPUB 3 package with metadata, navigation, and chapter XHTML files.
- Record import audit events for traceability.
- Maintain automated coverage for import, validation, sanitization, ordering, metadata, output-path handling, and EPUB export.

## Supported Input Formats

- `.txt`
- `.md`
- `.markdown`
- `.html`
- `.htm`

TXT files are converted into simple HTML paragraphs. Markdown files are converted with Markdig. HTML files are parsed with HtmlAgilityPack and sanitized before packaging.

## Basic Workflow

1. Choose **Import Chapter Folder** and select a folder containing chapter files.
2. Review the imported chapter order, skipped files, and warnings.
3. Enter and save book metadata.
4. Choose **Export EPUB** and select an output folder.

## Technology Stack

- C# and .NET 10
- Avalonia UI for the desktop app
- HtmlAgilityPack for HTML parsing and sanitization
- Markdig for Markdown conversion
- xUnit for automated tests

## Setup Requirements

- .NET 10 SDK
- Windows, macOS, or Linux supported by Avalonia and the installed .NET SDK

Restore dependencies before the first build:

```powershell
dotnet restore WebNovelPack.slnx
```

## Build

```powershell
dotnet build WebNovelPack.slnx
```

For the release validation flow used by this project:

```powershell
dotnet build WebNovelPack.slnx --no-restore
```

## Run

```powershell
dotnet run --project WebNovelPack.App
```

## Test

```powershell
dotnet test WebNovelPack.slnx
```

## Project Structure

- `WebNovelPack.App/` - Avalonia desktop app and export workflow UI.
- `WebNovelPack.Core/` - Import, metadata, sanitization, output-path, and EPUB generation logic.
- `WebNovelPack.Tests/` - xUnit tests for the core behavior.
- `WebNovelPack.slnx` - Solution file.

## Security and Reliability Decisions

- HTML sanitization removes script-like elements, embedded content, unsafe event attributes, inline styles, `srcdoc`, and unsafe links before imported HTML is packaged.
- Safe output-path handling rejects empty folders, missing folders, absolute filenames, path traversal, unsafe filename characters, and accidental overwrite attempts.
- Metadata validation requires title and author, trims user input, defaults blank language to `en`, and keeps invalid metadata from replacing the current saved metadata.
- Import and export operations return typed result objects with explicit statuses, counts, messages, and output paths.
- Import audit logging records start, validation, skip, title detection, HTML sanitization, successful import, and completion events.

## Known Limitations

- The app imports folders, not individual ad hoc file selections.
- Chapters can be previewed but not edited or reordered in the UI.
- EPUB output is intentionally basic and does not include cover images, custom CSS, series metadata, or advanced EPUB validation.
- Existing output files are not overwritten automatically.
- Project persistence is not implemented; metadata and imported chapters are kept only for the current app session.
- Markdown raw HTML keeps Markdig's current conversion behavior and is not sanitized in the same way as imported HTML files.
