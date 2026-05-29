using System.Text;

namespace WebNovelPack.Core.Exporting;

public sealed class EpubOutputPathBuilder
{
    private const string EpubExtension = ".epub";
    private const string ExtraUnsafeFileNameCharacters = "<>:\"|?*";

    public EpubOutputPathResult Build(
        string? outputFolder,
        string? titleOrFileName,
        bool overwriteExisting = false)
    {
        var folderValidation = ValidateOutputFolder(outputFolder);

        if (!folderValidation.IsValid)
        {
            return folderValidation;
        }

        var fileNameValidation = BuildSafeFileName(titleOrFileName);

        if (!fileNameValidation.IsValid)
        {
            return fileNameValidation.Result;
        }

        string outputPath = Path.GetFullPath(Path.Combine(folderValidation.OutputPath!, fileNameValidation.FileName!));
        string relativeOutputPath = Path.GetRelativePath(folderValidation.OutputPath!, outputPath);

        if (IsOutsideOutputFolder(relativeOutputPath) || Path.IsPathRooted(relativeOutputPath))
        {
            return EpubOutputPathResult.Invalid(
                EpubOutputPathStatus.PathTraversalDetected,
                "The output file name must stay inside the output folder.");
        }

        if (!overwriteExisting && File.Exists(outputPath))
        {
            return EpubOutputPathResult.Invalid(
                EpubOutputPathStatus.OutputFileAlreadyExists,
                "The output file already exists.",
                outputPath,
                fileNameValidation.FileName,
                fileNameValidation.WasSanitized);
        }

        return EpubOutputPathResult.Valid(
            outputPath,
            fileNameValidation.FileName!,
            fileNameValidation.WasSanitized);
    }

    private static EpubOutputPathResult ValidateOutputFolder(string? outputFolder)
    {
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            return EpubOutputPathResult.Invalid(
                EpubOutputPathStatus.InvalidOutputFolder,
                "Output folder is required.");
        }

        string fullOutputFolder;

        try
        {
            fullOutputFolder = Path.GetFullPath(outputFolder);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
        {
            return EpubOutputPathResult.Invalid(
                EpubOutputPathStatus.InvalidOutputFolder,
                $"Output folder is invalid: {ex.Message}");
        }

        if (!Directory.Exists(fullOutputFolder))
        {
            return EpubOutputPathResult.Invalid(
                EpubOutputPathStatus.MissingOutputFolder,
                "Output folder does not exist.",
                fullOutputFolder);
        }

        return EpubOutputPathResult.ValidFolder(fullOutputFolder);
    }

    private static SafeFileNameValidation BuildSafeFileName(string? titleOrFileName)
    {
        if (string.IsNullOrWhiteSpace(titleOrFileName))
        {
            return SafeFileNameValidation.Invalid(
                EpubOutputPathStatus.MissingFileName,
                "Output file name or book title is required.");
        }

        string requestedName = titleOrFileName.Trim();

        if (Path.IsPathRooted(requestedName))
        {
            return SafeFileNameValidation.Invalid(
                EpubOutputPathStatus.AbsoluteFileNameNotAllowed,
                "Output file name must not be an absolute path.");
        }

        if (ContainsDirectorySeparator(requestedName) || requestedName == "." || requestedName == "..")
        {
            return SafeFileNameValidation.Invalid(
                EpubOutputPathStatus.PathTraversalDetected,
                "Output file name must not contain path segments.");
        }

        string sanitizedName = SanitizeFileName(requestedName);
        sanitizedName = sanitizedName.Trim().TrimEnd('.', ' ');

        if (string.IsNullOrWhiteSpace(sanitizedName) || sanitizedName == "." || sanitizedName == "..")
        {
            return SafeFileNameValidation.Invalid(
                EpubOutputPathStatus.InvalidFileName,
                "Output file name is invalid after unsafe characters are removed.");
        }

        bool wasSanitized = sanitizedName != requestedName;

        if (!sanitizedName.EndsWith(EpubExtension, StringComparison.OrdinalIgnoreCase))
        {
            sanitizedName += EpubExtension;
        }

        return SafeFileNameValidation.Valid(sanitizedName, wasSanitized);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars()
            .Concat(ExtraUnsafeFileNameCharacters)
            .ToHashSet();

        var builder = new StringBuilder(fileName.Length);

        foreach (char character in fileName)
        {
            builder.Append(invalidCharacters.Contains(character) ? '_' : character);
        }

        return builder.ToString();
    }

    private static bool ContainsDirectorySeparator(string value)
    {
        return value.Contains(Path.DirectorySeparatorChar)
            || value.Contains(Path.AltDirectorySeparatorChar)
            || value.Contains('/')
            || value.Contains('\\');
    }

    private static bool IsOutsideOutputFolder(string relativePath)
    {
        return relativePath == ".."
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)
            || relativePath.StartsWith("../", StringComparison.Ordinal)
            || relativePath.StartsWith(@"..\", StringComparison.Ordinal);
    }

    private sealed record SafeFileNameValidation(
        bool IsValid,
        string? FileName,
        bool WasSanitized,
        EpubOutputPathResult Result)
    {
        public static SafeFileNameValidation Valid(string fileName, bool wasSanitized)
        {
            return new(true, fileName, wasSanitized, EpubOutputPathResult.ValidFileName(fileName, wasSanitized));
        }

        public static SafeFileNameValidation Invalid(EpubOutputPathStatus status, string message)
        {
            return new(false, null, false, EpubOutputPathResult.Invalid(status, message));
        }
    }
}
