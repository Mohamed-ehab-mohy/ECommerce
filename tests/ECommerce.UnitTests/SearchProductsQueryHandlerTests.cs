using ECommerce.Domain.Catalog;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Catalog.Handlers;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Pricing;

namespace ECommerce.UnitTests;

public sealed class SearchProductsQueryHandlerTests
{
    private readonly FakeProductSearchRepository _search = new();

    private readonly ILocaleCatalog _locales = new DefaultLocaleCatalog();

    private readonly ICurrencyCatalog _currencies = new DefaultCurrencyCatalog();

    private SearchProductsQueryHandler GetHandler =>
        new(_search, _locales, _currencies, new SearchProductsQueryValidator(_currencies, _locales));

    [Fact]
    public async Task Search_Returns_Items_And_Facets()
    {
        var product = CreateProduct("SKU-001", "wireless-headphones", name: "Wireless Headphones");
        _search.Products.Add(product);
        _search.TotalCount = 1;
        _search.Facets = new ProductSearchFacets(
            [new ProductFacetBucket(Guid.NewGuid(), "Electronics", 1)],
            [new ProductFacetBucket(Guid.NewGuid(), "Sony", 1)],
            [new PriceRangeFacet("under-50", "Under $50", null, 50m, 0)],
            [new RatingFacet(4, 1)]);

        var result = await GetHandler.Handle(
            new SearchProductsQuery("wireless", null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var response = result.Value;
        Assert.Equal(1, response.TotalCount);
        Assert.False(response.HasNext);
        Assert.Equal("wireless-headphones", response.Items[0].Slug);
        Assert.Single(response.Facets.Categories);
        Assert.Single(response.Facets.Brands);
        Assert.Single(response.Facets.PriceRanges);
        Assert.Single(response.Facets.Ratings);
    }

    [Fact]
    public async Task Search_HasNext_True_When_More_Pages_Exist()
    {
        _search.Products.Add(CreateProduct("SKU-002", "one", name: "One"));
        _search.TotalCount = 25;

        var result = await GetHandler.Handle(
            new SearchProductsQuery(null, null, null, null, null, null, Page: 1, PageSize: 20),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasNext);
    }

    [Fact]
    public async Task Search_HasNext_False_On_Last_Page()
    {
        _search.Products.Add(CreateProduct("SKU-003", "one", name: "One"));
        _search.TotalCount = 5;

        var result = await GetHandler.Handle(
            new SearchProductsQuery(null, null, null, null, null, null, Page: 1, PageSize: 20),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasNext);
    }

    [Fact]
    public async Task Search_Passes_Filters_And_Paging_To_Repository()
    {
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        var result = await GetHandler.Handle(
            new SearchProductsQuery(
                "headphones",
                categoryId,
                brandId,
                50m,
                250m,
                4m,
                Page: 2,
                PageSize: 25),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var criteria = Assert.Single(_search.ReceivedCriteria);
        Assert.Equal("headphones", criteria.Query);
        Assert.Equal(categoryId, criteria.CategoryId);
        Assert.Equal(brandId, criteria.BrandId);
        Assert.Equal(50m, criteria.PriceGte);
        Assert.Equal(250m, criteria.PriceLte);
        Assert.Equal(4m, criteria.RatingGte);
        Assert.Equal(2, criteria.Page);
        Assert.Equal(25, criteria.PageSize);
    }

    [Fact]
    public async Task Search_With_Blank_Query_Sends_Null()
    {
        var result = await GetHandler.Handle(
            new SearchProductsQuery("   ", null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(_search.ReceivedCriteria).Query);
    }

    [Fact]
    public async Task Search_Uses_Default_Locale_When_Not_Provided()
    {
        var result = await GetHandler.Handle(
            new SearchProductsQuery(null, null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("en", Assert.Single(_search.ReceivedCriteria).Locale);
    }

    [Fact]
    public async Task Search_With_Invalid_Page_Returns_Validation_Failure()
    {
        var result = await GetHandler.Handle(
            new SearchProductsQuery(null, null, null, null, null, null, Page: 0),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Search_With_Inverted_Price_Range_Returns_Validation_Failure()
    {
        var result = await GetHandler.Handle(
            new SearchProductsQuery(null, null, null, PriceGte: 300m, PriceLte: 50m, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Search_With_Unsupported_Currency_Returns_Validation_Failure()
    {
        var result = await GetHandler.Handle(
            new SearchProductsQuery(null, null, null, null, null, null, Currency: "JPY"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Search_Converts_Price_To_Requested_Currency()
    {
        var product = CreateProduct("SKU-004", "convertible", name: "Convertible");
        _search.Products.Add(product);

        var result = await GetHandler.Handle(
            new SearchProductsQuery("convertible", null, null, null, null, null, Currency: "AED"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal("AED", item.Currency);
        Assert.Equal(367.25m, item.ListAmount);
    }

    private static Product CreateProduct(
        string sku,
        string slug,
        string name = "Test Product") =>
        Product.Create(
            sku,
            slug,
            "en",
            name,
            null,
            "USD",
            100m,
            null,
            null,
            null,
            false,
            ProductStatus.Active,
            DateTime.UtcNow);
}
