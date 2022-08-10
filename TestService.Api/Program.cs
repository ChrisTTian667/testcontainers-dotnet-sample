using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContextFactory<TestDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("TestDb"));
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapPost("/message", async (IDbContextFactory<TestDbContext> dbContextFactory, [FromBody] Message message) =>
{
    var dbContext = await dbContextFactory.CreateDbContextAsync();
    dbContext.Messages.Add(message);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/message/{message.Id}", message);
});

app.MapGet("/message/{id:int}", async (IDbContextFactory<TestDbContext> dbContextFactory, [FromRoute] int id) =>
{
    var dbContext = await dbContextFactory.CreateDbContextAsync();
    var message = await dbContext.Messages.FirstOrDefaultAsync(message => message.Id == id);

    return JsonSerializer.Serialize(message);
});

app.MapDelete("/message/{id:int}", async (IDbContextFactory<TestDbContext> dbContextFactory, [FromRoute] int id) =>
{
    var dbContext = await dbContextFactory.CreateDbContextAsync();

    var message = await dbContext.Messages.FirstOrDefaultAsync(message => message.Id == id);
    if (message == null)
        return Results.NotFound();

    dbContext.Messages.Remove(message);
    await dbContext.SaveChangesAsync();

    return Results.Ok();
});

app.MapGet("/messages", async (IDbContextFactory<TestDbContext> dbContextFactory) =>
{
    var dbContext = await dbContextFactory.CreateDbContextAsync();
    var messages = await dbContext.Messages.ToListAsync();

    return JsonSerializer.Serialize(messages);
});


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
    db.Database.Migrate();
}

app.Run();

// This is required to let the web application factory access the program
// ReSharper disable once ClassNeverInstantiated.Global
public partial class Program
{
}