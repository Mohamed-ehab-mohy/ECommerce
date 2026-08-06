using ECommerce.Domain.Common;

namespace ECommerce.Domain.Inventory;

public sealed class Warehouse : BaseEntity<Guid>
{
    private Warehouse()
    {
        Code = string.Empty;
        Name = string.Empty;
        Address = string.Empty;
        Timezone = string.Empty;
    }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public string Address { get; private set; }

    public string Timezone { get; private set; }

    public WarehouseStatus Status { get; private set; }

    public static Warehouse Create(
        string code,
        string name,
        string address,
        string timezone,
        WarehouseStatus status,
        DateTime utcNow)
    {
        return new Warehouse
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Address = address,
            Timezone = timezone,
            Status = status,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void UpdateDetails(
        string? name,
        string? address,
        string? timezone,
        WarehouseStatus? status,
        DateTime utcNow)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name;
        }

        if (!string.IsNullOrWhiteSpace(address))
        {
            Address = address;
        }

        if (!string.IsNullOrWhiteSpace(timezone))
        {
            Timezone = timezone;
        }

        if (status is not null)
        {
            Status = status.Value;
        }

        UpdatedAt = utcNow;
    }

    public void Deactivate()
    {
        Status = WarehouseStatus.Inactive;
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
