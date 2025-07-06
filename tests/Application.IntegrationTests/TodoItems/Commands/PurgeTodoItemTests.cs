using FluentAssertions;
using NUnit.Framework;
using Todo_App.Application.Common.Exceptions;
using Todo_App.Application.TodoItems.Commands.CreateTodoItem;
using Todo_App.Application.TodoItems.Commands.DeleteTodoItem;
using Todo_App.Application.TodoItems.Commands.PurgeTodoItem;
using Todo_App.Application.TodoLists.Commands.CreateTodoList;
using Todo_App.Domain.Entities;

namespace Todo_App.Application.IntegrationTests.TodoItems.Commands;

using static Testing;

public class PurgeTodoItemTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidTodoItemId()
    {
        var command = new PurgeTodoItemCommand(99);
        await FluentActions.Invoking(() => Testing.SendAsync(command)).Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldPermanentlyDeleteSoftDeletedTodoItem()
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

        await Testing.SendAsync(new DeleteTodoItemCommand(itemId));

        var item = await Testing.FindIncludingSoftDeletedAsync<TodoItem>(itemId);
        item.Should().NotBeNull();
        item!.IsDeleted.Should().BeTrue();

        await Testing.SendAsync(new PurgeTodoItemCommand(itemId));

        var purgedItem = await Testing.FindIncludingSoftDeletedAsync<TodoItem>(itemId);
        purgedItem.Should().BeNull();

        var normalItem = await Testing.FindAsync<TodoItem>(itemId);
        normalItem.Should().BeNull();
    }

    [Test]
    public async Task ShouldNotPurgeNonDeletedTodoItem()
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

        var command = new PurgeTodoItemCommand(itemId);
        await FluentActions.Invoking(() => Testing.SendAsync(command)).Should().ThrowAsync<NotFoundException>();
    }
}
