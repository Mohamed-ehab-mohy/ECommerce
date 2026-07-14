namespace ECommerce.Domain.Entities;

public class ProductBrand : BaseEntity
{
    public const int MaxNameLength = 100;

    public string Name { get; private set; } = null!;
    public ICollection<Product> Products { get; set; } = [];

    private ProductBrand() { }

    private ProductBrand(string name) : base()
    {
        Name = name.Trim();
    }

    public static Result<ProductBrand> Create(string name, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BrandErrors.InvalidName;

        if (name.Trim().Length > MaxNameLength)
            return BrandErrors.NameTooLong;

        var brand = new ProductBrand(name.Trim());

        if (id.HasValue)
            brand.Id = id.Value;

        return brand;
    }

    public Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure(BrandErrors.InvalidName);

        if (newName.Trim().Length > MaxNameLength)
            return Result.Failure(BrandErrors.NameTooLong);

        Name = newName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}
