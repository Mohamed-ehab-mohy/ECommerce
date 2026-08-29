using ECommerce.Domain.Content;
using ECommerce.Shared.Content;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Content.Commands;
using ECommerce.UseCases.Content.Handlers;
using ECommerce.UseCases.Content.Ports;

namespace ECommerce.UnitTests;

public sealed class ContentCommandHandlerTests
{
    private sealed class DummyTenantService : ITenantService
    {
        public Guid? GetCurrentTenantId() => null;

        public void SetCurrentTenantId(Guid? tenantId)
        {
        }
    }

    private sealed class FakeContentRepository : IContentRepository
    {
        public List<Banner> Banners { get; } = [];

        public List<Page> Pages { get; } = [];

        public List<CmsLayout> Layouts { get; } = [];

        public Task<Banner?> GetBannerByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Banners.FirstOrDefault(banner => banner.Id == id));

        public Task<IReadOnlyList<Banner>> ListBannersAsync(int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Banner>>(Banners.Where(banner => banner.IsActive).ToList());

        public Task<int> CountBannersAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Banners.Count(banner => banner.IsActive));

        public void AddBanner(Banner banner) => Banners.Add(banner);

        public Task<Page?> GetPageByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Pages.FirstOrDefault(page => page.Id == id));

        public Task<Page?> GetPageBySlugAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult(Pages.FirstOrDefault(page => page.Slug == slug));

        public Task<Page?> GetPublishedPageBySlugAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult(Pages.FirstOrDefault(page => page.Slug == slug && page.IsPublished));

        public Task<IReadOnlyList<Page>> ListPagesAsync(int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Page>>(Pages.Where(p => p.IsPublished).ToList());

        public Task<int> CountPagesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Pages.Count(p => p.IsPublished));

        public void AddPage(Page page) => Pages.Add(page);

        public Task<CmsLayout?> GetLayoutByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Layouts.FirstOrDefault(layout => layout.Id == id));

        public Task<CmsLayout?> GetLayoutBySlugAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult(Layouts.FirstOrDefault(layout => layout.Slug == slug));

        public Task<CmsLayout?> GetActiveLayoutBySlugAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult(Layouts.FirstOrDefault(layout => layout.Slug == slug && layout.IsActive));

        public Task<IReadOnlyList<CmsLayout>> ListLayoutsAsync(int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CmsLayout>>(Layouts.Where(l => l.IsActive).ToList());

        public Task<int> CountLayoutsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Layouts.Count(l => l.IsActive));

        public void AddLayout(CmsLayout layout) => Layouts.Add(layout);
    }

    private readonly FakeContentRepository _content = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly DummyTenantService _tenantService = new();
    private readonly FakeAuditLogWriter _audit = new();

    private CreateBannerCommandHandler CreateBannerHandler =>
        new(_content, _unitOfWork, _tenantService, new CreateBannerCommandValidator(), _audit);

    private UpdateBannerCommandHandler UpdateBannerHandler =>
        new(_content, _unitOfWork, new UpdateBannerCommandValidator(), _audit);

    private DeactivateBannerCommandHandler DeactivateBannerHandler =>
        new(_content, _unitOfWork, _audit);

    private CreatePageCommandHandler CreatePageHandler =>
        new(_content, _unitOfWork, _tenantService, new CreatePageCommandValidator(), _audit);

    private UpdatePageCommandHandler UpdatePageHandler =>
        new(_content, _unitOfWork, new UpdatePageCommandValidator(), _audit);

    private CreateCmsLayoutCommandHandler CreateLayoutHandler =>
        new(_content, _unitOfWork, _tenantService, _timeProvider, new CreateCmsLayoutCommandValidator(), _audit);

    private UpdateCmsLayoutCommandHandler UpdateLayoutHandler =>
        new(_content, _unitOfWork, _timeProvider, new UpdateCmsLayoutCommandValidator(), _audit);

    private DeactivateCmsLayoutCommandHandler DeactivateLayoutHandler =>
        new(_content, _unitOfWork, _audit);

    [Fact]
    public async Task CreateBanner_Adds_Banner_And_Audits()
    {
        var result = await CreateBannerHandler.Handle(
            new CreateBannerCommand(null, "Flash Sale", "https://img.example/sale.png", "/sale", 1, true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var banner = Assert.Single(_content.Banners);
        Assert.Equal("Flash Sale", banner.Title);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_audit.Operations);
    }

    [Fact]
    public async Task CreateBanner_With_Invalid_ImageUrl_Returns_Validation_Failure()
    {
        var result = await CreateBannerHandler.Handle(
            new CreateBannerCommand(null, "Sale", string.Empty, null, 1, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
        Assert.Empty(_content.Banners);
    }

    [Fact]
    public async Task UpdateBanner_Updates_And_Audits()
    {
        var banner = Banner.Create(null, "Old", "https://img/old.png", null, 1, true);
        _content.Banners.Add(banner);

        var result = await UpdateBannerHandler.Handle(
            new UpdateBannerCommand(banner.Id, "New", "https://img/new.png", "/new", 2, false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", banner.Title);
        Assert.False(banner.IsActive);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_audit.Operations);
    }

    [Fact]
    public async Task UpdateBanner_With_Unknown_Id_Returns_NotFound()
    {
        var result = await UpdateBannerHandler.Handle(
            new UpdateBannerCommand(Guid.NewGuid(), "New", "https://img/new.png", null, 1, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.BannerNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task DeactivateBanner_Sets_Inactive_And_Audits()
    {
        var banner = Banner.Create(null, "Sale", "https://img/sale.png", null, 1, true);
        _content.Banners.Add(banner);

        var result = await DeactivateBannerHandler.Handle(
            new DeactivateBannerCommand(banner.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(banner.IsActive);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_audit.Operations);
    }

    [Fact]
    public async Task DeactivateBanner_With_Unknown_Id_Returns_NotFound()
    {
        var result = await DeactivateBannerHandler.Handle(
            new DeactivateBannerCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.BannerNotFound, result.Error);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreatePage_Adds_Page_And_Audits()
    {
        var result = await CreatePageHandler.Handle(
            new CreatePageCommand(null, "About Us", "about-us", "<h1>About</h1>", "About", "Our story", true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var page = Assert.Single(_content.Pages);
        Assert.Equal("about-us", page.Slug);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_audit.Operations);
    }

    [Fact]
    public async Task CreatePage_With_Duplicate_Slug_Returns_Conflict()
    {
        _content.Pages.Add(Page.Create(null, "Existing", "about-us", "<p>x</p>", null, null, true));

        var result = await CreatePageHandler.Handle(
            new CreatePageCommand(null, "Dupe", "about-us", "<p>y</p>", null, null, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.PageSlugAlreadyExists, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdatePage_With_Unknown_Id_Returns_NotFound()
    {
        var result = await UpdatePageHandler.Handle(
            new UpdatePageCommand(Guid.NewGuid(), "X", "x", "<p>x</p>", null, null, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.PageNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task CreateLayout_Adds_Layout_With_Sections_And_Audits()
    {
        var result = await CreateLayoutHandler.Handle(
            new CreateCmsLayoutCommand(
                null,
                "Homepage",
                "homepage",
                true,
                [new CmsLayoutSectionInput("Hero", CmsSectionType.Hero, 0, """{"headline":"Welcome"}""", true)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var layout = Assert.Single(_content.Layouts);
        Assert.Equal("homepage", layout.Slug);
        Assert.Single(layout.Sections);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_audit.Operations);
    }

    [Fact]
    public async Task CreateLayout_With_Duplicate_Slug_Returns_Conflict()
    {
        _content.Layouts.Add(CmsLayout.Create(null, "Homepage", "homepage", true, DateTime.UtcNow));

        var result = await CreateLayoutHandler.Handle(
            new CreateCmsLayoutCommand(null, "Homepage 2", "homepage", true, []),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ContentErrors.LayoutSlugAlreadyExists, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateLayout_Replaces_Sections_And_Audits()
    {
        var layout = CmsLayout.Create(null, "Homepage", "homepage", true, DateTime.UtcNow);
        layout.ReplaceSections([
            CmsLayoutSection.Create(layout.Id, "Hero", CmsSectionType.Hero, 0, null, true, DateTime.UtcNow)
        ]);
        _content.Layouts.Add(layout);

        var result = await UpdateLayoutHandler.Handle(
            new UpdateCmsLayoutCommand(
                layout.Id,
                "Homepage v2",
                "homepage-v2",
                true,
                [new CmsLayoutSectionInput("Carousel", CmsSectionType.BannerCarousel, 0, null, true)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("homepage-v2", layout.Slug);
        var section = Assert.Single(layout.Sections);
        Assert.Equal(CmsSectionType.BannerCarousel, section.Type);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_audit.Operations);
    }

    [Fact]
    public async Task DeactivateLayout_Sets_Inactive_And_Audits()
    {
        var layout = CmsLayout.Create(null, "Homepage", "homepage", true, DateTime.UtcNow);
        _content.Layouts.Add(layout);

        var result = await DeactivateLayoutHandler.Handle(
            new DeactivateCmsLayoutCommand(layout.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(layout.IsActive);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Single(_audit.Operations);
    }
}
