# WebNovelPack

WebNovelPack is a C#/.NET desktop app for turning a folder of story chapters into a single EPUB file.

The goal is to make it easier to package web novels, serial fiction, fan writing, or long article collections when each chapter is saved as a separate `.txt`, `.md`, or `.html` file.

## Current Status

This project is still in early development.

Current features:
- Avalonia desktop app setup
- Core library and test project setup
- Basic chapter import for `.txt`, `.md`, and `.html` files
- Natural chapter sorting by filename
- Chapter title detection from text, Markdown, and HTML files
- Readable fallback titles based on file names
- Import validation for missing, empty, unsupported, unreadable, or duplicate files
- Import report showing processed and skipped files
- Import audit log for traceability
- HTML sanitization for imported chapter files
- Safe EPUB output path validation for future export support
- Basic UI feedback for import results
- xUnit tests for import, validation, sanitization, ordering, title detection, audit logging, and output path handling

## Planned Features

- Preview the imported chapter list before export
- Edit and reorder chapters
- Add book metadata
- Export chapters as a single EPUB
- Improve error messages and validation reports

## Notes

Right now the app can import, validate, clean, sort, and title chapter files, but EPUB export is not finished yet.