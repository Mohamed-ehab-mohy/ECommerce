namespace ECommerce.Domain.Entities;

public class ProductType : BaseEntity
{
    public string Name { get; private set; }
    public ICollection<Product> Products { get; set; } = [];

    private ProductType() { Name = null!; }

    public ProductType(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Type name cannot be empty", nameof(name));

        Name = name;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Type name cannot be empty", nameof(newName));

        Name = newName;
    }
}