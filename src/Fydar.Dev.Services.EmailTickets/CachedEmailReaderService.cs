using Fydar.Dev.Services.EmailTickets.Models;
using Microsoft.Extensions.Caching.Hybrid;
using MimeKit;
using System.Threading;
using System.Threading.Tasks;

namespace Fydar.Dev.Services.EmailTickets;

public class CachedEmailReaderService : IEmailReaderService
{
    // Namespaced so the keys stay unambiguous if a distributed second level is ever added behind
    // the same cache.
    private const string CacheKeyPrefix = "email-ticket:";
    private const string ListingCacheKeyPrefix = "email-ticket-listing:";

    private readonly IEmailReaderService inner;
    private readonly HybridCache cache;
    private readonly HybridCacheEntryOptions entryOptions;
    private readonly HybridCacheEntryOptions listingEntryOptions;

    public CachedEmailReaderService(
        IEmailReaderService inner,
        HybridCache cache,
        CachedEmailReaderServiceConfiguration configuration)
    {
        this.inner = inner;
        this.cache = cache;

        entryOptions = new HybridCacheEntryOptions()
        {
            Expiration = configuration.Expiration,
            LocalCacheExpiration = configuration.Expiration
        };

        listingEntryOptions = new HybridCacheEntryOptions()
        {
            Expiration = configuration.ListingExpiration,
            LocalCacheExpiration = configuration.ListingExpiration
        };
    }

    public async Task<MimeMessage> ReadEmailAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(
            CacheKeyPrefix + ticketId,
            (inner, ticketId),
            static (state, cancellationToken) => new ValueTask<MimeMessage>(
                state.inner.ReadEmailAsync(state.ticketId, cancellationToken)),
            entryOptions,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TicketPageModel> ListEmailsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(
            $"{ListingCacheKeyPrefix}{pageNumber}:{pageSize}",
            (inner, pageNumber, pageSize),
            static (state, cancellationToken) => new ValueTask<TicketPageModel>(
                state.inner.ListEmailsAsync(state.pageNumber, state.pageSize, cancellationToken)),
            listingEntryOptions,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteEmailAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        await inner.DeleteEmailAsync(ticketId, cancellationToken);

        // A cached read of the now-deleted ticket would otherwise keep serving it until expiry.
        await cache.RemoveAsync(CacheKeyPrefix + ticketId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RestoreEmailAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        await inner.RestoreEmailAsync(ticketId, cancellationToken);

        await cache.RemoveAsync(CacheKeyPrefix + ticketId, cancellationToken);
    }
}
