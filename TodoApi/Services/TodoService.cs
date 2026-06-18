using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Repositories;
using TodoApi.Telemetry;

namespace TodoApi.Services;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _repository;
    
    private readonly ILogger<TodoService> _logger;
    
    private readonly IDistributedCache _cache;

    public TodoService(
        ITodoRepository repository,
        ILogger<TodoService> logger,
        IDistributedCache cache)
    {
        _repository = repository;
        _logger = logger;
        _cache = cache;
    }

    public async Task<List<TodoResponse>> GetAllAsync()
    {
        var cached = await _cache.GetStringAsync("todos");
        
        using var activity = TelemetryConstants.ActivitySource.StartActivity("GetAllTodos");

        if (!string.IsNullOrEmpty(cached))
        {
            activity?.SetTag("cache.enabled", true);
            return JsonSerializer.Deserialize<List<TodoResponse>>(cached)!;
        }
        
        var todos = await _repository.GetAllAsync();
        
        await _cache.SetStringAsync(
            "todos",
            JsonSerializer.Serialize(todos),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
        
        _logger.LogInformation("Retrieved {Count} todos from cache", todos.Count);
        
        activity?.SetTag("operation.type", "database");
        
        return todos.Select(x => new TodoResponse
        {
            Id = x.Id,
            Title = x.Title,
            IsCompleted = x.IsCompleted,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<TodoResponse> CreateAsync(CreateTodoRequest request)
    {
        _logger.LogInformation("Creating todo with title {Title}", request.Title);
        
        var todo = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            CreatedAt = DateTime.UtcNow,
            IsCompleted = false
        };

        await _repository.AddAsync(todo);

        await _repository.SaveChangesAsync();
        
        await _cache.RemoveAsync("todos");
        
        _logger.LogInformation("Todo created with id {TodoId}", todo.Id);

        return new TodoResponse
        {
            Id = todo.Id,
            Title = todo.Title,
            IsCompleted = todo.IsCompleted,
            CreatedAt = todo.CreatedAt
        };
    }

    public async Task<TodoResponse?> GetByIdAsync(Guid id)
    {
        var todo = await _repository.GetByIdAsync(id);

        if (todo is null)
            return null;

        return new TodoResponse
        {
            Id = todo.Id,
            Title = todo.Title,
            IsCompleted = todo.IsCompleted,
            CreatedAt = todo.CreatedAt
        };
    }

    public async Task<TodoResponse?> UpdateAsync(
        Guid id,
        UpdateTodoRequest request)
    {
        var todo = await _repository.GetByIdAsync(id);

        if (todo is null)
            return null;

        todo.Title = request.Title;
        todo.IsCompleted = request.IsCompleted;

        _repository.Update(todo);

        await _repository.SaveChangesAsync();
        
        await _cache.RemoveAsync("todos");

        return new TodoResponse
        {
            Id = todo.Id,
            Title = todo.Title,
            IsCompleted = todo.IsCompleted,
            CreatedAt = todo.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Deleting todo {TodoId}", id);
        
        var todo = await _repository.GetByIdAsync(id);

        if (todo is null)
            return false;

        _repository.Delete(todo);

        await _repository.SaveChangesAsync();
        
        await _cache.RemoveAsync("todos");

        return true;
    }
}