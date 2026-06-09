using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Repositories;

namespace TodoApi.Services;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _repository;
    
    private readonly ILogger<TodoService> _logger;

    public TodoService(
        ITodoRepository repository,
        ILogger<TodoService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<TodoResponse>> GetAllAsync()
    {
        var todos = await _repository.GetAllAsync();

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

        return true;
    }
}