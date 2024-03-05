using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

public class SqsMessageProcessor : IHostedService, IDisposable
{
    private Timer? _timer; // Make timer nullable
    private readonly AmazonSQSClient _sqsClient;
    private readonly string _queueUrl = "https://sqs.eu-west-3.amazonaws.com/954718177936/myqueue";

    public SqsMessageProcessor()
    {
        Console.WriteLine("Initializing AmazonSQSClient with Region: EUWest3");
        try
        {
            _sqsClient = new AmazonSQSClient(Amazon.RegionEndpoint.EUWest3);
            Console.WriteLine("AmazonSQSClient initialization successful.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing AmazonSQSClient: {ex.Message}");
            throw;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Poll every 1 second (adjust as necessary)
        _timer = new Timer(async _ => await DoWork(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        return Task.CompletedTask;
    }

    private async Task DoWork()
    {
        var receiveMessageRequest = new ReceiveMessageRequest
        {
            QueueUrl = _queueUrl,
            MaxNumberOfMessages = 10 // Adjust based on your needs
        };

        try
        {
            var response = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest);
            foreach (var message in response.Messages)
            {
                Console.WriteLine($"Received message: {message.Body}");

                // var deleteMessageRequest = new DeleteMessageRequest
                // {
                //     QueueUrl = _queueUrl,
                //     ReceiptHandle = message.ReceiptHandle
                // };
                // await _sqsClient.DeleteMessageAsync(deleteMessageRequest);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            // Handle exception (e.g., log the error)
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
