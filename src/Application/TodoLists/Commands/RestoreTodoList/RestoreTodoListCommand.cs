using MediatR;
using Todo_App.Application.Common.Exceptions;
using Todo_App.Application.Common.Interfaces;
using Todo_App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Todo_App.Application.TodoLists.Commands.RestoreTodoList;

public record RestoreTodoListCommand(int Id) : IRequest;

public class RestoreTodoListCommandHandler : IRequestHandler<RestoreTodoListCommand>
{
    private readonly IApplicationDbContext _context;

    public RestoreTodoListCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(RestoreTodoListCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoLists
            .IgnoreQueryFilters()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(TodoList), request.Id);
        }

        entity.IsDeleted = false;
        
        foreach (var item in entity.Items.Where(i => i.IsDeleted))
        {
            item.IsDeleted = false;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
