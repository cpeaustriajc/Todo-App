using MediatR;
using Todo_App.Application.Common.Exceptions;
using Todo_App.Application.Common.Interfaces;
using Todo_App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Todo_App.Application.TodoItems.Commands.PurgeTodoItem;

public record PurgeTodoItemCommand(int Id) : IRequest;

public class PurgeTodoItemCommandHandler : IRequestHandler<PurgeTodoItemCommand>
{
    private readonly IApplicationDbContext _context;

    public PurgeTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(PurgeTodoItemCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(TodoItem), request.Id);
        }

        _context.TodoItems.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
