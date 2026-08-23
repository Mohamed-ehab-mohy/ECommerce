namespace ECommerce.UseCases.Identity;

public static class IdentityRoles
{
    public const string Customer = "Customer";
    public const string Staff = "Staff";
    public const string Finance = "Finance";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";

    public static IReadOnlyList<string> All { get; } = [Customer, Staff, Finance, Admin, SuperAdmin];
}
