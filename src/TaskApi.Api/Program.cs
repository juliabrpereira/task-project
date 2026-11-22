using TaskApi.Api.Data;
using TaskApi.Api.Models;
using TaskApi.Api.Rules;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryTaskRepository>();
builder.Services.AddSingleton<TaskRules>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var tasks = app.MapGroup("/tasks");

tasks.MapGet("/", (InMemoryTaskRepository repo) => Results.Ok(repo.GetAll()));

tasks.MapGet("/{id:int}", (int id, InMemoryTaskRepository repo) =>
    repo.GetById(id) is { } task ? Results.Ok(task) : Results.NotFound());

tasks.MapPost("/", (CreateTaskRequest request, InMemoryTaskRepository repo, TaskRules rules) =>
{
    var errors = rules.ValidateCreate(request);
    if (errors.Count > 0)
        return ValidationError(errors);

    var task = repo.Add(request.Title!.Trim(), request.Description);
    return Results.Created($"/tasks/{task.Id}", task);
});

tasks.MapPut("/{id:int}", (int id, UpdateTaskRequest request, InMemoryTaskRepository repo, TaskRules rules) =>
{
    var task = repo.GetById(id);
    if (task is null)
        return Results.NotFound();

    var errors = rules.ValidateUpdate(task, request);
    if (errors.Count > 0)
        return ValidationError(errors);

    task.Title = request.Title!.Trim();
    task.Description = request.Description;
    task.IsCompleted = request.IsCompleted;
    return Results.Ok(task);
});

tasks.MapDelete("/{id:int}", (int id, InMemoryTaskRepository repo) =>
    repo.Remove(id) ? Results.NoContent() : Results.NotFound());

app.Run();

static IResult ValidationError(IReadOnlyList<string> errors) =>
    Results.ValidationProblem(new Dictionary<string, string[]> { ["task"] = errors.ToArray() });

public partial class Program { }
