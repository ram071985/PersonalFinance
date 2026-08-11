using System.Text;
using PersonalFinance.Core.Services;

namespace PersonalFinance.Tests.Services;

[TestFixture]
public class BankStatementCsvParserTests
{
    [Test]
    public void Parse_AmountColumn_SignedAmounts()
    {
        var csv = """
                  Date,Description,Amount
                  2026-01-15,Coffee,-4.50
                  2026-01-16,Payroll,2000.00
                  """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = BankStatementCsvParser.Parse(stream);

        Assert.That(rows.Count, Is.EqualTo(2));
        Assert.That(rows[0].Description, Is.EqualTo("Coffee"));
        Assert.That(rows[0].SignedAmount, Is.EqualTo(-4.50m));
        Assert.That(rows[1].SignedAmount, Is.EqualTo(2000m));
    }

    [Test]
    public void Parse_DebitCreditColumns()
    {
        var csv = """
                  Transaction Date,Payee,Debit,Credit
                  01/20/2026,Grocery Store,55.10,
                  01/21/2026,Employer,,1500
                  """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = BankStatementCsvParser.Parse(stream);

        Assert.That(rows.Count, Is.EqualTo(2));
        Assert.That(rows[0].SignedAmount, Is.EqualTo(-55.10m));
        Assert.That(rows[1].SignedAmount, Is.EqualTo(1500m));
    }

    [Test]
    public void Parse_MissingRequiredColumns_Throws()
    {
        var csv = "Foo,Bar\n1,2\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        Assert.Throws<InvalidOperationException>(() => BankStatementCsvParser.Parse(stream));
    }
}