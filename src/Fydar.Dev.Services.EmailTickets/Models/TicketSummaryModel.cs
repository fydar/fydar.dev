using System;

namespace Fydar.Dev.Services.EmailTickets.Models;

public class TicketSummaryModel
{
    public string TicketId { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public long Size { get; set; }
}
