using PersonalFinance.Core.Enums;

namespace PersonalFinance.Web.Helpers;

/// <summary>
/// Enum-backed dropdown options. Keeps Enum.GetValues out of .razor files
/// so the Razor editor doesn't hit ambiguous Core type resolution.
/// </summary>
public static class EnumOptions
{
    public static IReadOnlyList<Option> AccountTypes { get; } =
        Enum.GetValues<AccountType>()
            .Select(t => new Option((int)t, t.ToString()))
            .ToList();

    public static IReadOnlyList<Option> CategoryTypes { get; } =
        Enum.GetValues<CategoryType>()
            .Select(t => new Option((int)t, t.ToString()))
            .ToList();

    public static IReadOnlyList<Option> TransactionTypes { get; } =
        Enum.GetValues<TransactionType>()
            .Select(t => new Option((int)t, t.ToString()))
            .ToList();

    public readonly record struct Option(int Value, string Name);
}