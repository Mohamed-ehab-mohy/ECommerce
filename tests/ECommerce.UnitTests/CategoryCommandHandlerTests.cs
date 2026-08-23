using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Audit;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Handlers;
using ECommerce.UseCases.Catalog.Queries;

namespace ECommerce.UnitTests;

public sealed class CategoryCommandHandlerTests
{
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly FakeAuditEntryRepository _auditEntries = new();
    private readonly FakeAuditContextProvider _auditContext = new();

    private CreateCategoryCommandHandler CreateHandler =>
        new(_categories, _unitOfWork, _timeProvider, new CreateCategoryCommandValidator(),
            new AuditLogWriter(_auditEntries, _auditContext));

    private UpdateCategoryCommandHandler UpdateHandler =>
        new(_categories, _unitOfWork, _timeProvider, new UpdateCategoryCommandValidator(),
            new AuditLogWriter(_auditEntries, _auditContext));

    private GetCategoryTreeQueryHandler TreeHandler => new(_categories);

    [Fact]
    public async Task CreateCategory_As_Root_Adds_Level_One()
    {
        var result = await CreateHandler.Handle(
            new CreateCategoryCommand("Electronics", "electronics", null, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var category = Assert.Single(_categories.Categories);
        Assert.Equal(1, category.Level);
        Assert.Null(category.ParentId);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateCategory_With_Parent_Sets_Level_From_Parent()
    {
        var parent = Category.Create("Electronics", "electronics", null, 1, 1, DateTime.UtcNow);
        _categories.Categories.Add(parent);

        var result = await CreateHandler.Handle(
            new CreateCategoryCommand("Phones", "phones", parent.Id, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var category = _categories.Categories.Single(item => item.Id != parent.Id);
        Assert.Equal(2, category.Level);
        Assert.Equal(parent.Id, category.ParentId);
    }

    [Fact]
    public async Task CreateCategory_With_Depth_Over_Five_Returns_BadRequest()
    {
        var level5 = Category.Create("Level5", "level-5", null, 1, 5, DateTime.UtcNow);
        _categories.Categories.Add(level5);

        var result = await CreateHandler.Handle(
            new CreateCategoryCommand("Level6", "level-6", level5.Id, 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.DepthLimitExceeded, result.Error);
        Assert.Equal(ErrorType.BadRequest, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateCategory_With_Unknown_Parent_Returns_NotFound()
    {
        var result = await CreateHandler.Handle(
            new CreateCategoryCommand("Orphan", "orphan", Guid.NewGuid(), 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.ParentNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task CreateCategory_With_Duplicate_Slug_Returns_Conflict()
    {
        _categories.Categories.Add(Category.Create("Existing", "electronics", null, 1, 1, DateTime.UtcNow));

        var result = await CreateHandler.Handle(
            new CreateCategoryCommand("New", "electronics", null, 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.SlugAlreadyExists, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task CreateCategory_With_Invalid_Slug_Returns_Validation_Failure()
    {
        var result = await CreateHandler.Handle(
            new CreateCategoryCommand("Bad", "Has Spaces", null, 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task UpdateCategory_Changing_Parent_To_Its_Descendant_Returns_BadRequest()
    {
        var root = Category.Create("Root", "root", null, 1, 1, DateTime.UtcNow);
        var child = Category.Create("Child", "child", root.Id, 1, 2, DateTime.UtcNow);
        var grandChild = Category.Create("GrandChild", "grand-child", child.Id, 1, 3, DateTime.UtcNow);
        _categories.Categories.Add(root);
        _categories.Categories.Add(child);
        _categories.Categories.Add(grandChild);

        var result = await UpdateHandler.Handle(
            new UpdateCategoryCommand(root.Id, null, null, grandChild.Id, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.CycleDetected, result.Error);
        Assert.Equal(ErrorType.BadRequest, result.Error.Type);
        Assert.Null(root.ParentId);
        Assert.Equal(1, root.Level);
    }

    [Fact]
    public async Task UpdateCategory_With_Self_As_Parent_Returns_BadRequest()
    {
        var category = Category.Create("Solo", "solo", null, 1, 1, DateTime.UtcNow);
        _categories.Categories.Add(category);

        var result = await UpdateHandler.Handle(
            new UpdateCategoryCommand(category.Id, null, null, category.Id, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.CycleDetected, result.Error);
    }

    [Fact]
    public async Task UpdateCategory_Updates_Details()
    {
        var category = Category.Create("Old", "old", null, 1, 1, DateTime.UtcNow);
        _categories.Categories.Add(category);

        var result = await UpdateHandler.Handle(
            new UpdateCategoryCommand(category.Id, "New Name", "new-slug", null, 5),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", category.Name);
        Assert.Equal("new-slug", category.Slug);
        Assert.Equal(5, category.SortOrder);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_auditEntries.Entries);
    }

    [Fact]
    public async Task UpdateCategory_With_Unknown_Id_Returns_NotFound()
    {
        var result = await UpdateHandler.Handle(
            new UpdateCategoryCommand(Guid.NewGuid(), "Name", null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.CategoryNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task GetCategoryTree_Returns_Nested_Structure()
    {
        var root = Category.Create("Electronics", "electronics", null, 1, 1, DateTime.UtcNow);
        var phones = Category.Create("Phones", "phones", root.Id, 1, 2, DateTime.UtcNow);
        var accessories = Category.Create("Accessories", "accessories", root.Id, 2, 2, DateTime.UtcNow);
        var chargers = Category.Create("Chargers", "chargers", accessories.Id, 1, 3, DateTime.UtcNow);
        _categories.Categories.Add(root);
        _categories.Categories.Add(phones);
        _categories.Categories.Add(accessories);
        _categories.Categories.Add(chargers);

        var result = await TreeHandler.Handle(new GetCategoryTreeQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var rootNode = Assert.Single(result.Value);
        Assert.Equal("electronics", rootNode.Slug);
        Assert.Equal(2, rootNode.Children.Count);

        var accessoriesNode = rootNode.Children.Single(node => node.Slug == "accessories");
        var chargerNode = Assert.Single(accessoriesNode.Children);
        Assert.Equal("chargers", chargerNode.Slug);
        Assert.Empty(chargerNode.Children);
    }
}
