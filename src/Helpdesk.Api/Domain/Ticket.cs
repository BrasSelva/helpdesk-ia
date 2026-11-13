namespace Helpdesk.Api.Domain;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter<TicketCategory>))]
public enum TicketCategory
{
    Network,
    Account,
    Hardware,
    Software,
    Printing,
    Messaging
}

[JsonConverter(typeof(JsonStringEnumConverter<TicketPriority>))]
public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical
}

[JsonConverter(typeof(JsonStringEnumConverter<TicketStatus>))]
public enum TicketStatus
{
    Open,
    InProgress,
    Resolved
}

public class Ticket
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    /// <summary>
    /// Null tant que le ticket n'a pas été classé (manuellement ou, à partir de la semaine 2, par le modèle).
    /// </summary>
    public TicketCategory? Category { get; set; }

    public TicketPriority? Priority { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public string? Resolution { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
