using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TodoApi.Common;
using TodoApi.DTOs;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TodosController : ControllerBase
{
    private readonly ITodoService _service;

    public TodosController(ITodoService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "TodoReadPolicy")]
    [SwaggerOperation(Summary = "Get all todos", Description = "Returns all todo items")]
    public async Task<ActionResult<List<TodoResponse>>> GetAll()
    {
        var result = await _service.GetAllAsync();

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "TodoCreatePolicy")]
    public async Task<ActionResult<TodoResponse>> Create(CreateTodoRequest request)
    {
        var result = await _service.CreateAsync(request);

        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TodoResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
        {
            throw new NotFoundException($"Todo '{id}' was not found.");
        }

        return Ok(result);
    }
    
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TodoResponse>> Update(
        Guid id,
        UpdateTodoRequest request)
    {
        var result = await _service.UpdateAsync(id, request);

        if (result == null)
        {
            throw new NotFoundException($"Todo '{id}' was not found.");
        }

        return Ok(result);
    }
    
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "TodoDeletePolicy")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
        {
            throw new NotFoundException($"Todo '{id}' was not found.");
        }

        return NoContent();
    }
}