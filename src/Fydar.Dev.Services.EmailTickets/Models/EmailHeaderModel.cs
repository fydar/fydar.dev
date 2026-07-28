using System;
using System.Collections.Generic;

namespace Fydar.Dev.Services.EmailTickets.Models;

public class EmailHeaderModel
{
    public string MessageId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public IList<string> From { get; set; } = Array.Empty<string>();
    public IList<string> To { get; set; } = Array.Empty<string>();
    public string Subject { get; set; } = string.Empty;
}
