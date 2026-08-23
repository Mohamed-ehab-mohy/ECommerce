namespace ECommerce.UseCases.Messaging.Ports;

public interface IInboxMessageRepository
{
    /// <summary>
    /// Atomically claims a message for the given queue. Returns true when this
    /// call is the first to see the message id; false when it was already processed.
    /// </summary>
    Task<bool> TryConsumeAsync(string consumerQueue, Guid messageId, CancellationToken cancellationToken);
}
