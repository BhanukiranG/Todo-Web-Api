using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Data;
using Microsoft.EntityFrameworkCore;
using TodoApi.DTOs;

namespace TodoApi.Controllers;

[ApiController]
[Authorize]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/todos")]
public class TodosV2Controller : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public TodosV2Controller(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var todos = await _db.Todos
            .Select(x => new TodoResponseV2
            {
                Id = x.Id,
                Title = x.Title,
                IsCompleted = x.IsCompleted,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(todos);
    }
}