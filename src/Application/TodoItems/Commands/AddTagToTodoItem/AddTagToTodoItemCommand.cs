using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo_App.Application.Common.Exceptions;
using Todo_App.Application.Common.Interfaces;
using Todo_App.Domain.Entities;

namespace Todo_App.Application.TodoItems.Commands.AddTagToTodoItem;

public record AddTagToTodoItemCommand : IRequest
{
    public int TodoItemId { get; init; }

    public int TagId { get; init; }
}

public class AddTagToTodoItemCommandHandler : IRequestHandler<AddTagToTodoItemCommand>
{
    private readonly IApplicationDbContext _context;

    public AddTagToTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(AddTagToTodoItemCommand request, CancellationToken cancellationToken)
    {
        var todoItem = await _context.TodoItems
            .FirstOrDefaultAsync(t => t.Id == request.TodoItemId, cancellationToken);

        if (todoItem == null)
        {
            throw new NotFoundException(nameof(TodoItem), request.TodoItemId);
        }

        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Id == request.TagId, cancellationToken);

        if (tag == null)
        {
            throw new NotFoundException(nameof(Tag), request.TagId);
        }

        var existingRelation = await _context.TodoItemTags
            .FirstOrDefaultAsync(tt => tt.TodoItemId == request.TodoItemId && tt.TagId == request.TagId, cancellationToken);

        if (existingRelation == null)
        {
            var todoItemTag = new TodoItemTag
            {
                TodoItemId = request.TodoItemId,
                TagId = request.TagId
            };

            _context.TodoItemTags.Add(todoItemTag);

            tag.UsageCount++;

            await _context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
