using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

//Enkel API-nyckel-autentisering
var apiKey = "unsafe-secret-key-for-now"; 

app.Use(async (context, next) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

    if (authHeader != $"Bearer {apiKey}")
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Unauthorized. Invalid API key.",
            path = context.Request.Path,
            timestamp = DateTime.UtcNow
        });
        return;
    }

    await next();
});

//Global felhantering
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = "https://httpstatuses.com/500",
            title = "Internal Server Error",
            status = 500,
            detail = "Ett oväntat fel inträffade.",
            instance = context.Request.Path,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(problem);
    });
});

//Custom exception middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = "Internal server error.",
            detail = ex.Message,
            path = context.Request.Path,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }
});

//Logging
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var path = context.Request.Path;

    await next();

    var statusCode = context.Response.StatusCode;
    Console.WriteLine($"[Request] {method} {path} → {statusCode}");
});

// In-memory user store
var users = new ConcurrentDictionary<int, User>();
var nextId = 1;

//GET all users
app.MapGet("/users", (HttpRequest request) =>
{
    var skipStr = request.Query["skip"];
    var limitStr = request.Query["limit"];

    int.TryParse(skipStr, out int skip);
    int.TryParse(limitStr, out int limit);

    var result = users.Values
        .Skip(skip > 0 ? skip : 0)
        .Take(limit > 0 ? limit : users.Count);

    return Results.Ok(result);
});

//Landing page
app.MapGet("/", () => "Welcome to user management api");

//GET user by ID
app.MapGet("/users/{id:int}", (int id) =>
{
    try
    {
        return users.TryGetValue(id, out var user)
            ? Results.Ok(user)
            : Results.NotFound(new { error = $"User with ID {id} not found." });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Unexpected error: {ex.Message}");
    }
});

//POST create new user
app.MapPost("/users", async (HttpRequest request) =>
{
    try
    {
        var user = await request.ReadFromJsonAsync<User>();
        if (!IsValidUser(user))
            return Results.BadRequest("Invalid user data. Name, Department, and a valid Email are required.");

        // Avoid duplicates :) 
        var duplicate = users.Values.Any(u =>
            string.Equals(u.Name, user.Name, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(u.Email, user.Email, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            return Results.Conflict(new
            {
                error = "A user with the same name and email already exists.",
                name = user.Name,
                email = user.Email
            });
        }

        user.Id = nextId++;
        users[user.Id] = user;
        return Results.Created($"/users/{user.Id}", user);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Unexpected error: {ex.Message}");
    }
});

//PUT update user
app.MapPut("/users/{id:int}", async (int id, HttpRequest request) =>
{
    try
    {
        if (!users.ContainsKey(id))
            return Results.NotFound(new { error = $"User with ID {id} not found." });

        var updatedUser = await request.ReadFromJsonAsync<User>();
        if (!IsValidUser(updatedUser))
            return Results.BadRequest("Invalid user data. Name, Department, and a valid Email are required.");

        updatedUser.Id = id;
        users[id] = updatedUser;
        return Results.Ok(updatedUser);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Unexpected error: {ex.Message}");
    }
});

//DELETE user
app.MapDelete("/users/{id:int}", (int id) =>
{
    try
    {
        return users.TryRemove(id, out var removed)
            ? Results.Ok(removed)
            : Results.NotFound($"User with ID {id} not found.");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Unexpected error: {ex.Message}");
    }
});

app.Run();

//Valideringsfunktion
bool IsValidUser(User user)
{
    if (user is null) return false;
    if (string.IsNullOrWhiteSpace(user.Name)) return false;
    if (string.IsNullOrWhiteSpace(user.Department)) return false;
    if (string.IsNullOrWhiteSpace(user.Email)) return false;

    try
    {
        var addr = new System.Net.Mail.MailAddress(user.Email);
        return addr.Address == user.Email;
    }
    catch
    {
        return false;
    }
}

//User-model
record User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Department { get; set; }
    public string Email { get; set; }
}