using PersonalFinance.Web.Helpers;

namespace PersonalFinance.Web.Tests.Helpers;

[TestFixture]
public class CurrencyFormatTests
{
    [Test]
    public void Format_UsesCurrencyStyle()
    {
        var s = CurrencyFormat.Format(1234.5m);
        Assert.That(s, Does.Contain("1,234.50").Or.Contain("1234.50"));
    }

    [TestCase("$1,234.56", 1234.56)]
    [TestCase("(50.00)", -50.00)]
    [TestCase("  12.5 ", 12.5)]
    [TestCase("-$10", -10)]
    [TestCase("0", 0)]
    [TestCase("", 0)]
    [TestCase(null, 0)]
    public void Sanitize_StripsSymbols(string? input, decimal expected)
    {
        Assert.That(CurrencyFormat.Sanitize(input), Is.EqualTo(expected));
    }

    [Test]
    public void FormatCurrency_Extension_MatchesFormat()
    {
        Assert.That(99.9m.FormatCurrency(), Is.EqualTo(CurrencyFormat.Format(99.9m)));
    }
}