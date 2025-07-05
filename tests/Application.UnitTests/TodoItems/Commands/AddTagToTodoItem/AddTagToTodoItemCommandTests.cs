using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Todo_App.Application.TodoItems.Commands.AddTagToTodoItem;
using Todo_App.Domain.Entities;

namespace Todo_App.Application.UnitTests.TodoItems.Commands.AddTagToTodoItem;

public class AddTagToTodoItemCommandTests
{
    private DbContext? _context;
    private AddTagToTodoItemCommandHandler? _handler;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _handler = new AddTagToTodoItemCommandHandler((TestDbContext)_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public async Task ShouldAddTagToTodoItem()
    {
        var list = new TodoList { Title = "Shopping" };
        _context!.Set<TodoList>().Add(list);
        await _context.SaveChangesAsync();

        var item = new TodoItem { Title = "Buy milk", ListId = list.Id };
        _context.Set<TodoItem>().Add(item);
        await _context.SaveChangesAsync();

        var tag = new Tag { Name = "Important", Color = "#ff0000" };
        _context.Set<Tag>().Add(tag);
        await _context.SaveChangesAsync();

        var command = new AddTagToTodoItemCommand { TodoItemId = item.Id, TagId = tag.Id };
        await _handler!.Handle(command, CancellationToken.None);

        var todoItemTag = await _context.Set<TodoItemTag>()
            .FirstOrDefaultAsync(tt => tt.TodoItemId == item.Id && tt.TagId == tag.Id);

        todoItemTag.Should().NotBeNull();
        todoItemTag!.TodoItemId.Should().Be(item.Id);
        todoItemTag.TagId.Should().Be(tag.Id);
    }

    [Test]
    public async Task ShouldNotAddDuplicateTagToTodoItem()
    {
        var list = new TodoList { Title = "Shopping" };
        _context!.Set<TodoList>().Add(list);
        await _context.SaveChangesAsync();

        var item = new TodoItem { Title = "Buy milk", ListId = list.Id };
        _context.Set<TodoItem>().Add(item);
        await _context.SaveChangesAsync();

        var tag = new Tag { Name = "Important", Color = "#ff0000" };
        _context.Set<Tag>().Add(tag);
        await _context.SaveChangesAsync();

        // Add tag first time
        var todoItemTag = new TodoItemTag { TodoItemId = item.Id, TagId = tag.Id };
        _context.Set<TodoItemTag>().Add(todoItemTag);
        await _context.SaveChangesAsync();

        var command = new AddTagToTodoItemCommand { TodoItemId = item.Id, TagId = tag.Id };
        await _handler!.Handle(command, CancellationToken.None);

        var todoItemTagsCount = await _context.Set<TodoItemTag>()
            .CountAsync(tt => tt.TodoItemId == item.Id && tt.TagId == tag.Id);

        todoItemTagsCount.Should().Be(1); // Should not add duplicate
    }
}

// Simple test DbContext that implements IApplicationDbContext
public class TestDbContext : DbContext, Todo_App.Application.Common.Interfaces.IApplicationDbContext
{
    public TestDbContext(DbContextOptions options) : base(options) { }

    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TodoItemTag> TodoItemTags => Set<TodoItemTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure TodoList
        modelBuilder.Entity<TodoList>(entity =>
        {
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.OwnsOne(b => b.Colour);
        });

        // Configure TodoItem
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.HasOne(t => t.List).WithMany(l => l.Items).HasForeignKey(t => t.ListId);
        });

        // Configure Tag
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(100).IsRequired();
            entity.Property(t => t.Color).HasMaxLength(7);
            entity.HasIndex(t => t.Name).IsUnique();
        });

        // Configure TodoItemTag
        modelBuilder.Entity<TodoItemTag>(entity =>
        {
            entity.HasKey(t => new { t.TodoItemId, t.TagId });
            entity.HasOne(t => t.TodoItem).WithMany(i => i.TodoItemTags).HasForeignKey(t => t.TodoItemId);
            entity.HasOne(t => t.Tag).WithMany().HasForeignKey(t => t.TagId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
