namespace ECommerce.Domain.Entities;

public class ProductBrand : BaseEntity
{
    public string Name { get; private set; }
    public ICollection<Product> Products { get; set; } = [];

    private ProductBrand() { Name = null!; }

    private ProductBrand(string name)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
    }

    public static ProductBrand Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ProductBrand(name.Trim());
    }

    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        Name = newName.Trim();
    }
}