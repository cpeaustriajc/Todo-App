using FluentAssertions;
using NUnit.Framework;
using Todo_App.Application.Common.Exceptions;
using Todo_App.Application.TodoLists.Commands.CreateTodoList;
using Todo_App.Application.TodoLists.Commands.DeleteTodoList;
using Todo_App.Application.TodoItems.Commands.CreateTodoItem;
using Todo_App.Domain.Entities;

namespace Todo_App.Application.IntegrationTests.TodoLists.Commands;

using static Testing;

public class DeleteTodoListTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidTodoListId()
    {
        var command = new DeleteTodoListCommand(99);
        await FluentActions.Invoking(() => SendAsync(command)).Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldSoftDeleteTodoList()
    {
        var listId = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        await SendAsync(new DeleteTodoListCommand(listId));

        var list = await FindIncludingSoftDeletedAsync<TodoList>(listId);

        list.Should().NotBeNull();
        list!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task SoftDeletedListShouldNotAppearInQueries()
    {
        var listId = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        var listCountBefore = await CountAsync<TodoList>();

        await SendAsync(new DeleteTodoListCommand(listId));

        var listCountAfter = await CountAsync<TodoList>();

        listCountAfter.Should().Be(listCountBefore - 1);
    }

    [Test]
    public async Task ShouldSoftDeleteAllItemsWhenDeletingList()
    {
        var listId = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        var itemId1 = await SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "Item 1"
        });

        var itemId2 = await SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "Item 2"
        });

        await SendAsync(new DeleteTodoListCommand(listId));

        var item1 = await FindIncludingSoftDeletedAsync<TodoItem>(itemId1);
        var item2 = await FindIncludingSoftDeletedAsync<TodoItem>(itemId2);

        item1.Should().NotBeNull();
        item1!.IsDeleted.Should().BeTrue();
        item2.Should().NotBeNull();
        item2!.IsDeleted.Should().BeTrue();
    }
}
