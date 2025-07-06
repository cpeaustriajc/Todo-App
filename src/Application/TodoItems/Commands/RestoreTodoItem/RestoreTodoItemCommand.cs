using MediatR;
using Todo_App.Application.Common.Exceptions;
using Todo_App.Application.Common.Interfaces;
using Todo_App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Todo_App.Application.TodoItems.Commands.RestoreTodoItem;

public record RestoreTodoItemCommand(int Id) : IRequest;

public class RestoreTodoItemCommandHandler : IRequestHandler<RestoreTodoItemCommand>
{
    private readonly IApplicationDbContext _context;

    public RestoreTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(RestoreTodoItemCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(TodoItem), request.Id);
        }

        entity.IsDeleted = false;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
