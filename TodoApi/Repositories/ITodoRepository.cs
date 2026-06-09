using TodoApi.Models;

namespace TodoApi.Repositories;

public interface ITodoRepository
{
    Task<List<TodoItem>> GetAllAsync();

    Task<TodoItem?> GetByIdAsync(Guid id);

    Task AddAsync(TodoItem todo);

    Task SaveChangesAsync();
    
    void Delete(TodoItem todo);

    void Update(TodoItem todo);
}