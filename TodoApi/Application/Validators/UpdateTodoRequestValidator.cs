using FluentValidation;
using TodoApi.DTOs;

namespace TodoApi.Validators;

public class UpdateTodoRequestValidator
    : AbstractValidator<UpdateTodoRequest>
{
    public UpdateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(100);
    }
}