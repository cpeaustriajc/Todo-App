using MediatR;
using Todo_App.Application.Common.Exceptions;
using Todo_App.Application.Common.Interfaces;
using Todo_App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Todo_App.Application.TodoLists.Commands.PurgeTodoList;

public record PurgeTodoListCommand(int Id) : IRequest;

public class PurgeTodoListCommandHandler : IRequestHandler<PurgeTodoListCommand>
{
    private readonly IApplicationDbContext _context;

    public PurgeTodoListCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(PurgeTodoListCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoLists
            .IgnoreQueryFilters()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(TodoList), request.Id);
        }

        _context.TodoItems.RemoveRange(entity.Items);
        _context.TodoLists.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
