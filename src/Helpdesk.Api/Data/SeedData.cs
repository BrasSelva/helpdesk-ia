using System.Text.Json;
using System.Text.Json.Serialization;
using Helpdesk.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Data;

/// <summary>
/// Charge l'historique de tickets fictifs (data/tickets.json) dans une base vide.
/// Le jeu de test (data/tickets-test.json) n'est jamais chargé : il sert à l'évaluation.
/// </summary>
public static class SeedData
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public sealed record SeedTicket(
        string Title,
        string Description,
        TicketCategory Category,
        TicketPriority Priority,
        TicketStatus Status,
        string? Resolution,
        DateTimeOffset CreatedAt);

    public static IReadOnlyList<Ticket> Parse(string json)
    {
        var items = JsonSerializer.Deserialize<List<SeedTicket>>(json, JsonOptions)
            ?? throw new InvalidDataException("Le fichier de tickets est vide ou invalide.");

        return items.Select(s => new Ticket
        {
            Title = s.Title,
            Description = s.Description,
            Category = s.Category,
            Priority = s.Priority,
            Status = s.Status,
            Resolution = s.Resolution,
            CreatedAt = s.CreatedAt.ToUniversalTime()
        }).ToList();
    }

    public static async Task SeedAsync(HelpdeskDbContext db, string path, ILogger logger, CancellationToken ct = default)
    {
        if (await db.Tickets.AnyAsync(ct))
        {
            return;
        }

        if (!File.Exists(path))
        {
            logger.LogWarning("Fichier de données introuvable ({Path}) : lance `python scripts/generate_tickets.py`.", path);
            return;
        }

        var tickets = Parse(await File.ReadAllTextAsync(path, ct));
        db.Tickets.AddRange(tickets);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("{Count} tickets fictifs chargés depuis {Path}.", tickets.Count, path);
    }
}
