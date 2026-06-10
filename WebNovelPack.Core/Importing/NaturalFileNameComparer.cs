namespace WebNovelPack.Core.Importing;

internal sealed class NaturalFileNameComparer : IComparer<string>
{
    public static NaturalFileNameComparer OrdinalIgnoreCase { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        string left = Path.GetFileName(x);
        string right = Path.GetFileName(y);

        int fileNameComparison = CompareNatural(left, right);

        return fileNameComparison != 0
            ? fileNameComparison
            : StringComparer.OrdinalIgnoreCase.Compare(x, y);
    }

    private static int CompareNatural(string left, string right)
    {
        int leftIndex = 0;
        int rightIndex = 0;

        while (leftIndex < left.Length && rightIndex < right.Length)
        {
            char leftChar = left[leftIndex];
            char rightChar = right[rightIndex];

            if (char.IsDigit(leftChar) && char.IsDigit(rightChar))
            {
                int numberComparison = CompareNumberRuns(left, ref leftIndex, right, ref rightIndex);

                if (numberComparison != 0)
                {
                    return numberComparison;
                }

                continue;
            }

            int charComparison = StringComparer.OrdinalIgnoreCase.Compare(
                leftChar.ToString(),
                rightChar.ToString());

            if (charComparison != 0)
            {
                return charComparison;
            }

            leftIndex++;
            rightIndex++;
        }

        return left.Length.CompareTo(right.Length);
    }

    private static int CompareNumberRuns(string left, ref int leftIndex, string right, ref int rightIndex)
    {
        int leftStart = leftIndex;
        int rightStart = rightIndex;

        while (leftIndex < left.Length && char.IsDigit(left[leftIndex]))
        {
            leftIndex++;
        }

        while (rightIndex < right.Length && char.IsDigit(right[rightIndex]))
        {
            rightIndex++;
        }

        ReadOnlySpan<char> leftNumber = left.AsSpan(leftStart, leftIndex - leftStart);
        ReadOnlySpan<char> rightNumber = right.AsSpan(rightStart, rightIndex - rightStart);
        ReadOnlySpan<char> normalizedLeftNumber = TrimLeadingZeros(leftNumber);
        ReadOnlySpan<char> normalizedRightNumber = TrimLeadingZeros(rightNumber);

        int lengthComparison = normalizedLeftNumber.Length.CompareTo(normalizedRightNumber.Length);

        if (lengthComparison != 0)
        {
            return lengthComparison;
        }

        int valueComparison = normalizedLeftNumber.SequenceCompareTo(normalizedRightNumber);

        if (valueComparison != 0)
        {
            return valueComparison;
        }

        return leftNumber.Length.CompareTo(rightNumber.Length);
    }

    private static ReadOnlySpan<char> TrimLeadingZeros(ReadOnlySpan<char> value)
    {
        int index = 0;

        while (index < value.Length - 1 && value[index] == '0')
        {
            index++;
        }

        return value[index..];
    }
}
