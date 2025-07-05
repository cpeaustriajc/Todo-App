using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo_App.Application.Common.Interfaces;

namespace Todo_App.Application.TodoItems.Commands.RemoveTagFromTodoItem;

public record RemoveTagFromTodoItemCommand : IRequest
{
    public int TodoItemId { get; init; }

    public int TagId { get; init; }
}

public class RemoveTagFromTodoItemCommandHandler : IRequestHandler<RemoveTagFromTodoItemCommand>
{
    private readonly IApplicationDbContext _context;

    public RemoveTagFromTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(RemoveTagFromTodoItemCommand request, CancellationToken cancellationToken)
    {
        var todoItemTag = await _context.TodoItemTags
            .FirstOrDefaultAsync(tt => tt.TodoItemId == request.TodoItemId && tt.TagId == request.TagId, cancellationToken);

        if (todoItemTag != null)
        {
            _context.TodoItemTags.Remove(todoItemTag);

            var tag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Id == request.TagId, cancellationToken);

            if (tag != null && tag.UsageCount > 0)
            {
                tag.UsageCount--;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
