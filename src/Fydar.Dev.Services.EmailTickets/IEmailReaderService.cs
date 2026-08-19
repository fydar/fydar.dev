using Fydar.Dev.Services.EmailTickets.Models;
using MimeKit;
using System.Collections.Generic;
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
    /// <para>Moves the given tickets out of the listing. Tickets are kept, not erased, so
    /// <see cref="RestoreEmailsAsync"/> can bring them back.</para>
    /// </summary>
    /// <returns>The ids that were actually moved. A ticket can be missing from this list if it
    /// had already been removed, or if the underlying storage rejected that one entry.</returns>
    public Task<IReadOnlyList<string>> DeleteEmailsAsync(
        IReadOnlyCollection<string> ticketIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>Reverses a <see cref="DeleteEmailsAsync"/> for tickets that haven't been purged yet.</para>
    /// </summary>
    /// <returns>The ids that were actually restored.</returns>
    public Task<IReadOnlyList<string>> RestoreEmailsAsync(
        IReadOnlyCollection<string> ticketIds,
        CancellationToken cancellationToken = default);
}
