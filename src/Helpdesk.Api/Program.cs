using System.Text.Json.Serialization;
using Helpdesk.Api.Data;
using Helpdesk.Api.Tickets;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<HelpdeskDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Helpdesk"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5)));

var frontOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddPolicy("front", policy =>
    policy.WithOrigins(frontOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("front");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");
app.MapTicketEndpoints();

// Semaine 1 : schéma créé au démarrage et historique fictif chargé si la base est vide.
// Les migrations EF Core remplaceront EnsureCreated quand pgvector arrivera (semaine 3).
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var seedPath = Path.GetFullPath(Path.Combine(
        app.Environment.ContentRootPath,
        app.Configuration["Seed:TicketsPath"] ?? "../../data/tickets.json"));

    await db.Database.EnsureCreatedAsync();
    await SeedData.SeedAsync(db, seedPath, logger);
}

app.Run();

public partial class Program;
