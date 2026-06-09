namespace TodoApi.DTOs;

public class UpdateTodoRequest
{
    public string Title { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }
}