using System.Globalization;
using System.Text;

namespace PersonalFinance.Core.Services;

/// <summary>
/// Flexible bank CSV parser — no third-party CSV lib.
/// Supports common headers: Date/Transaction Date, Description/Memo/Payee,
/// Amount, or separate Debit/Credit columns.
/// </summary>
public static class BankStatementCsvParser
{
    public sealed record ParsedRow(DateTime Date, string Description, decimal SignedAmount);

    public static IReadOnlyList<ParsedRow> Parse(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line.Trim());
        }

        if (lines.Count < 2)
            return Array.Empty<ParsedRow>();

        var header = SplitCsvLine(lines[0]);
        var map = MapColumns(header);
        if (map.DateIndex < 0 || map.DescriptionIndex < 0)
            throw new InvalidOperationException(
                "CSV must include Date and Description (or Memo/Payee) columns.");

        if (map.AmountIndex < 0 && map.DebitIndex < 0 && map.CreditIndex < 0)
            throw new InvalidOperationException(
                "CSV must include Amount, or Debit/Credit columns.");

        var rows = new List<ParsedRow>();
        for (var i = 1; i < lines.Count; i++)
        {
            var cols = SplitCsvLine(lines[i]);
            if (cols.Count == 0) continue;

            try
            {
                var dateStr = Get(cols, map.DateIndex);
                if (!TryParseDate(dateStr, out var date))
                    continue;

                var desc = Get(cols, map.DescriptionIndex).Trim();
                if (string.IsNullOrWhiteSpace(desc))
                    desc = "Imported";

                decimal amount;
                if (map.AmountIndex >= 0)
                {
                    amount = ParseMoney(Get(cols, map.AmountIndex));
                }
                else
                {
                    var debit = map.DebitIndex >= 0 ? ParseMoney(Get(cols, map.DebitIndex)) : 0m;
                    var credit = map.CreditIndex >= 0 ? ParseMoney(Get(cols, map.CreditIndex)) : 0m;
                    amount = credit - debit;
                    if (amount == 0 && debit != 0)
                        amount = -Math.Abs(debit);
                }

                if (amount == 0) continue;
                rows.Add(new ParsedRow(date, desc, amount));
            }
            catch
            {
                // skip bad row
            }
        }

        return rows;
    }

    private sealed class ColumnMap
    {
        public int DateIndex = -1;
        public int DescriptionIndex = -1;
        public int AmountIndex = -1;
        public int DebitIndex = -1;
        public int CreditIndex = -1;
    }

    private static ColumnMap MapColumns(IReadOnlyList<string> header)
    {
        var map = new ColumnMap();
        for (var i = 0; i < header.Count; i++)
        {
            var h = header[i].Trim().Trim('"').ToLowerInvariant();
            if (map.DateIndex < 0 && (h is "date" or "transaction date" or "posted date" or "posting date" or "trans date"))
                map.DateIndex = i;
            else if (map.DescriptionIndex < 0 && (h is "description" or "memo" or "payee" or "narrative" or "details" or "name"))
                map.DescriptionIndex = i;
            else if (map.AmountIndex < 0 && (h is "amount" or "transaction amount" or "value"))
                map.AmountIndex = i;
            else if (map.DebitIndex < 0 && (h is "debit" or "withdrawal" or "out"))
                map.DebitIndex = i;
            else if (map.CreditIndex < 0 && (h is "credit" or "deposit" or "in"))
                map.CreditIndex = i;
        }
        return map;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                    inQuotes = !inQuotes;
                continue;
            }
            if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }
            sb.Append(c);
        }
        result.Add(sb.ToString());
        return result;
    }

    private static string Get(IReadOnlyList<string> cols, int index) =>
        index >= 0 && index < cols.Count ? cols[index].Trim().Trim('"') : "";

    private static bool TryParseDate(string value, out DateTime date)
    {
        var formats = new[]
        {
            "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy", "dd/MM/yyyy", "d/M/yyyy",
            "MM-dd-yyyy", "dd-MM-yyyy", "yyyy/MM/dd", "M/d/yy", "MM/dd/yy"
        };
        return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture,
                   DateTimeStyles.None, out date)
               || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
               || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
    }

    private static decimal ParseMoney(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0m;
        var cleaned = value.Trim()
            .Replace("$", "", StringComparison.Ordinal)
            .Replace("(", "-", StringComparison.Ordinal)
            .Replace(")", "", StringComparison.Ordinal)
            .Replace(",", "", StringComparison.Ordinal)
            .Replace(" ", "", StringComparison.Ordinal);
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
            ? d
            : 0m;
    }
}
