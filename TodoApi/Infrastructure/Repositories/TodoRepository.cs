using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly ApplicationDbContext _db;

    public TodoRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<TodoItem>> GetAllAsync()
    {
        return await _db.Todos.ToListAsync();
    }

    public async Task<TodoItem?> GetByIdAsync(Guid id)
    {
        return await _db.Todos.FindAsync(id);
    }

    public async Task AddAsync(TodoItem todo)
    {
        await _db.Todos.AddAsync(todo);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
    
    public void Delete(TodoItem todo)
    {
        _db.Todos.Remove(todo);
    }

    public void Update(TodoItem todo)
    {
        _db.Todos.Update(todo);
    }
}