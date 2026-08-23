namespace ECommerce.UseCases.Common;

public interface ICorrelationIdProvider
{
    string CorrelationId { get; }
}
