using FluentAssertions;
using NUnit.Framework;
using Todo_App.Application.Common.Exceptions;
using Todo_App.Application.TodoLists.Commands.CreateTodoList;
using Todo_App.Application.TodoLists.Commands.DeleteTodoList;
using Todo_App.Application.TodoLists.Commands.RestoreTodoList;
using Todo_App.Application.TodoLists.Commands.PurgeTodoList;
using Todo_App.Application.TodoItems.Commands.CreateTodoItem;
using Todo_App.Domain.Entities;

namespace Todo_App.Application.IntegrationTests.TodoLists.Commands;

using static Testing;

public class RestoreTodoListTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidTodoListId()
    {
        var command = new RestoreTodoListCommand(99);
        await FluentActions.Invoking(() => Testing.SendAsync(command)).Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldRestoreSoftDeletedTodoList()
    {
        var listId = await Testing.SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        await Testing.SendAsync(new DeleteTodoListCommand(listId));

        var list = await Testing.FindIncludingSoftDeletedAsync<TodoList>(listId);
        list.Should().NotBeNull();
        list!.IsDeleted.Should().BeTrue();

        await Testing.SendAsync(new RestoreTodoListCommand(listId));

        var restoredList = await Testing.FindAsync<TodoList>(listId);
        restoredList.Should().NotBeNull();
        restoredList!.IsDeleted.Should().BeFalse();
    }

    [Test]
    public async Task ShouldRestoreTodoListWithItems()
    {
        var listId = await Testing.SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        var itemId = await Testing.SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "New Item"
        });

        await Testing.SendAsync(new DeleteTodoListCommand(listId));

        var list = await Testing.FindIncludingSoftDeletedAsync<TodoList>(listId);
        var item = await Testing.FindIncludingSoftDeletedAsync<TodoItem>(itemId);
        list.Should().NotBeNull();
        item.Should().NotBeNull();
        list!.IsDeleted.Should().BeTrue();
        item!.IsDeleted.Should().BeTrue();

        await Testing.SendAsync(new RestoreTodoListCommand(listId));

        var restoredList = await Testing.FindAsync<TodoList>(listId);
        var restoredItem = await Testing.FindAsync<TodoItem>(itemId);
        restoredList.Should().NotBeNull();
        restoredItem.Should().NotBeNull();
        restoredList!.IsDeleted.Should().BeFalse();
        restoredItem!.IsDeleted.Should().BeFalse();
    }

    [Test]
    public async Task ShouldNotRestoreNonDeletedTodoList()
    {
        var listId = await Testing.SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        var command = new RestoreTodoListCommand(listId);
        await FluentActions.Invoking(() => Testing.SendAsync(command)).Should().ThrowAsync<NotFoundException>();
    }
}
