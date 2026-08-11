using PersonalFinance.Infrastructure.Services;

namespace PersonalFinance.Tests.Services;

[TestFixture]
public class NotificationServiceTests
{
    [TestCase("+15551234567", "+15551234567")]
    [TestCase("5551234567", "+15551234567")]
    [TestCase("1-555-123-4567", "+15551234567")]
    [TestCase("(555) 123-4567", "+15551234567")]
    [TestCase("+44 7700 900123", "+447700900123")]
    public void NormalizePhone_Valid_ReturnsE164(string input, string expected)
    {
        Assert.That(NotificationService.NormalizePhone(input), Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("123")]
    [TestCase("not-a-phone")]
    [TestCase(null)]
    public void NormalizePhone_Invalid_ReturnsNull(string? input)
    {
        Assert.That(NotificationService.NormalizePhone(input!), Is.Null);
    }
}