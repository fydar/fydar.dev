using Fydar.Dev.Services.EmailTickets.Models;
using MimeKit;
using System.Threading;
using System.Threading.Tasks;

namespace Fydar.Dev.Services.EmailTickets;

public interface IEmailReaderService
{
    public Task<MimeMessage> ReadEmailAsync(
        string ticketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>Lists the stored tickets, newest first.</para>
    /// </summary>
    /// <param name="pageNumber">The one-based page to return.</param>
    /// <param name="pageSize">The number of tickets a page holds.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    public Task<TicketPageModel> ListEmailsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
