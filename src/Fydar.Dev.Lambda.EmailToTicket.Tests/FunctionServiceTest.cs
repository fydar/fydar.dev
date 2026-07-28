using Amazon.Lambda.SimpleEmailEvents;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using Amazon.Lambda.TestUtilities;
using Fydar.Dev.Lambda.EmailToTicket.Tests.Mock;
using Xunit;

namespace Fydar.Dev.Lambda.EmailToTicket.Tests;

public class FunctionServiceTest
{
    [Fact]
    public void TestSQSEventLambdaFunction()
    {
        var logger = new TestLambdaLogger();
        var context = new TestLambdaContext
        {
            Logger = logger
        };
        var sesEvent = new SimpleEmailEvent<LambdaReceiptAction>()
        {
            Records = []
        };

        var mockEmailReaderService = new MockEmailReaderService();
        var mockNotifyingService = new MockNotifyingService();

        // var functionService = new FunctionService(mockEmailReaderService, mockNotifyingService);
        // 
        // // Act
        // string result = await functionService.FunctionHandler(sesEvent, context);
        // 
        // // Assert
        // Assert.Contains("CONTINUE", result);
    }
}
