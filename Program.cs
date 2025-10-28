using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

// ------------------------------------------------------------
// User Management API
// A minimal ASP.NET Core Web API for managing users in memory.
// Features: API key authentication, CRUD operations, pagination.
// ------------------------------------------------------------
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// API key used for simple authentication during development.
// In production, I would replace with a secure method like JWT or OAuth.
// This approach is acceptable for small-scale or educational projects like this one! :)
var apiKey = "unsafe-secret-key-for-now";

//Middleware to assure correct API key is present during usage
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

//Global exception handler for unexpected errors.
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

//Middleware: to catch and handle exceptions in the pipeline and return JSON error messages.
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

//Middleware: Logs each request with method and statuscode. 
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

//Endpoint: GET
//Fetches all users
// Supports optional pagination via query parameters:
// skip: number of users to skip (for e.g. pagination)
// limit: max number of users to return
// If the parameters are missing, all users are returned.
// example: GET http:localhost:XXXX/users
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

//Landing page, to ensure everything works as intended.
app.MapGet("/", () => "Welcome to user management api");

//Endpoint: GET
//Fetches a specific user.
//param: id - the id of the user we want to fetch.
//example: GET http:localhost:XXXX/users/1
//returns: The specific user if successful.
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

//Endpoint: POST
// Creates a new user if the user does not already exist, based on name and email, 
// and checks if the user is a valid user. 
// Example: 
//          POST http:localhost:XXXX/users.
// Example json raw body: 
//          { "name":"Alice", "department": "IT", "email":"alice@gmail.com"}
// Returns: 201 Created with the new user if successful,
//          400 Bad Request if validation fails,
//          409 Conflict if duplicate user exists.
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

//Endpoint: PUT
//Updates the information of a specific user, matching the id.
//param: id, the specific user's id.
//example: PUT http:localhost:XXXX/users/1 and change name, email or department in the raw json body.
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

//Endpoint: DELETE
// Deletes a specific user based on user ID. 
// param: id - the specific user's ID. 
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

///<summary>
///Validates that user data is filled in correctly.
///Checks that name, department and email exist and that email is valid.
///</summary>
bool IsValidUser(User user)
{
    if (user is null) return false;
    if (string.IsNullOrWhiteSpace(user.Name)) return false;
    if (string.IsNullOrWhiteSpace(user.Department)) return false;
    if (string.IsNullOrWhiteSpace(user.Email)) return false;

    try
    {
        //validate the format of an email address
        //For example. @gmail.com would work. The string "some-email-here" would not
        var addr = new System.Net.Mail.MailAddress(user.Email);
        return addr.Address == user.Email;
    }
    catch
    {
        return false;
    }
}

///<summary>
/// User model.
/// name - the name of the user.
/// department - what department the user works in. 
/// email - the user's email.
///</summary>
record User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Department { get; set; }
    public string Email { get; set; }
}