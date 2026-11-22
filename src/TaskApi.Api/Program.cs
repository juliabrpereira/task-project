using TaskApi.Api.Data;
using TaskApi.Api.Health;
using TaskApi.Api.Models;
using TaskApi.Api.Rules;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryTaskRepository>();
builder.Services.AddSingleton<TaskRules>();
builder.Services.AddHealthChecks()
    .AddCheck<MemoryHealthCheck>("memoria");

var app = builder.Build();

app.MapHealthChecks("/health");

var tasks = app.MapGroup("/tasks");

tasks.MapGet("/", (InMemoryTaskRepository repo) => Results.Ok(repo.GetAll()));

tasks.MapGet("/{id:int}", (int id, InMemoryTaskRepository repo, ILogger<Program> logger) =>
{
    var task = repo.GetById(id);
    if (task is null)
    {
        logger.LogWarning("Tarefa {TaskId} não encontrada", id);
        return Results.NotFound();
    }

    return Results.Ok(task);
});

tasks.MapPost("/", (CreateTaskRequest request, InMemoryTaskRepository repo, TaskRules rules, ILogger<Program> logger) =>
{
    var errors = rules.ValidateCreate(request);
    if (errors.Count > 0)
    {
        logger.LogWarning("Validação falhou ao criar tarefa: {Errors}", errors);
        return ValidationError(errors);
    }

    var task = repo.Add(request.Title!.Trim(), request.Description);
    logger.LogInformation("Tarefa {TaskId} criada com título {Title}", task.Id, task.Title);
    return Results.Created($"/tasks/{task.Id}", task);
});

tasks.MapPut("/{id:int}", (int id, UpdateTaskRequest request, InMemoryTaskRepository repo, TaskRules rules, ILogger<Program> logger) =>
{
    var task = repo.GetById(id);
    if (task is null)
    {
        logger.LogWarning("Tarefa {TaskId} não encontrada para atualização", id);
        return Results.NotFound();
    }

    var errors = rules.ValidateUpdate(task, request);
    if (errors.Count > 0)
    {
        logger.LogWarning("Validação falhou ao atualizar tarefa {TaskId}: {Errors}", id, errors);
        return ValidationError(errors);
    }

    task.Title = request.Title!.Trim();
    task.Description = request.Description;
    task.IsCompleted = request.IsCompleted;
    logger.LogInformation("Tarefa {TaskId} atualizada (concluída: {IsCompleted})", task.Id, task.IsCompleted);
    return Results.Ok(task);
});

tasks.MapDelete("/{id:int}", (int id, InMemoryTaskRepository repo, ILogger<Program> logger) =>
{
    if (!repo.Remove(id))
    {
        logger.LogWarning("Tarefa {TaskId} não encontrada para remoção", id);
        return Results.NotFound();
    }

    logger.LogInformation("Tarefa {TaskId} removida", id);
    return Results.NoContent();
});

app.Run();

static IResult ValidationError(IReadOnlyList<string> errors) =>
    Results.ValidationProblem(new Dictionary<string, string[]> { ["task"] = errors.ToArray() });

public partial class Program { }
