namespace Todo_App.Domain.Entities;

public class Tag : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = "#6c757d";

    public int UsageCount { get; set; } = 0;

    public IList<TodoItemTag> TodoItemTags { get; private set; } = new List<TodoItemTag>();
}
