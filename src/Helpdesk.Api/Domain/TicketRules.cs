namespace Helpdesk.Api.Domain;

/// <summary>
/// Règles métier des tickets, sans dépendance à la base : testables unitairement.
/// </summary>
public static class TicketRules
{
    public const int TitleMinLength = 5;
    public const int TitleMaxLength = 200;
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 4000;
    public const int ResolutionMaxLength = 4000;

    public static Dictionary<string, string[]> ValidateNewTicket(string? title, string? description)
    {
        var errors = new Dictionary<string, string[]>();

        var cleanTitle = title?.Trim() ?? string.Empty;
        if (cleanTitle.Length < TitleMinLength || cleanTitle.Length > TitleMaxLength)
        {
            errors["title"] = [$"Le titre doit faire entre {TitleMinLength} et {TitleMaxLength} caractères."];
        }

        var cleanDescription = description?.Trim() ?? string.Empty;
        if (cleanDescription.Length < DescriptionMinLength || cleanDescription.Length > DescriptionMaxLength)
        {
            errors["description"] = [$"La description doit faire entre {DescriptionMinLength} et {DescriptionMaxLength} caractères."];
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateStatusChange(TicketStatus newStatus, string? resolution)
    {
        var errors = new Dictionary<string, string[]>();
        var cleanResolution = resolution?.Trim() ?? string.Empty;

        if (newStatus == TicketStatus.Resolved && cleanResolution.Length == 0)
        {
            errors["resolution"] = ["Une résolution est obligatoire pour clore un ticket."];
        }

        if (cleanResolution.Length > ResolutionMaxLength)
        {
            errors["resolution"] = [$"La résolution ne doit pas dépasser {ResolutionMaxLength} caractères."];
        }

        return errors;
    }
}
