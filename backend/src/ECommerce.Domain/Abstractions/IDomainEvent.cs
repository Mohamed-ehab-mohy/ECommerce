namespace ECommerce.Domain.Abstractions;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
