namespace ECommerce.Domain.Entities;

public class ProductBrand : BaseEntity
{
    public string Name { get; private set; }
    public ICollection<Product> Products { get; set; } = [];

    private ProductBrand() { Name = null!; }

    public ProductBrand(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Brand name cannot be empty", nameof(name));

        Name = name;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Brand name cannot be empty", nameof(newName));

        Name = newName;
    }
}