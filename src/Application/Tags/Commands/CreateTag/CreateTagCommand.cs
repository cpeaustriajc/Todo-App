using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo_App.Application.Common.Interfaces;
using Todo_App.Domain.Entities;

namespace Todo_App.Application.Tags.Commands.CreateTag;

public record CreateTagCommand : IRequest<int>
{
    public string Name { get; init; } = string.Empty;

    public string Color { get; init; } = "#6c757d";
}

public class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateTagCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        var existingTag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Name == request.Name, cancellationToken);

        if (existingTag != null)
        {
            return existingTag.Id;
        }

        var entity = new Tag
        {
            Name = request.Name,
            Color = request.Color
        };

        _context.Tags.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
