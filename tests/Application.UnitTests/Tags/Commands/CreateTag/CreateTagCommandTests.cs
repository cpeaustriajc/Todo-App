using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Todo_App.Application.Tags.Commands.CreateTag;
using Todo_App.Domain.Entities;

namespace Todo_App.Application.UnitTests.Tags.Commands.CreateTag;

public class CreateTagCommandTests
{
    private DbContext? _context;
    private CreateTagCommandHandler? _handler;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _handler = new CreateTagCommandHandler((TestDbContext)_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    [Test]
    public async Task ShouldCreateTag()
    {
        var command = new CreateTagCommand { Name = "Important", Color = "#ff0000" };
        var tagId = await _handler!.Handle(command, CancellationToken.None);
        tagId.Should().BeGreaterThan(0);

        var tag = await _context!.Set<Tag>().FindAsync(tagId);
        tag.Should().NotBeNull();
        tag!.Name.Should().Be("Important");
        tag.Color.Should().Be("#ff0000");
        tag.UsageCount.Should().Be(0);
    }

    [Test]
    public async Task ShouldReturnExistingTagIdIfTagAlreadyExists()
    {
        var existingTag = new Tag { Name = "Important", Color = "#ff0000" };
        _context!.Set<Tag>().Add(existingTag);
        await _context.SaveChangesAsync();

        var command = new CreateTagCommand { Name = "Important", Color = "#00ff00" };
        var tagId = await _handler!.Handle(command, CancellationToken.None);

        tagId.Should().Be(existingTag.Id);
        var tagsCount = await _context.Set<Tag>().CountAsync();
        tagsCount.Should().Be(1);

        var tag = await _context.Set<Tag>().FindAsync(existingTag.Id);
        tag!.Color.Should().Be("#ff0000"); // Should keep original color
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
