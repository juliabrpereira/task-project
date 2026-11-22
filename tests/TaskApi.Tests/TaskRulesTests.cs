using TaskApi.Api.Models;
using TaskApi.Api.Rules;

namespace TaskApi.Tests;

public class TaskRulesTests
{
    private readonly TaskRules _rules = new();

    [Fact]
    public void ValidateCreate_ComTituloValido_NaoRetornaErros()
    {
        var errors = _rules.ValidateCreate(new CreateTaskRequest("Estudar DevOps", null));

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_ComTituloVazio_RetornaErro(string? title)
    {
        var errors = _rules.ValidateCreate(new CreateTaskRequest(title, null));

        Assert.Contains("O título é obrigatório.", errors);
    }

    [Fact]
    public void ValidateCreate_ComTituloMuitoLongo_RetornaErro()
    {
        var title = new string('a', TaskRules.MaxTitleLength + 1);

        var errors = _rules.ValidateCreate(new CreateTaskRequest(title, null));

        Assert.Single(errors);
    }

    [Fact]
    public void ValidateUpdate_ConcluirTarefaPendente_NaoRetornaErros()
    {
        var task = new TaskItem { Id = 1, Title = "Tarefa", IsCompleted = false };

        var errors = _rules.ValidateUpdate(task, new UpdateTaskRequest("Tarefa", null, true));

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateUpdate_ConcluirTarefaJaConcluida_RetornaErro()
    {
        var task = new TaskItem { Id = 1, Title = "Tarefa", IsCompleted = true };

        var errors = _rules.ValidateUpdate(task, new UpdateTaskRequest("Tarefa", null, true));

        Assert.Contains("A tarefa já está concluída e não pode ser concluída novamente.", errors);
    }
}
