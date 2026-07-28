using MimeKit;

namespace Fydar.Dev.Services.EmailTickets.Models;

public class EmailModel
{
    public EmailHeaderModel Header { get; set; }
    public MimeMessage Message { get; set; }

    public EmailModel(
        EmailHeaderModel header,
        MimeMessage message)
    {
        Header = header;
        Message = message;
    }
}
