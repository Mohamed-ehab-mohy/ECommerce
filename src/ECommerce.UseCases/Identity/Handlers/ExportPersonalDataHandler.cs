using ECommerce.Domain.Identity;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class ExportPersonalDataHandler(
    IUserRepository users,
    IOrderRepository orders,
    IAddressRepository addresses) : IRequestHandler<ExportPersonalDataCommand, Result<PersonalDataExport>>
{
    public async Task<Result<PersonalDataExport>> Handle(ExportPersonalDataCommand request, CancellationToken cancellationToken)
    {
        var customer = await users.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.CustomerNotFound;
        }

        var roles = await users.GetRolesAsync(customer.Id, cancellationToken);

        var orderHistory = await orders.ListByCustomerAsync(customer.Id, cursor: null, pageSize: int.MaxValue, cancellationToken);

        var orderExports = orderHistory.Items
            .Select(o => new OrderExportData(
                o.Id,
                o.OrderNumber,
                o.CreatedAt,
                o.Status.ToString(),
                o.GrandTotal,
                o.Currency))
            .ToList();

        var customerAddresses = await addresses.GetByCustomerIdAsync(customer.Id, cancellationToken);

        var addressExports = customerAddresses
            .Select(a => new AddressExportData(
                a.Label ?? string.Empty,
                a.Street,
                a.City,
                a.Region ?? string.Empty,
                a.PostalCode ?? string.Empty,
                a.Country))
            .ToList();

        return new PersonalDataExport(
            customer.Email,
            customer.Locale,
            customer.CreatedAt,
            orderExports,
            addressExports,
            roles);
    }
}
