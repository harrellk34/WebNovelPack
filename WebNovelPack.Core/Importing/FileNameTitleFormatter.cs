using System.Globalization;
using System.Text.RegularExpressions;

namespace WebNovelPack.Core.Importing;

internal static partial class FileNameTitleFormatter
{
    public static string Format(string path)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "Untitled Chapter";
        }

        string spaced = SeparatorRegex()
            .Replace(fileName, " ")
            .Trim();

        if (string.IsNullOrWhiteSpace(spaced))
        {
            return "Untitled Chapter";
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced.ToLowerInvariant());
    }

    [GeneratedRegex(@"[_\-.]+")]
    private static partial Regex SeparatorRegex();
}
