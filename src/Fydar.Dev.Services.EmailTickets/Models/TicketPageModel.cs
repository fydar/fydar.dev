using System;
using System.Collections.Generic;

namespace Fydar.Dev.Services.EmailTickets.Models;

public class TicketPageModel
{
    public IReadOnlyList<TicketSummaryModel> Tickets { get; set; } = Array.Empty<TicketSummaryModel>();

    /// <summary>
    /// <para>The one-based page this result covers, clamped to a page that exists.</para>
    /// </summary>
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    public int TotalPages => PageSize < 1
        ? 0
        : (TotalCount + PageSize - 1) / PageSize;
}
