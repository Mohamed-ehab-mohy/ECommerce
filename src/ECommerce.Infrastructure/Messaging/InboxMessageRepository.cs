using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Messaging.Ports;
using Npgsql;

namespace ECommerce.Infrastructure.Messaging;

public sealed class InboxMessageRepository(ECommerceDbContext dbContext) : IInboxMessageRepository
{
    public async Task<bool> TryConsumeAsync(
        string consumerQueue,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var inserted = await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO inbox_messages (consumer_queue, message_id, processed_at)
            VALUES (@consumer_queue, @message_id, @processed_at)
            ON CONFLICT (consumer_queue, message_id) DO NOTHING
            """,
            new object[]
            {
                new NpgsqlParameter("@consumer_queue", consumerQueue),
                new NpgsqlParameter("@message_id", messageId),
                new NpgsqlParameter("@processed_at", DateTime.UtcNow),
            },
            cancellationToken);

        return inserted > 0;
    }
}
