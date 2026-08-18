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

    /// <summary>
    /// <para>Moves a ticket out of the listing. The ticket is kept, not erased, so
    /// <see cref="RestoreEmailAsync"/> can bring it back.</para>
    /// </summary>
    public Task DeleteEmailAsync(
        string ticketId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>Reverses a <see cref="DeleteEmailAsync"/> for a ticket that hasn't been purged yet.</para>
    /// </summary>
    public Task RestoreEmailAsync(
        string ticketId,
        CancellationToken cancellationToken = default);
}
