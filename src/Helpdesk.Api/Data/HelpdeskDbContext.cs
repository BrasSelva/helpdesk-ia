using Helpdesk.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Api.Data;

public class HelpdeskDbContext(DbContextOptions<HelpdeskDbContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var ticket = modelBuilder.Entity<Ticket>();

        ticket.ToTable("tickets");
        ticket.Property(t => t.Title).HasMaxLength(TicketRules.TitleMaxLength);
        ticket.Property(t => t.Description).HasMaxLength(TicketRules.DescriptionMaxLength);

        // Enums stockés en texte : lisibles en SQL et robustes si l'ordre des valeurs change.
        ticket.Property(t => t.Category).HasConversion<string>().HasMaxLength(32);
        ticket.Property(t => t.Priority).HasConversion<string>().HasMaxLength(32);
        ticket.Property(t => t.Status).HasConversion<string>().HasMaxLength(32);

        ticket.HasIndex(t => t.Status);
        ticket.HasIndex(t => t.Category);
    }
}
