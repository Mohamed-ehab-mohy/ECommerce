namespace ECommerce.Domain.Entities;

public class Product : BaseEntity
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 1000;

    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string PictureUrl { get; private set; } = null!;
    public decimal Price { get; private set; }
    public Guid ProductBrandId { get; private set; }
    public ProductBrand ProductBrand { get; set; } = null!;
    public Guid ProductTypeId { get; private set; }
    public ProductType ProductType { get; set; } = null!;

    private Product() { }

    private Product(string name, string description, string pictureUrl, decimal price, Guid productBrandId, Guid productTypeId)
    {
        SetName(name);
        SetDescription(description);
        PictureUrl = pictureUrl;
        SetPrice(price);
        ProductBrandId = productBrandId;
        ProductTypeId = productTypeId;
        Id = Guid.NewGuid();
    }

    public static Product Create(string name, string description, string pictureUrl, decimal price, Guid productBrandId, Guid productTypeId)
    {
        return new Product(name, description, pictureUrl, price, productBrandId, productTypeId);
    }

    public void UpdateDetails(string name, string description, decimal price)
    {
        SetName(name);
        SetDescription(description);
        SetPrice(price);
    }

    public void UpdatePicture(string pictureUrl)
    {
        PictureUrl = pictureUrl;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Product name cannot be empty.");

        if (name.Trim().Length > MaxNameLength)
            throw new InvalidOperationException($"Product name cannot exceed {MaxNameLength} characters.");

        Name = name.Trim();
    }

    private void SetDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("Product description cannot be empty.");

        if (description.Trim().Length > MaxDescriptionLength)
            throw new InvalidOperationException($"Product description cannot exceed {MaxDescriptionLength} characters.");

        Description = description.Trim();
    }

    private void SetPrice(decimal price)
    {
        if (price < 0)
            throw new InvalidOperationException("Product price cannot be negative.");

        Price = price;
    }
}