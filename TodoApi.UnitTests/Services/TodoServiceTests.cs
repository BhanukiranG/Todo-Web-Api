using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Repositories;
using TodoApi.Services;

namespace TodoApi.UnitTests.Services;

public class TodoServiceTests
{
    private readonly Mock<ITodoRepository> _repository;
    private readonly Mock<IDistributedCache> _cache;
    private readonly TodoService _service;

    public TodoServiceTests()
    {
        _repository = new Mock<ITodoRepository>();
        _cache = new Mock<IDistributedCache>();

        _service = new TodoService(
            _repository.Object,
            Mock.Of<ILogger<TodoService>>(),
            _cache.Object); // Added the missing IDistributedCache dependency
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Todos_From_Database_When_Cache_Empty()
    {
        // Arrange
        _cache.Setup(x => x.GetAsync("todos", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var todos = new List<TodoItem>
        {
            new TodoItem { Id = Guid.NewGuid(), Title = "Task 1", IsCompleted = false },
            new TodoItem { Id = Guid.NewGuid(), Title = "Task 2", IsCompleted = true }
        };

        _repository.Setup(x => x.GetAllAsync()).ReturnsAsync(todos);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        _repository.Verify(x => x.GetAllAsync(), Times.Once);
        _cache.Verify(x => x.SetAsync("todos", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Should_Create_Todo()
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = "Learn Unit Testing"
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Learn Unit Testing", result.Title);

        _repository.Verify(
            x => x.AddAsync(It.IsAny<TodoItem>()),
            Times.Once);

        _repository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
            
        // Optional: Verify the cache was invalidated
        _cache.Verify(
            x => x.RemoveAsync("todos", It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Todo_When_Exists()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repository.Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync(new TodoItem
            {
                Id = id,
                Title = "Existing Todo",
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            });

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("Existing Todo", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_NotFound()
    {
        // Arrange
        _repository.Setup(x =>
                x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((TodoItem?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Todo_When_Exists()
    {
        // Arrange
        var id = Guid.NewGuid();

        var todo = new TodoItem
        {
            Id = id,
            Title = "Old Title",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _repository.Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync(todo);

        var request = new UpdateTodoRequest
        {
            Title = "New Title",
            IsCompleted = true
        };

        // Act
        var result = await _service.UpdateAsync(id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Title", result!.Title);
        Assert.True(result.IsCompleted);

        _repository.Verify(
            x => x.Update(It.IsAny<TodoItem>()),
            Times.Once);

        _repository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
            
        // Optional: Verify the cache was invalidated
        _cache.Verify(
            x => x.RemoveAsync("todos", It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Should_Delete_Todo_When_Exists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var todo = new TodoItem { Id = id, Title = "To Delete" };

        _repository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(todo);

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        Assert.True(result);
        _repository.Verify(x => x.Delete(todo), Times.Once);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _cache.Verify(x => x.RemoveAsync("todos", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Should_Return_False_When_NotFound()
    {
        // Arrange
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TodoItem?)null);

        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
        _repository.Verify(x => x.Delete(It.IsAny<TodoItem>()), Times.Never);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}