using TaskApi.Api.Models;

namespace TaskApi.Api.Rules;

public class TaskRules
{
    public const int MaxTitleLength = 100;

    public IReadOnlyList<string> ValidateCreate(CreateTaskRequest request)
    {
        var errors = new List<string>();
        ValidateTitle(request.Title, errors);
        return errors;
    }

    public IReadOnlyList<string> ValidateUpdate(TaskItem existing, UpdateTaskRequest request)
    {
        var errors = new List<string>();
        ValidateTitle(request.Title, errors);

        if (existing.IsCompleted && request.IsCompleted)
            errors.Add("A tarefa já está concluída e não pode ser concluída novamente.");

        return errors;
    }

    private static void ValidateTitle(string? title, List<string> errors)
    {
        if (string.IsNullOrEmpty(title))
            errors.Add("O título é obrigatório.");
        else if (title.Trim().Length > MaxTitleLength)
            errors.Add($"O título deve ter no máximo {MaxTitleLength} caracteres.");
    }
}
