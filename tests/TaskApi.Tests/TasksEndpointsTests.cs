using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskApi.Api.Models;

namespace TaskApi.Tests;

public class TasksEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TasksEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<TaskItem> CreateTaskAsync(string title)
    {
        var response = await _client.PostAsJsonAsync("/tasks", new CreateTaskRequest(title, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskItem>())!;
    }

    [Fact]
    public async Task GetHealth_Retorna200()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetHealth_ComMemoriaAcimaDoLimite_Retorna503()
    {
        var client = _factory
            .WithWebHostBuilder(b => b.UseSetting("HealthChecks:MemoryLimitMB", "0"))
            .CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetTasks_Retorna200()
    {
        var response = await _client.GetAsync("/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostTask_ComTituloValido_Retorna201ComTarefa()
    {
        var response = await _client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Nova tarefa", "Descrição"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskItem>();
        Assert.NotNull(task);
        Assert.Equal("Nova tarefa", task.Title);
        Assert.False(task.IsCompleted);
        Assert.Equal($"/tasks/{task.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task PostTask_ComTituloVazio_Retorna400()
    {
        var response = await _client.PostAsJsonAsync("/tasks", new CreateTaskRequest("", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTaskPorId_Inexistente_Retorna404()
    {
        var response = await _client.GetAsync("/tasks/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutTask_ConcluirTarefaJaConcluida_Retorna400()
    {
        var task = await CreateTaskAsync("Tarefa para concluir");
        var request = new UpdateTaskRequest(task.Title, null, true);

        var first = await _client.PutAsJsonAsync($"/tasks/{task.Id}", request);
        var second = await _client.PutAsJsonAsync($"/tasks/{task.Id}", request);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_RemoveATarefa()
    {
        var task = await CreateTaskAsync("Tarefa para remover");

        var deleteResponse = await _client.DeleteAsync($"/tasks/{task.Id}");
        var getResponse = await _client.GetAsync($"/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
