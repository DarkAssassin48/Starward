using System.Text.RegularExpressions;

namespace Starward.Features.GameRecord.StarRail;


/// <summary>
/// Normalizes stage titles returned by the game-record API for display in the UI.
/// </summary>
public static class StarRailRecordTextHelper
{

    private static readonly Regex MissingSpaceAfterClosingParenthesisRegex = new(
        @"\)(?=\p{L})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex MissingSpaceAfterRomanNumeralRegex = new(
        @"\b([IVXLCDM]{1,8})(?=\p{Lu}\p{Ll})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);


    public static string NormalizeStageName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string result = MissingSpaceAfterClosingParenthesisRegex.Replace(value.Trim(), ") ");
        return MissingSpaceAfterRomanNumeralRegex.Replace(result, "$1 ");
    }

}
