using TodoApi.DTOs;

namespace TodoApi.Services;

public interface ITodoService
{
    Task<List<TodoResponse>> GetAllAsync();

    Task<TodoResponse> CreateAsync(CreateTodoRequest request);
    
    Task<TodoResponse?> GetByIdAsync(Guid id);

    Task<TodoResponse?> UpdateAsync(Guid id, UpdateTodoRequest request);

    Task<bool> DeleteAsync(Guid id);
}