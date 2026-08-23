using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.Domain.Orders;
using Xunit;

namespace ECommerce.UnitTests.Tests.Contract;

public sealed class ApiContractTests
{
    [Fact]
    public void ReturnRequest_HasRequiredProperties()
    {
        var items = new List<ReturnRequestItemDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", 1, 29.99m, "Defective")
        };
        var command = new CreateReturnRequestCommand(Guid.NewGuid(), "Wrong item", true, items);

        Assert.Equal("Wrong item", command.Reason);
        Assert.True(command.Restock);
        Assert.Single(command.Items);
    }

    [Fact]
    public void ProductSearchCriteria_DefaultPage_IsOne()
    {
        var criteria = new ProductSearchCriteria("phone", "en", null, null, null, null, null, 1, 20);

        Assert.Equal(1, criteria.Page);
        Assert.Equal(20, criteria.PageSize);
    }

    [Fact]
    public void ReturnRequestStatus_HasAllStates()
    {
        Assert.Equal(5, Enum.GetValues<ReturnRequestStatus>().Length);
        Assert.Contains(ReturnRequestStatus.Requested, Enum.GetValues<ReturnRequestStatus>());
        Assert.Contains(ReturnRequestStatus.Approved, Enum.GetValues<ReturnRequestStatus>());
        Assert.Contains(ReturnRequestStatus.Rejected, Enum.GetValues<ReturnRequestStatus>());
        Assert.Contains(ReturnRequestStatus.Completed, Enum.GetValues<ReturnRequestStatus>());
        Assert.Contains(ReturnRequestStatus.Cancelled, Enum.GetValues<ReturnRequestStatus>());
    }

    [Fact]
    public void ReturnRequest_CreatesWithCorrectValues()
    {
        var items = new List<ReturnRequestItem>
        {
            ReturnRequestItem.Create(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", 2, 15.50m, "Damaged")
        };
        var rr = ReturnRequest.Create(Guid.NewGuid(), Guid.NewGuid(), "Wrong color", "USD", 31.00m, true, items, DateTime.UtcNow);

        Assert.Equal("Wrong color", rr.Reason);
        Assert.Equal(31.00m, rr.RefundAmount);
        Assert.Equal(ReturnRequestStatus.Requested, rr.Status);
        Assert.Single(rr.Items);
    }
}
