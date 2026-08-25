using Fydar.Dev.Services.EmailTickets;
using Fydar.Dev.Services.EmailTickets.Models;
using MimeKit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

    public async Task<TicketPageModel> ListEmailsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        return new TicketPageModel()
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<string>> DeleteEmailsAsync(
        IReadOnlyCollection<string> ticketIds,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        return [.. ticketIds];
    }

    public async Task<IReadOnlyList<string>> RestoreEmailsAsync(
        IReadOnlyCollection<string> ticketIds,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        return [.. ticketIds];
    }
}
