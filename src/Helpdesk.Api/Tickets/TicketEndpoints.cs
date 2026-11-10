using Helpdesk.Api.Data;
using Helpdesk.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Tickets;

public static class TicketEndpoints
{
    private const int MaxPageSize = 100;

    public static RouteGroupBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tickets").WithTags("Tickets");

        group.MapGet("/", ListTickets).WithName("ListTickets");
        group.MapGet("/{id:int}", GetTicket).WithName("GetTicket");
        group.MapPost("/", CreateTicket).WithName("CreateTicket");
        group.MapPatch("/{id:int}/status", UpdateStatus).WithName("UpdateTicketStatus");

        return group;
    }

    private static async Task<Ok<PagedResult<TicketDto>>> ListTickets(
        HelpdeskDbContext db,
        TicketStatus? status,
        TicketCategory? category,
        string? search,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = db.Tickets.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(t => t.Status == status);
        }

        if (category is not null)
        {
            query = query.Where(t => t.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(t => EF.Functions.ILike(t.Title, pattern) || EF.Functions.ILike(t.Description, pattern));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketDto(t.Id, t.Title, t.Description, t.Category, t.Priority, t.Status, t.Resolution, t.CreatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResult<TicketDto>(items, page, pageSize, total));
    }

    private static async Task<Results<Ok<TicketDto>, NotFound>> GetTicket(int id, HelpdeskDbContext db, CancellationToken ct)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return ticket is null ? TypedResults.NotFound() : TypedResults.Ok(TicketDto.From(ticket));
    }

    private static async Task<Results<Created<TicketDto>, ValidationProblem>> CreateTicket(
        CreateTicketRequest request,
        HelpdeskDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        var errors = TicketRules.ValidateNewTicket(request.Title, request.Description);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var ticket = new Ticket
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = TicketStatus.Open,
            CreatedAt = clock.GetUtcNow()
        };

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/tickets/{ticket.Id}", TicketDto.From(ticket));
    }

    private static async Task<Results<Ok<TicketDto>, NotFound, ValidationProblem>> UpdateStatus(
        int id,
        UpdateStatusRequest request,
        HelpdeskDbContext db,
        CancellationToken ct)
    {
        var errors = TicketRules.ValidateStatusChange(request.Status, request.Resolution);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
        {
            return TypedResults.NotFound();
        }

        ticket.Status = request.Status;
        ticket.Resolution = string.IsNullOrWhiteSpace(request.Resolution) ? ticket.Resolution : request.Resolution.Trim();
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(TicketDto.From(ticket));
    }
}
