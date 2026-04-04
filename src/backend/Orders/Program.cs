using Microsoft.EntityFrameworkCore;
using Orders.Composition.DependencyInjection;
using Orders.Data;
using Orders.Middleware;
using Shared.Logging.Extensions;
using Shared.OpenTelemetry.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddOpenTelemetryAutoInstrumentation("orders", "1.0.0");
builder.Services.AddOrdersApplication(builder.Configuration, builder.Environment);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseOpenTelemetryMiddleware();
app.UseLoggingMiddleware();
app.UseSerilogMiddleware();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsEnvironment("IntegrationTest"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
    await context.Database.MigrateAsync();

    var seeder = new DatabaseSeeder(context, app.Environment.ContentRootPath, logger);
    await seeder.SeedAsync();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

app.Run();

public partial class Program;
