using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Fydar.Dev.Services.EmailTickets.Models;
using MimeKit;
using System.IO;
using System.Threading.Tasks;

namespace Fydar.Dev.Lambda.EmailToTicket.Services;

public class SESEmailForwardingService : IEmailSinkService
{
    private readonly IAmazonSimpleEmailService amazonSimpleEmailService;
    private readonly string destination;

    public SESEmailForwardingService(
        IAmazonSimpleEmailService amazonSimpleEmailService,
        string destination)
    {
        this.amazonSimpleEmailService = amazonSimpleEmailService;
        this.destination = destination;
    }

    public async Task<bool> ForwardEmailAsync(
        EmailModel email)
    {
        var forwardedMessage = new MimeMessage
        (
            from: [new MailboxAddress("Fydar Avery", "contact@fydar.dev")],
            to: [new MailboxAddress("Fydar Avery", destination)],
            subject: email.Message.Subject,
            body: email.Message.Body
        );

        using var stream = new MemoryStream();
        await forwardedMessage.WriteToAsync(stream);

        var request = new SendRawEmailRequest()
        {
            Source = "contact@fydar.dev",
            RawMessage = new RawMessage(stream),
            Destinations =
            [
                destination
            ]
        };

        var response = await amazonSimpleEmailService.SendRawEmailAsync(request);

        return true;
    }
}
