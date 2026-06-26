namespace ECommerce.Domain.Entities;

public class ProductType : BaseEntity
{
    public string Name { get; private set; }
    public ICollection<Product> Products { get; set; } = [];

    private ProductType() { Name = null!; }

    private ProductType(string name)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
    }

    public static ProductType Create(string name, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var type = new ProductType(name.Trim());

        if (id.HasValue)
            type.Id = id.Value;

        return type;
    }

    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        Name = newName.Trim();
    }
}