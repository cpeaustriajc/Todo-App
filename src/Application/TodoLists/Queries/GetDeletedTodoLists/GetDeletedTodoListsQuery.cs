using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Todo_App.Application.Common.Interfaces;
using Todo_App.Application.Common.Mappings;
using Todo_App.Application.Common.Models;
using Todo_App.Application.TodoLists.Queries.GetTodos;
using Microsoft.EntityFrameworkCore;

namespace Todo_App.Application.TodoLists.Queries.GetDeletedTodoLists;

public record GetDeletedTodoListsQuery : IRequest<PaginatedList<TodoListDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetDeletedTodoListsQueryHandler : IRequestHandler<GetDeletedTodoListsQuery, PaginatedList<TodoListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetDeletedTodoListsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<TodoListDto>> Handle(GetDeletedTodoListsQuery request, CancellationToken cancellationToken)
    {
        return await _context.TodoLists
            .IgnoreQueryFilters()
            .Where(x => x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .ProjectTo<TodoListDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
