namespace ECommerce.UseCases.Common;

public interface ITenantService
{
    Guid? GetCurrentTenantId();
}
