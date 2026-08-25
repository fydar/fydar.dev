using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SimpleEmailEvents;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using Amazon.S3;
using Amazon.SimpleEmail;
using Fydar.Dev.Lambda.EmailToTicket.Services;
using Fydar.Dev.Services.EmailTickets;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Fydar.Dev.Lambda.EmailToTicket;

public static class Program
{
    /// <summary>
    /// The main entry point for the Lambda function. The main function is called once during the Lambda init phase. It
    /// initializes the .NET Lambda runtime client passing in the function handler to invoke for each Lambda event and
    /// the JSON serializer to use for converting Lambda JSON format to the .NET types. 
    /// </summary>
    private static async Task Main()
    {
        string? emailBuckt = Environment.GetEnvironmentVariable("CONFIG_EMAILBUCKET");
        string? forwardTo = Environment.GetEnvironmentVariable("CONFIG_FORWARDTO");

        if (emailBuckt == null)
        {
            throw new InvalidOperationException("Failed to create function as email bucket was not defined.");
        }
        if (forwardTo == null)
        {
            throw new InvalidOperationException("Failed to create function as forward to email was not defined.");
        }

        var amazonS3 = new AmazonS3Client();
        var amazonSimpleEmail = new AmazonSimpleEmailServiceClient();

        var emailReaderServiceOptions = Options.Create(new S3EmailReaderServiceConfiguration()
        {
            Bucket = emailBuckt
        });

        var emailReaderService = new S3EmailReaderService(amazonS3, emailReaderServiceOptions);
        var emailSinkService = new SESNotifyingService(amazonSimpleEmail, forwardTo);

        var function = new FunctionService(emailReaderService, emailSinkService);

        Func<SimpleEmailEvent<LambdaReceiptAction>, ILambdaContext, Task<string>> handler = function.FunctionHandler;

        var lambdaBootstrapBuilder = LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<ApplicationJsonSerializerContext>());
        var lambdaBootstrap = lambdaBootstrapBuilder.Build();

        await lambdaBootstrap.RunAsync();
    }
}
