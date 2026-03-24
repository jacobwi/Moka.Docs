// SampleApi — A minimal API project showcasing MokaDocs ASP.NET Core integration

using Moka.Docs.AspNetCore;
using SampleApi.Models;
using SampleApi.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddSingleton<TodoService>();
builder.Services.AddSingleton<WeatherService>();

// Register MokaDocs — auto-discovers API from this assembly
builder.Services.AddMokaDocs(options =>
{
    options.Title = "SampleApi Docs";
    options.Description = "API documentation for the SampleApi demo project";
    options.Assemblies = [typeof(TodoItem).Assembly];
    options.PrimaryColor = "#0ea5e9";
    options.DocsPath = "./docs";
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

// Serve MokaDocs documentation at /docs
app.MapMokaDocs("/docs");

// ── Todo endpoints ──────────────────────────────────────────
var todos = app.MapGroup("/api/todos")
    .WithTags("Todos");

todos.MapGet("/", (TodoService svc) => svc.GetAll())
    .WithSummary("Get all todos")
    .WithDescription("Returns all todo items, ordered by creation date.");

todos.MapGet("/{id:int}", (int id, TodoService svc) =>
        svc.GetById(id) is { } todo
            ? Results.Ok(todo)
            : Results.NotFound())
    .WithSummary("Get a todo by ID");

todos.MapPost("/", (CreateTodoRequest request, TodoService svc) =>
    {
        var todo = svc.Create(request);
        return Results.Created($"/api/todos/{todo.Id}", todo);
    })
    .WithSummary("Create a new todo")
    .WithDescription("Creates a new todo item. The ID is auto-generated.");

todos.MapPut("/{id:int}", (int id, UpdateTodoRequest request, TodoService svc) =>
        svc.Update(id, request) ? Results.NoContent() : Results.NotFound())
    .WithSummary("Update an existing todo");

todos.MapDelete("/{id:int}", (int id, TodoService svc) =>
        svc.Delete(id) ? Results.NoContent() : Results.NotFound())
    .WithSummary("Delete a todo");

// ── Weather endpoints ───────────────────────────────────────
var weather = app.MapGroup("/api/weather")
    .WithTags("Weather");

weather.MapGet("/forecast", (WeatherService svc, int days = 5) =>
        svc.GetForecast(days))
    .WithSummary("Get weather forecast")
    .WithDescription("Returns a weather forecast for the specified number of days (default: 5).");

weather.MapGet("/current", (WeatherService svc) =>
        svc.GetCurrent())
    .WithSummary("Get current weather conditions");

app.Run();