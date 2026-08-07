using ECommerce.Domain.Cart;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Cart.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Carts;

public sealed class CartRepository : ICartRepository
{
    private const int StampedeLockMilliseconds = 100;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(30);
    private static readonly RedisKey CachePrefix = "cart:";

    private readonly ECommerceDbContext _dbContext;
    private readonly IDatabase _cache;
    private readonly ILogger<CartRepository> _logger;
    private long _cacheHits;
    private long _cacheMisses;

    public CartRepository(ECommerceDbContext dbContext, IConnectionMultiplexer redis, ILogger<CartRepository> logger)
    {
        _dbContext = dbContext;
        _cache = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<Cart?> ByOwnerKeyAsync(string ownerKey, CancellationToken cancellationToken)
    {
        var key = new RedisKey($"{CachePrefix}{ownerKey}");

        var cached = await _cache.StringGetAsync(key);
        if (!cached.IsNullOrEmpty)
        {
            Interlocked.Increment(ref _cacheHits);
            LogHitRatio();
            return CartCacheCodec.Deserialize(cached.ToString());
        }

        Interlocked.Increment(ref _cacheMisses);
        LogHitRatio();

        var lockKey = new RedisKey($"{CachePrefix}{ownerKey}:lock");
        var lockAcquired = await _cache.StringSetAsync(lockKey, "1", TimeSpan.FromMilliseconds(StampedeLockMilliseconds), When.NotExists);

        if (lockAcquired)
        {
            try
            {
                var cart = await LoadFromStoreAsync(ownerKey, cancellationToken);
                if (cart is not null)
                {
                    await _cache.StringSetAsync(key, CartCacheCodec.Serialize(cart), CacheTtl);
                }

                return cart;
            }
            finally
            {
                await _cache.KeyDeleteAsync(lockKey);
            }
        }

        await Task.Delay(TimeSpan.FromMilliseconds(StampedeLockMilliseconds), cancellationToken);

        var retry = await _cache.StringGetAsync(key);
        if (!retry.IsNullOrEmpty)
        {
            return CartCacheCodec.Deserialize(retry.ToString());
        }

        var retried = await LoadFromStoreAsync(ownerKey, cancellationToken);
        if (retried is not null)
        {
            await _cache.StringSetAsync(key, CartCacheCodec.Serialize(retried), CacheTtl);
        }

        return retried;
    }

    public Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<Cart>()
            .Include(cart => cart.Items)
            .SingleOrDefaultAsync(cart => cart.Id == id, cancellationToken);

    public async Task SaveAsync(Cart cart, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Set<Cart>()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == cart.Id, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (existing is null)
        {
            _dbContext.Set<Cart>().Add(cart);
        }
        else
        {
            if (existing.Version != cart.Version)
            {
                throw new CartConcurrencyException(
                    $"Cart {cart.Id} was modified concurrently. Expected version {existing.Version}, got {cart.Version}.");
            }

            cart.SetVersion(cart.Version + 1);

            await _dbContext.Set<CartItem>()
                .Where(item => item.CartId == cart.Id)
                .ExecuteDeleteAsync(cancellationToken);

            var entry = _dbContext.Set<Cart>().Attach(cart);
            entry.Property(item => item.Version).OriginalValue = existing.Version;
            entry.State = EntityState.Modified;

            foreach (var item in cart.Items)
            {
                _dbContext.Entry(item).State = EntityState.Added;
            }
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new CartConcurrencyException(
                $"Cart {cart.Id} was modified concurrently.",
                exception);
        }

        var key = new RedisKey($"{CachePrefix}{cart.OwnerKey}");
        await _cache.StringSetAsync(key, CartCacheCodec.Serialize(cart), CacheTtl);
    }

    private Task<Cart?> LoadFromStoreAsync(string ownerKey, CancellationToken cancellationToken) =>
        _dbContext.Set<Cart>()
            .Include(cart => cart.Items)
            .SingleOrDefaultAsync(cart => cart.OwnerKey == ownerKey, cancellationToken);

    private void LogHitRatio()
    {
        var total = Interlocked.Read(ref _cacheHits) + Interlocked.Read(ref _cacheMisses);
        if (total == 0)
        {
            return;
        }

        var ratio = Interlocked.Read(ref _cacheHits) / (double)total;
        _logger.LogInformation("Cart cache hit ratio: {HitRatio:P2} ({Hits} hits / {Misses} misses)", ratio, _cacheHits, _cacheMisses);
    }
}
