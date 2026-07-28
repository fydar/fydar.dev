using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Fydar.Dev.WebApp.Components.Email;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MimeKit;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace Fydar.Dev.WebApp;

public class ContactSubmitRequestModel
{
    [Required]
    public string RequestId { get; set; } = string.Empty;

    [DisplayName("Email")]
    [Required(AllowEmptyStrings = false, ErrorMessage = "An email is required.")]
    [EmailAddress(ErrorMessage = "The email must be in a valid format.")]
    [DataType(DataType.EmailAddress, ErrorMessage = "The email must be in a valid format.")]
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// <para>This is a honeypot. Users attempting to submit a form with this data will be detected as bots.</para>
    /// </summary>
    [DisplayName("ConfirmEmail")]
    public string ConfirmUserEmail { get; set; } = string.Empty;

    [DisplayName("Subject")]
    [Required(ErrorMessage = "A subject is required.")]
    [MinLength(2, ErrorMessage = "The subject is too short.")]
    [DataType(DataType.Text)]
    public string UserSubject { get; set; } = string.Empty;

    [DisplayName("Body")]
    [Required(ErrorMessage = "A body is required.")]
    [MinLength(10, ErrorMessage = "The body is too short.")]
    [DataType(DataType.MultilineText)]
    public string UserBody { get; set; } = string.Empty;
}

public interface IContactSubmitSink
{
    public Task ProcessSubmitAsync(ContactSubmitModel contactSubmit);
}

public class ContactNotificationSubmitSink : IContactSubmitSink
{
    private readonly ILogger<ContactNotificationSubmitSink> logger;
    private readonly IAmazonSimpleEmailService simpleEmailService;
    private readonly HtmlRenderer htmlRenderer;

    public ContactNotificationSubmitSink(
        ILogger<ContactNotificationSubmitSink> logger,
        IAmazonSimpleEmailService simpleEmailService,
        HtmlRenderer htmlRenderer)
    {
        this.logger = logger;
        this.simpleEmailService = simpleEmailService;
        this.htmlRenderer = htmlRenderer;
    }

    public async Task ProcessSubmitAsync(ContactSubmitModel contactSubmit)
    {
        string htmlBody = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var renderedComponent = await htmlRenderer.RenderComponentAsync<BasicEmail>(ParameterView.FromDictionary(new Dictionary<string, object?>()
            {
                ["Model"] = contactSubmit
            }));
            return renderedComponent.ToHtmlString();
        });

        var request = new SendEmailRequest()
        {
            Source = "Fydar <contact@fydar.dev>",
            Destination = new Destination()
            {
                ToAddresses =
                [
                    "dev.anthonymarmont@gmail.com"
                ]
            },
            Message = new Message()
            {
                Subject = new Content($"You have new unread messages"),
                Body = new Body()
                {
                    Html = new Content(htmlBody)
                }
            },
        };
        await simpleEmailService.SendEmailAsync(request);
    }
}

public class SaveTicketSubmitSink : IContactSubmitSink
{
    private readonly ILogger<SaveTicketSubmitSink> logger;
    private readonly IAmazonS3 amazonS3;

    public SaveTicketSubmitSink(
        ILogger<SaveTicketSubmitSink> logger,
        IAmazonS3 amazonS3)
    {
        this.logger = logger;
        this.amazonS3 = amazonS3;
    }

    public async Task ProcessSubmitAsync(ContactSubmitModel contactSubmit)
    {
        var message = new MailMessage(new MailAddress(contactSubmit.UserEmail), new MailAddress($"form-{contactSubmit.FormName}@fydar.dev"))
        {
            Body = contactSubmit.UserBody
        };

        var mimeMessage = MimeMessage.CreateFromMailMessage(message);

        string emailString;
        using (var ms = new MemoryStream())
        {
            await mimeMessage.WriteToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            using var streamReader = new StreamReader(ms);
            emailString = streamReader.ReadToEnd();
        }

        var request = new PutObjectRequest()
        {
            BucketName = "fydar.dev-inbound-email",
            Key = contactSubmit.TicketId,
            ContentBody = emailString
        };

        _ = await amazonS3.PutObjectAsync(request);
    }
}
