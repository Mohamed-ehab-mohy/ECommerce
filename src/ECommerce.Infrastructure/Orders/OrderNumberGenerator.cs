using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Orders.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Orders;

public sealed class OrderNumberGenerator(ECommerceDbContext dbContext) : IOrderNumberGenerator
{
    public async Task<string> GenerateAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var sequence = await dbContext.Database
            .SqlQuery<long>($"SELECT nextval('order_number_seq') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return OrderNumber.Create(utcNow, sequence).Value;
    }
}
