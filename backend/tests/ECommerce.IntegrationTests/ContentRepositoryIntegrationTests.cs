using ECommerce.Domain.Content;
using ECommerce.Infrastructure.Content;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Outbox;
using ECommerce.Shared.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ECommerce.IntegrationTests;

[Collection("Integration")]
public sealed class ContentRepositoryIntegrationTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public ContentRepositoryIntegrationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Banner_Persists_And_Reloads()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await IntegrationFixture.EnsureDatabaseReadyAsync();

        var banner = Banner.Create(null, "Flash Sale", "https://img.example/sale.png", "/sale", 1, true);
        await SaveAsync(_ => new ContentRepository(_).AddBanner(banner));

        using var readContext = CreateContext();
        var repository = new ContentRepository(readContext);
        var reloaded = await repository.GetBannerByIdAsync(banner.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal("Flash Sale", reloaded.Title);
        Assert.True(reloaded.IsActive);
    }

    [SkippableFact]
    public async Task Page_BySlug_Filters_Published()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await IntegrationFixture.EnsureDatabaseReadyAsync();

        var draft = Page.Create(null, "Draft", "draft-page", "<p>d</p>", null, null, false);
        var published = Page.Create(null, "About", "about-us", "<p>a</p>", null, null, true);

        await SaveAsync(context =>
        {
            var repository = new ContentRepository(context);
            repository.AddPage(draft);
            repository.AddPage(published);
        });

        using var readContext = CreateContext();
        var repository = new ContentRepository(readContext);

        var bySlug = await repository.GetPageBySlugAsync("draft-page", CancellationToken.None);
        Assert.NotNull(bySlug);

        var publishedOnly = await repository.GetPublishedPageBySlugAsync("draft-page", CancellationToken.None);
        Assert.Null(publishedOnly);

        var about = await repository.GetPublishedPageBySlugAsync("about-us", CancellationToken.None);
        Assert.NotNull(about);
    }

    [SkippableFact]
    public async Task CmsLayout_With_Sections_Persists_And_Reloads()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await IntegrationFixture.EnsureDatabaseReadyAsync();

        var utcNow = DateTime.UtcNow;
        var layout = CmsLayout.Create(null, "Homepage", "homepage", true, utcNow);
        layout.ReplaceSections([
            CmsLayoutSection.Create(layout.Id, "Hero", CmsSectionType.Hero, 0, """{"headline":"Welcome"}""", true, utcNow),
            CmsLayoutSection.Create(layout.Id, "Carousel", CmsSectionType.BannerCarousel, 1, null, true, utcNow)
        ]);
        await SaveAsync(_ => new ContentRepository(_).AddLayout(layout));

        using var readContext = CreateContext();
        var repository = new ContentRepository(readContext);
        var reloaded = await repository.GetLayoutByIdAsync(layout.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal("homepage", reloaded.Slug);
        Assert.Equal(2, reloaded.Sections.Count);
        Assert.Contains(reloaded.Sections, section => section.Type == CmsSectionType.Hero);
    }

    [SkippableFact]
    public async Task CmsLayout_Section_Replacement_Deletes_Old_Sections()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await IntegrationFixture.EnsureDatabaseReadyAsync();

        var utcNow = DateTime.UtcNow;
        var layout = CmsLayout.Create(null, "Homepage", "homepage", true, utcNow);
        layout.ReplaceSections([
            CmsLayoutSection.Create(layout.Id, "Old Hero", CmsSectionType.Hero, 0, null, true, utcNow)
        ]);
        await SaveAsync(_ => new ContentRepository(_).AddLayout(layout));

        using (var updateContext = CreateContext())
        {
            var updateRepo = new ContentRepository(updateContext);
            var loaded = await updateRepo.GetLayoutByIdAsync(layout.Id, CancellationToken.None);
            Assert.NotNull(loaded);

            loaded.ReplaceSections([
                CmsLayoutSection.Create(loaded.Id, "New Carousel", CmsSectionType.BannerCarousel, 0, null, true, utcNow)
            ]);

            await updateContext.SaveChangesAsync(CancellationToken.None);
        }

        using var readContext = CreateContext();
        var repository = new ContentRepository(readContext);
        var reloaded = await repository.GetLayoutByIdAsync(layout.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        var section = Assert.Single(reloaded.Sections);
        Assert.Equal("New Carousel", section.Title);
    }

    private async Task SaveAsync(Action<ECommerceDbContext> action)
    {
        using var context = CreateContext();
        action(context);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    private ECommerceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseNpgsql(_fixture.PostgresConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .AddInterceptors(new DomainEventsInterceptor())
            .Options;
        return new ECommerceDbContext(options);
    }
}
