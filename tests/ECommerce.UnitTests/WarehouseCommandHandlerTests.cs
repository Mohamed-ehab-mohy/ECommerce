using ECommerce.Domain.Inventory;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Audit;
using ECommerce.UseCases.Inventory.Commands;
using ECommerce.UseCases.Inventory.Handlers;

namespace ECommerce.UnitTests;

public sealed class WarehouseCommandHandlerTests
{
    private readonly FakeWarehouseRepository _warehouses = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly FakeAuditEntryRepository _auditEntries = new();
    private readonly FakeAuditContextProvider _auditContext = new();

    private CreateWarehouseCommandHandler CreateHandler =>
        new(_warehouses, _unitOfWork, _timeProvider, new CreateWarehouseCommandValidator(),
            new AuditLogWriter(_auditEntries, _auditContext));

    private UpdateWarehouseCommandHandler UpdateHandler =>
        new(_warehouses, _unitOfWork, _timeProvider, new UpdateWarehouseCommandValidator(),
            new AuditLogWriter(_auditEntries, _auditContext));

    private DeactivateWarehouseCommandHandler DeactivateHandler =>
        new(_warehouses, _unitOfWork, new AuditLogWriter(_auditEntries, _auditContext));

    [Fact]
    public async Task CreateWarehouse_Adds_Warehouse_And_Audits()
    {
        var result = await CreateHandler.Handle(
            new CreateWarehouseCommand("cai-01", "Cairo Hub", "Downtown, Cairo", "Africa/Cairo", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var warehouse = Assert.Single(_warehouses.Warehouses);
        Assert.Equal("CAI-01", warehouse.Code);
        Assert.Equal("Cairo Hub", warehouse.Name);
        Assert.Equal("Africa/Cairo", warehouse.Timezone);
        Assert.Equal(WarehouseStatus.Active, warehouse.Status);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_auditEntries.Entries);
    }

    [Fact]
    public async Task CreateWarehouse_With_Duplicate_Code_Returns_Conflict()
    {
        _warehouses.Warehouses.Add(CreateWarehouse("CAI-01", "Existing"));

        var result = await CreateHandler.Handle(
            new CreateWarehouseCommand("cai-01", "Duplicate", "Address", "UTC", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WarehouseErrors.CodeAlreadyExists, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateWarehouse_With_Invalid_Code_Returns_Validation_Failure()
    {
        var result = await CreateHandler.Handle(
            new CreateWarehouseCommand("a b", "Invalid", "Address", "UTC", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
        Assert.Empty(_warehouses.Warehouses);
    }

    [Fact]
    public async Task CreateWarehouse_With_Invalid_Status_Returns_Validation_Failure()
    {
        var result = await CreateHandler.Handle(
            new CreateWarehouseCommand("CAI-02", "Cairo", "Address", "UTC", "Pending"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task UpdateWarehouse_Updates_Fields_And_Audits()
    {
        var warehouse = CreateWarehouse("CAI-01", "Cairo");
        _warehouses.Warehouses.Add(warehouse);

        var result = await UpdateHandler.Handle(
            new UpdateWarehouseCommand(warehouse.Id, "Cairo North", null, "Africa/Cairo", "Inactive"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cairo North", warehouse.Name);
        Assert.Equal("Address", warehouse.Address);
        Assert.Equal("Africa/Cairo", warehouse.Timezone);
        Assert.Equal(WarehouseStatus.Inactive, warehouse.Status);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_auditEntries.Entries);
    }

    [Fact]
    public async Task UpdateWarehouse_With_Unknown_Id_Returns_NotFound()
    {
        var result = await UpdateHandler.Handle(
            new UpdateWarehouseCommand(Guid.NewGuid(), "Name", null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WarehouseErrors.WarehouseNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task UpdateWarehouse_With_No_Fields_Returns_Validation_Failure()
    {
        var warehouse = CreateWarehouse("CAI-01", "Cairo");
        _warehouses.Warehouses.Add(warehouse);

        var result = await UpdateHandler.Handle(
            new UpdateWarehouseCommand(warehouse.Id, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task DeactivateWarehouse_Sets_Inactive_And_Audits()
    {
        var warehouse = CreateWarehouse("CAI-01", "Cairo");
        _warehouses.Warehouses.Add(warehouse);

        var result = await DeactivateHandler.Handle(
            new DeactivateWarehouseCommand(warehouse.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(WarehouseStatus.Inactive, warehouse.Status);
        Assert.True(warehouse.IsDeleted);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_auditEntries.Entries);
    }

    [Fact]
    public async Task DeactivateWarehouse_With_Unknown_Id_Returns_NotFound()
    {
        var result = await DeactivateHandler.Handle(
            new DeactivateWarehouseCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WarehouseErrors.WarehouseNotFound, result.Error);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    private static Warehouse CreateWarehouse(string code, string name) =>
        Warehouse.Create(code, name, "Address", "UTC", WarehouseStatus.Active, DateTime.UtcNow);
}
