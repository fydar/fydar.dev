using System;

namespace Fydar.Dev.Services.EmailTickets;

public class CachedEmailReaderServiceConfiguration
{
    /// <summary>
    /// The expiration duration for cached tickets.
    /// </summary>
    public TimeSpan Expiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// <para>The expiration duration for a cached page of the ticket listing.</para>
    /// <para>Unlike a ticket, the listing gains an entry every time mail arrives, so this is a
    /// staleness window and is kept short.</para>
    /// </summary>
    public TimeSpan ListingExpiration { get; set; } = TimeSpan.FromSeconds(30);
}
