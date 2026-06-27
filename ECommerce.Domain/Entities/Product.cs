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
        Name = name;
        Description = description;
        PictureUrl = pictureUrl;
        Price = price;
        ProductBrandId = productBrandId;
        ProductTypeId = productTypeId;
        Id = Guid.NewGuid();
    }

    public static Result<Product> Create(string name, string description, string pictureUrl, decimal price, Guid productBrandId, Guid productTypeId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Product>.Failure(ProductErrors.InvalidName);

        if (name.Trim().Length > MaxNameLength)
            return Result<Product>.Failure(ProductErrors.InvalidName);

        if (string.IsNullOrWhiteSpace(description))
            return Result<Product>.Failure(ProductErrors.InvalidDescription);

        if (description.Trim().Length > MaxDescriptionLength)
            return Result<Product>.Failure(ProductErrors.InvalidDescription);

        if (price < 0)
            return Result<Product>.Failure(ProductErrors.InvalidPrice);

        return Result<Product>.Success(new Product(
            name.Trim(),
            description.Trim(),
            pictureUrl,
            price,
            productBrandId,
            productTypeId));
    }

    public Result UpdateDetails(string name, string description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(ProductErrors.InvalidName);

        if (name.Trim().Length > MaxNameLength)
            return Result.Failure(ProductErrors.InvalidName);

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure(ProductErrors.InvalidDescription);

        if (description.Trim().Length > MaxDescriptionLength)
            return Result.Failure(ProductErrors.InvalidDescription);

        if (price < 0)
            return Result.Failure(ProductErrors.InvalidPrice);

        Name = name.Trim();
        Description = description.Trim();
        Price = price;

        return Result.Success();
    }

    public void UpdatePicture(string pictureUrl)
    {
        PictureUrl = pictureUrl;
    }
}