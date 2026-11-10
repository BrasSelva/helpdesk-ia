using Helpdesk.Api.Data;
using Helpdesk.Api.Domain;

namespace Helpdesk.Api.Tests;

public class SeedDataTests
{
    [Fact]
    public void Parse_ReadsEnumsAsTextAndConvertsDatesToUtc()
    {
        const string json = """
            [{
              "title": "VPN impossible à connecter",
              "description": "Le VPN refuse la connexion.",
              "category": "Network",
              "priority": "High",
              "status": "Resolved",
              "resolution": "Profil réinstallé.",
              "createdAt": "2026-10-01T10:00:00+02:00"
            }]
            """;

        var ticket = Assert.Single(SeedData.Parse(json));

        Assert.Equal(TicketCategory.Network, ticket.Category);
        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Equal(TicketStatus.Resolved, ticket.Status);
        Assert.Equal(TimeSpan.Zero, ticket.CreatedAt.Offset);
        Assert.Equal(8, ticket.CreatedAt.Hour);
    }

    [Theory]
    [InlineData("tickets.json", 400)]
    [InlineData("tickets-test.json", 100)]
    public void GeneratedDatasets_AreValidAndCoverEveryCategory(string fileName, int expectedCount)
    {
        var tickets = SeedData.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "data", fileName)));

        Assert.Equal(expectedCount, tickets.Count);
        Assert.All(tickets, t => Assert.Empty(TicketRules.ValidateNewTicket(t.Title, t.Description)));
        Assert.Equal(Enum.GetValues<TicketCategory>().Length, tickets.Select(t => t.Category).Distinct().Count());
    }

    [Fact]
    public void TestSet_DoesNotLeakIntoHistory()
    {
        var root = RepositoryRoot();
        var history = SeedData.Parse(File.ReadAllText(Path.Combine(root, "data", "tickets.json")));
        var test = SeedData.Parse(File.ReadAllText(Path.Combine(root, "data", "tickets-test.json")));

        var historyKeys = history.Select(Key).ToHashSet();

        Assert.DoesNotContain(test, t => historyKeys.Contains(Key(t)));

        static string Key(Ticket t) => $"{t.Title}|{t.Description}|{t.CreatedAt:O}";
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "HelpdeskIA.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Racine du dépôt introuvable (HelpdeskIA.slnx).");
    }
}
