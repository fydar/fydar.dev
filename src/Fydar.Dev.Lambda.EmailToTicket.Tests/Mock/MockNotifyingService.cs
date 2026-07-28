using Fydar.Dev.Lambda.EmailToTicket.Services;
using Fydar.Dev.Services.EmailTickets.Models;

namespace Fydar.Dev.Lambda.EmailToTicket.Tests.Mock;

public class MockNotifyingService : IEmailSinkService
{
    private readonly List<EmailModel> emails = [];

    public IReadOnlyList<EmailModel> Emails => emails;

    public async Task<bool> ForwardEmailAsync(
        EmailModel email)
    {
        emails.Add(email);
        await Task.Delay(10);
        return true;
    }
}
