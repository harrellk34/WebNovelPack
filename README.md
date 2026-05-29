# WebNovelPack

WebNovelPack is a C#/.NET desktop app for turning a folder of story chapters into a single EPUB file.

The goal is to make it easier to package web novels, serial fiction, fan writing, or long article collections when each chapter is saved as a separate `.txt`, `.md`, or `.html` file.

## Current Status

This project is still in early development.

Current features:
- Avalonia desktop app setup
- Core library and test project setup
- Basic chapter import for `.txt`, `.md`, and `.html` files
- Import validation for missing, empty, unsupported, unreadable, or duplicate files
- Import report showing processed and skipped files
- Basic UI feedback for import results
- xUnit tests for the import/validation workflow
- HTML sanitization for imported chapter files
- Safe EPUB output path validation for future export support

## Planned Features

- Sort chapters by filename
- Detect chapter titles
- Edit and reorder chapters
- Add book metadata
- Export chapters as a single EPUB
- Improve error messages and validation reports

## Notes

Right now the app can import and validate chapter files, but EPUB export is not finished yet.