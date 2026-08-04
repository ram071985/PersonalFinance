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
    /// Supports accounting negatives: (50.00) → -50.00
    /// </summary>
    public static decimal Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0m;

        var trimmed = input.Trim();
        var negative = false;

        // Accounting format: (50.00) means -50.00
        if (trimmed.StartsWith('(') && trimmed.EndsWith(')'))
        {
            negative = true;
            trimmed = trimmed[1..^1].Trim();
        }

        var sb = new StringBuilder(trimmed.Length);
        var seenDot = false;
        foreach (var ch in trimmed)
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
            {
                negative = true;
                continue;
            }

            // skip $ , spaces and other symbols
        }

        if (!decimal.TryParse(
                sb.ToString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var value))
            return 0m;

        return negative ? -Math.Abs(value) : value;
    }

    public static bool TrySanitize(string? input, out decimal value)
    {
        value = Sanitize(input);
        return !string.IsNullOrWhiteSpace(input) && input.Any(char.IsDigit);
    }
}
