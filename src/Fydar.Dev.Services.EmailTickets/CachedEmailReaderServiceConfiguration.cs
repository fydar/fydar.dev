using System;

namespace Fydar.Dev.Services.EmailTickets;

public class CachedEmailReaderServiceConfiguration
{
    /// <summary>
    /// The expiration duration for cached tickets.
    /// </summary>
    public TimeSpan Expiration { get; set; } = TimeSpan.FromHours(1);
}
