using System.Globalization;
using System.Text;

namespace PersonalFinance.Web.Helpers;

/// <summary>
/// Display + input sanitization for money fields (no third-party deps).
/// </summary>
public static class CurrencyFormat
{
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

    public static string Format(decimal amount) =>
        amount.ToString("C", Us);

    public static string Format(decimal? amount) =>
        amount is null ? "—" : Format(amount.Value);

    /// <summary>Extension for razor: @amount.FormatCurrency()</summary>
    public static string FormatCurrency(this decimal amount) => Format(amount);

    public static string FormatCurrency(this decimal? amount) => Format(amount);

    /// <summary>
    /// Strips currency symbols, spaces, and thousands separators; parses a decimal.
    /// </summary>
    public static decimal Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0m;

        var sb = new StringBuilder(input.Length);
        var seenDot = false;
        foreach (var ch in input.Trim())
        {
            if (char.IsDigit(ch))
            {
                sb.Append(ch);
                continue;
            }

            if (ch == '.' && !seenDot)
            {
                sb.Append('.');
                seenDot = true;
                continue;
            }

            if (ch == '-' && sb.Length == 0)
                sb.Append('-');
            // skip $ , spaces and other symbols
        }

        return decimal.TryParse(
            sb.ToString(),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : 0m;
    }

    public static bool TrySanitize(string? input, out decimal value)
    {
        value = Sanitize(input);
        return !string.IsNullOrWhiteSpace(input) && input.Any(char.IsDigit);
    }
}