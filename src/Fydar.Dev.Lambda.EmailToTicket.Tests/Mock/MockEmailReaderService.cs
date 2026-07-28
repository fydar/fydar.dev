using Fydar.Dev.Services.EmailTickets;
using MimeKit;

namespace Fydar.Dev.Lambda.EmailToTicket.Tests.Mock;

public class MockEmailReaderService : IEmailReaderService
{
    public MockEmailReaderService()
    {
    }

    public async Task<MimeMessage> ReadEmailAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        return new MimeMessage();
    }
}
