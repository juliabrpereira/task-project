using System.Collections.Concurrent;
using TaskApi.Api.Models;

namespace TaskApi.Api.Data;

public class InMemoryTaskRepository
{
    private readonly ConcurrentDictionary<int, TaskItem> _tasks = new();
    private int _nextId;

    public IEnumerable<TaskItem> GetAll() => _tasks.Values.OrderBy(t => t.Id);

    public TaskItem? GetById(int id) => _tasks.GetValueOrDefault(id);

    public TaskItem Add(string title, string? description)
    {
        var task = new TaskItem
        {
            Id = Interlocked.Increment(ref _nextId),
            Title = title,
            Description = description,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _tasks[task.Id] = task;
        return task;
    }

    public bool Remove(int id) => _tasks.TryRemove(id, out _);
}
