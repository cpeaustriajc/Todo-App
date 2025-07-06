using FluentAssertions;
using NUnit.Framework;
using Todo_App.Application.TodoItems.Commands.CreateTodoItem;
using Todo_App.Application.TodoItems.Commands.DeleteTodoItem;
using Todo_App.Application.TodoItems.Queries.GetDeletedTodoItems;
using Todo_App.Application.TodoLists.Commands.CreateTodoList;

namespace Todo_App.Application.IntegrationTests.TodoItems.Queries;

using static Testing;

public class GetDeletedTodoItemsTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnEmptyListWhenNoDeletedItems()
    {
        var query = new GetDeletedTodoItemsQuery();

        var result = await Testing.SendAsync(query);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task ShouldReturnDeletedTodoItems()
    {
        var listId = await Testing.SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        var itemId1 = await Testing.SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "Active Item"
        });

        var itemId2 = await Testing.SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "Deleted Item 1"
        });

        var itemId3 = await Testing.SendAsync(new CreateTodoItemCommand
        {
            ListId = listId,
            Title = "Deleted Item 2"
        });

        await Testing.SendAsync(new DeleteTodoItemCommand(itemId2));
        await Testing.SendAsync(new DeleteTodoItemCommand(itemId3));

        var query = new GetDeletedTodoItemsQuery();
        var result = await Testing.SendAsync(query);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().Contain(x => x.Title == "Deleted Item 1");
        result.Items.Should().Contain(x => x.Title == "Deleted Item 2");
        result.Items.Should().NotContain(x => x.Title == "Active Item");
    }

    [Test]
    public async Task ShouldSupportPagination()
    {
        var listId = await Testing.SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        for (int i = 1; i <= 15; i++)
        {
            var itemId = await Testing.SendAsync(new CreateTodoItemCommand
            {
                ListId = listId,
                Title = $"Deleted Item {i}"
            });
            await Testing.SendAsync(new DeleteTodoItemCommand(itemId));
        }

        var query = new GetDeletedTodoItemsQuery { PageNumber = 1, PageSize = 10 };
        var result = await Testing.SendAsync(query);

        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(15);
        result.PageNumber.Should().Be(1);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }
}
