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

    private readonly IEmailReaderService inner;
    private readonly HybridCache cache;
    private readonly HybridCacheEntryOptions entryOptions;

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
}
