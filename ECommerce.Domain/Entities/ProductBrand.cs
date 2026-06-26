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

    public static ProductBrand Create(string name, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var brand = new ProductBrand(name.Trim());

        if (id.HasValue)
            brand.Id = id.Value;

        return brand;
    }

    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        Name = newName.Trim();
    }
}