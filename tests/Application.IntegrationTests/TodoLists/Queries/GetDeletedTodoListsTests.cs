using FluentAssertions;
using NUnit.Framework;
using Todo_App.Application.TodoLists.Commands.CreateTodoList;
using Todo_App.Application.TodoLists.Commands.DeleteTodoList;
using Todo_App.Application.TodoLists.Queries.GetDeletedTodoLists;
using Todo_App.Application.TodoItems.Commands.CreateTodoItem;

namespace Todo_App.Application.IntegrationTests.TodoLists.Queries;

using static Testing;

public class GetDeletedTodoListsTests : BaseTestFixture
{
    [Test]
    public async Task ShouldReturnEmptyListWhenNoDeletedLists()
    {
        var query = new GetDeletedTodoListsQuery();

        var result = await Testing.SendAsync(query);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task ShouldReturnDeletedTodoLists()
    {
        var listId1 = await Testing.SendAsync(new CreateTodoListCommand
        {
            Title = "Active List"
        });

        var listId2 = await Testing.SendAsync(new CreateTodoListCommand
        {
            Title = "Deleted List 1"
        });

        var listId3 = await Testing.SendAsync(new CreateTodoListCommand
        {
            Title = "Deleted List 2"
        });

        await Testing.SendAsync(new DeleteTodoListCommand(listId2));
        await Testing.SendAsync(new DeleteTodoListCommand(listId3));

        var query = new GetDeletedTodoListsQuery();
        var result = await Testing.SendAsync(query);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().Contain(x => x.Title == "Deleted List 1");
        result.Items.Should().Contain(x => x.Title == "Deleted List 2");
        result.Items.Should().NotContain(x => x.Title == "Active List");
    }

    [Test]
    public async Task ShouldSupportPagination()
    {
        for (int i = 1; i <= 15; i++)
        {
            var listId = await SendAsync(new CreateTodoListCommand
            {
                Title = $"Deleted List {i}"
            });
            await SendAsync(new DeleteTodoListCommand(listId));
        }

        var query = new GetDeletedTodoListsQuery { PageNumber = 1, PageSize = 10 };
        var result = await SendAsync(query);

        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(15);
        result.PageNumber.Should().Be(1);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }
}
