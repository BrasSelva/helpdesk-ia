using Helpdesk.Api.Domain;

namespace Helpdesk.Api.Tests;

public class TicketRulesTests
{
    [Fact]
    public void ValidTicket_HasNoErrors()
    {
        var errors = TicketRules.ValidateNewTicket("VPN impossible", "Le VPN refuse la connexion depuis ce matin.");

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("VPN")]
    public void TooShortTitle_IsRejected(string? title)
    {
        var errors = TicketRules.ValidateNewTicket(title, "Une description suffisamment longue.");

        Assert.Contains("title", errors.Keys);
    }

    [Fact]
    public void TitleIsTrimmedBeforeLengthCheck()
    {
        var errors = TicketRules.ValidateNewTicket("   abc   ", "Une description suffisamment longue.");

        Assert.Contains("title", errors.Keys);
    }

    [Fact]
    public void TooLongDescription_IsRejected()
    {
        var description = new string('a', TicketRules.DescriptionMaxLength + 1);

        var errors = TicketRules.ValidateNewTicket("Titre valide", description);

        Assert.Contains("description", errors.Keys);
    }

    [Fact]
    public void ResolvingWithoutResolution_IsRejected()
    {
        var errors = TicketRules.ValidateStatusChange(TicketStatus.Resolved, "  ");

        Assert.Contains("resolution", errors.Keys);
    }

    [Theory]
    [InlineData(TicketStatus.Open, null)]
    [InlineData(TicketStatus.InProgress, null)]
    [InlineData(TicketStatus.Resolved, "Profil VPN réinstallé.")]
    public void ValidStatusChanges_HaveNoErrors(TicketStatus status, string? resolution)
    {
        Assert.Empty(TicketRules.ValidateStatusChange(status, resolution));
    }
}
