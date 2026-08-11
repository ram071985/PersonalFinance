using Bunit;
using PersonalFinance.Web.Components;

namespace PersonalFinance.Web.Tests.Components;

[TestFixture]
public class EmptyStateTests
{
    [Test]
    public void Renders_Title_And_Message()
    {
        using var ctx = new Bunit.TestContext();
        var cut = ctx.RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Icon, "🏦")
            .Add(p => p.Title, "No accounts")
            .Add(p => p.Message, "Add an account to get started."));

        cut.Markup.Contains("No accounts");
        Assert.That(cut.Markup, Does.Contain("No accounts"));
        Assert.That(cut.Markup, Does.Contain("Add an account to get started."));
        Assert.That(cut.Markup, Does.Contain("🏦"));
    }

    [Test]
    public void Omits_Message_When_Null()
    {
        using var ctx = new Bunit.TestContext();
        var cut = ctx.RenderComponent<EmptyState>(parameters => parameters
            .Add(p => p.Title, "Nothing here"));

        Assert.That(cut.Markup, Does.Contain("Nothing here"));
        Assert.That(cut.FindAll(".empty-state-message"), Is.Empty);
    }
}
