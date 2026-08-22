namespace ECommerce.Domain.Common;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    void Delete();
}
