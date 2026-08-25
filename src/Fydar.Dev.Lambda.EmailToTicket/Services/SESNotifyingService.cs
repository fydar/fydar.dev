using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Fydar.Dev.Services.EmailTickets.Models;
using MimeKit;
using System.IO;
using System.Threading.Tasks;

namespace Fydar.Dev.Lambda.EmailToTicket.Services;

public class SESNotifyingService : IEmailSinkService
{
    private readonly IAmazonSimpleEmailService amazonSimpleEmailService;
    private readonly string destination;

    public SESNotifyingService(
        IAmazonSimpleEmailService amazonSimpleEmailService,
        string destination)
    {
        this.amazonSimpleEmailService = amazonSimpleEmailService;
        this.destination = destination;
    }

    public async Task<bool> ForwardEmailAsync(
        EmailModel email)
    {
        var body = new BodyBuilder
        {
            TextBody = $"You have new unread messages from a user using your contact email address.\n" +
                $"To view the message; use the administrator contact panel.\n" +
                $"Your new message can be found here. https://fydar.dev/ticket/{email.Header.MessageId}"
        };

        var notificationMessage = new MimeMessage
        (
            from: [new MailboxAddress("Fydar Avery", "contact@fydar.dev")],
            to: [new MailboxAddress("Fydar Avery", destination)],
            subject: "You have new unread messages",
            body: body.ToMessageBody()
        );

        using var stream = new MemoryStream();
        await notificationMessage.WriteToAsync(stream);

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
