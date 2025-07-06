using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Todo_App.Application.Common.Interfaces;
using Todo_App.Application.Common.Mappings;
using Todo_App.Application.Common.Models;
using Todo_App.Application.TodoItems.Queries.GetTodoItemsWithPagination;
using Microsoft.EntityFrameworkCore;

namespace Todo_App.Application.TodoItems.Queries.GetDeletedTodoItems;

public record GetDeletedTodoItemsQuery : IRequest<PaginatedList<TodoItemBriefDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetDeletedTodoItemsQueryHandler : IRequestHandler<GetDeletedTodoItemsQuery, PaginatedList<TodoItemBriefDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetDeletedTodoItemsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<TodoItemBriefDto>> Handle(GetDeletedTodoItemsQuery request, CancellationToken cancellationToken)
    {
        return await _context.TodoItems
            .IgnoreQueryFilters()
            .Where(x => x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .ProjectTo<TodoItemBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
