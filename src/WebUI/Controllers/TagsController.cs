using Microsoft.AspNetCore.Mvc;
using Todo_App.Application.Common.Models;
using Todo_App.Application.Tags.Commands.CreateTag;
using Todo_App.Application.Tags.Queries.GetTags;
using Todo_App.Application.TodoItems.Commands.AddTagToTodoItem;
using Todo_App.Application.TodoItems.Commands.RemoveTagFromTodoItem;

namespace Todo_App.WebUI.Controllers;

public class TagsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IList<TagDto>>> Get()
    {
        return Ok(await Mediator.Send(new GetTagsQuery()));
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateTagCommand command)
    {
        return await Mediator.Send(command);
    }

    [HttpPost("todoitem/{todoItemId}/add/{tagId}")]
    public async Task<ActionResult> AddTagToTodoItem(int todoItemId, int tagId)
    {
        await Mediator.Send(new AddTagToTodoItemCommand { TodoItemId = todoItemId, TagId = tagId });
        return NoContent();
    }

    [HttpDelete("todoitem/{todoItemId}/remove/{tagId}")]
    public async Task<ActionResult> RemoveTagFromTodoItem(int todoItemId, int tagId)
    {
        await Mediator.Send(new RemoveTagFromTodoItemCommand { TodoItemId = todoItemId, TagId = tagId });
        return NoContent();
    }
}
