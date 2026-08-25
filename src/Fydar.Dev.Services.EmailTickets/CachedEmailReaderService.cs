using Fydar.Dev.Services.EmailTickets.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fydar.Dev.Services.EmailTickets;

public class CachedEmailReaderService : IEmailReaderService
{
    private const string cacheKeyPrefix = "email-ticket:";
    private const string listingCacheKeyPrefix = "email-ticket-listing:";
    private const string listingCacheTag = "email-ticket-listing";

    private readonly IEmailReaderService inner;
    private readonly HybridCache cache;
    private readonly HybridCacheEntryOptions entryOptions;
    private readonly HybridCacheEntryOptions listingEntryOptions;

    public CachedEmailReaderService(
        S3EmailReaderService inner,
        HybridCache cache,
        IOptions<CachedEmailReaderServiceConfiguration> configuration)
    {
        this.inner = inner;
        this.cache = cache;

        entryOptions = new HybridCacheEntryOptions()
        {
            Expiration = configuration.Value.Expiration,
            LocalCacheExpiration = configuration.Value.Expiration
        };

        listingEntryOptions = new HybridCacheEntryOptions()
        {
            Expiration = configuration.Value.ListingExpiration,
            LocalCacheExpiration = configuration.Value.ListingExpiration
        };
    }

    /// <inheritdoc/>
    public async Task<MimeMessage> ReadEmailAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(
            cacheKeyPrefix + ticketId,
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
            $"{listingCacheKeyPrefix}{pageNumber}:{pageSize}",
            (inner, pageNumber, pageSize),
            static (state, cancellationToken) => new ValueTask<TicketPageModel>(
                state.inner.ListEmailsAsync(state.pageNumber, state.pageSize, cancellationToken)),
            listingEntryOptions,
            tags: [listingCacheTag],
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> DeleteEmailsAsync(
        IReadOnlyCollection<string> ticketIds,
        CancellationToken cancellationToken = default)
    {
        var moved = await inner.DeleteEmailsAsync(ticketIds, cancellationToken);

        await InvalidateAsync(moved, cancellationToken);

        return moved;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> RestoreEmailsAsync(
        IReadOnlyCollection<string> ticketIds,
        CancellationToken cancellationToken = default)
    {
        var moved = await inner.RestoreEmailsAsync(ticketIds, cancellationToken);

        await InvalidateAsync(moved, cancellationToken);

        return moved;
    }

    private async Task InvalidateAsync(
        IReadOnlyCollection<string> movedTicketIds,
        CancellationToken cancellationToken)
    {
        if (movedTicketIds.Count == 0)
        {
            return;
        }

        await cache.RemoveAsync(movedTicketIds.Select(id => cacheKeyPrefix + id), cancellationToken);
        await cache.RemoveByTagAsync(listingCacheTag, cancellationToken);
    }
}
