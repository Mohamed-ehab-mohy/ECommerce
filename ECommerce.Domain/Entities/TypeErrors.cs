namespace ECommerce.Domain.Entities;

public static class TypeErrors
{
    public static readonly Error TypeNotFound = new("Type.NotFound", "Type not found.", ErrorType.NotFound);
    public static readonly Error InvalidName = new("Type.InvalidName", "Type name cannot be empty.", ErrorType.Validation);
    public static readonly Error NameTooLong = new("Type.NameTooLong", $"Type name cannot exceed {ProductType.MaxNameLength} characters.", ErrorType.Validation);
    public static readonly Error DuplicateName = new("Type.DuplicateName", "A type with this name already exists.", ErrorType.Conflict);
}
