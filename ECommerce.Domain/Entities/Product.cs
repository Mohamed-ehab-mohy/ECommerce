namespace ECommerce.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string PictureUrl { get; private set; }
    public decimal Price { get; private set; }
    public Guid ProductBrandId { get; private set; }
    public ProductBrand ProductBrand { get; set; } = null!;
    public Guid ProductTypeId { get; private set; }
    public ProductType ProductType { get; set; } = null!;

    private Product() { Name = null!; Description = null!; PictureUrl = null!; }

    public Product(string name, string description, string pictureUrl, decimal price, Guid productBrandId, Guid productTypeId)
    {
        Name = name;
        Description = description;
        PictureUrl = pictureUrl;
        Price = price;
        ProductBrandId = productBrandId;
        ProductTypeId = productTypeId;
    }

    public void UpdateDetails(string name, string description, decimal price)
    {
        Name = name;
        Description = description;
        Price = price;
    }

    public void UpdatePicture(string pictureUrl)
    {
        PictureUrl = pictureUrl;
    }
}