using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Audit;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Handlers;
using ECommerce.UseCases.Catalog.Queries;

namespace ECommerce.UnitTests;

public sealed class BrandCommandHandlerTests
{
    private readonly FakeBrandRepository _brands = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly FakeAuditEntryRepository _auditEntries = new();
    private readonly FakeAuditContextProvider _auditContext = new();

    private CreateBrandCommandHandler CreateHandler =>
        new(_brands, _unitOfWork, _timeProvider, new CreateBrandCommandValidator(),
            new AuditLogWriter(_auditEntries, _auditContext));

    private UpdateBrandCommandHandler UpdateHandler =>
        new(_brands, _unitOfWork, _timeProvider, new UpdateBrandCommandValidator(),
            new AuditLogWriter(_auditEntries, _auditContext));

    private ListBrandsQueryHandler ListHandler =>
        new(_brands, new ListBrandsQueryValidator());

    [Fact]
    public async Task CreateBrand_Adds_Brand_And_Audits()
    {
        var result = await CreateHandler.Handle(
            new CreateBrandCommand("Acme", "Acme Inc.", "https://acme.example"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var brand = Assert.Single(_brands.Brands);
        Assert.Equal("Acme", brand.Name);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_auditEntries.Entries);
    }

    [Fact]
    public async Task CreateBrand_With_Duplicate_Name_Returns_Conflict()
    {
        _brands.Brands.Add(Brand.Create("Acme", null, null, DateTime.UtcNow));

        var result = await CreateHandler.Handle(
            new CreateBrandCommand("Acme", null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(BrandErrors.NameAlreadyExists, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateBrand_With_Invalid_Website_Returns_Validation_Failure()
    {
        var result = await CreateHandler.Handle(
            new CreateBrandCommand("Acme", null, "not-a-url"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task UpdateBrand_Updates_Fields()
    {
        var brand = Brand.Create("Old", "Desc", "https://old.example", DateTime.UtcNow);
        _brands.Brands.Add(brand);

        var result = await UpdateHandler.Handle(
            new UpdateBrandCommand(brand.Id, "New", null, "https://new.example"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", brand.Name);
        Assert.Equal("https://new.example", brand.Website);
        Assert.Equal("Desc", brand.Description);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateBrand_With_Unknown_Id_Returns_NotFound()
    {
        var result = await UpdateHandler.Handle(
            new UpdateBrandCommand(Guid.NewGuid(), "Name", null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(BrandErrors.BrandNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task UpdateBrand_With_Duplicate_Name_Returns_Conflict()
    {
        var brand = Brand.Create("Mine", null, null, DateTime.UtcNow);
        _brands.Brands.Add(brand);
        _brands.Brands.Add(Brand.Create("Taken", null, null, DateTime.UtcNow));

        var result = await UpdateHandler.Handle(
            new UpdateBrandCommand(brand.Id, "Taken", null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(BrandErrors.NameAlreadyExists, result.Error);
    }

    [Fact]
    public async Task ListBrands_Returns_Paged_Items()
    {
        _brands.Brands.Add(Brand.Create("Zebra", null, null, DateTime.UtcNow));
        _brands.Brands.Add(Brand.Create("Alpha", null, null, DateTime.UtcNow));

        var result = await ListHandler.Handle(new ListBrandsQuery(1, 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal("Alpha", result.Value.Items[0].Name);
        Assert.Equal("Zebra", result.Value.Items[1].Name);
    }
}
