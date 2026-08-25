using Amazon.Lambda.Core;
using Amazon.Lambda.SimpleEmailEvents;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using Fydar.Dev.Lambda.EmailToTicket.Services;
using Fydar.Dev.Services.EmailTickets;
using Fydar.Dev.Services.EmailTickets.Models;
using System;
using System.Threading.Tasks;

namespace Fydar.Dev.Lambda.EmailToTicket;

public class FunctionService
{
    private readonly IEmailReaderService emailReaderService;
    private readonly IEmailSinkService emailSinkService;

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public FunctionService(
        IEmailReaderService emailReaderService,
        IEmailSinkService emailSinkService)
    {
        this.emailReaderService = emailReaderService;
        this.emailSinkService = emailSinkService;
    }

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="sesEvent">The lambda event to process.</param>
    /// <param name="context">Context for the execution of this lambda function.</param>
    /// <returns></returns>
    public async Task<string> FunctionHandler(
        SimpleEmailEvent<LambdaReceiptAction> sesEvent,
        ILambdaContext context)
    {
        foreach (var record in sesEvent.Records)
        {
            foreach (string from in record.Ses.Mail.CommonHeaders.From)
            {
                if (from.EndsWith("amazonses.com", StringComparison.OrdinalIgnoreCase))
                {
                    context.Logger.LogLine("Email was from amazonses.com, ignoring it.");
                    continue;
                }
            }

            var emailHeader = new EmailHeaderModel()
            {
                MessageId = record.Ses.Mail.MessageId,
                Timestamp = record.Ses.Mail.Timestamp,
                From = record.Ses.Mail.CommonHeaders.From,
                To = record.Ses.Mail.CommonHeaders.To,
                Subject = record.Ses.Mail.CommonHeaders.Subject
            };

            var mimeMessage = await emailReaderService.ReadEmailAsync(emailHeader.MessageId);

            await emailSinkService.ForwardEmailAsync(new EmailModel(emailHeader, mimeMessage));
        }

        return "CONTINUE";
    }
}
