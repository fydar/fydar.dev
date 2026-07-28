using MimeKit;
using System.Threading;
using System.Threading.Tasks;

namespace Fydar.Dev.Services.EmailTickets;

public interface IEmailReaderService
{
    public Task<MimeMessage> ReadEmailAsync(
        string ticketId,
        CancellationToken cancellationToken = default);
}
