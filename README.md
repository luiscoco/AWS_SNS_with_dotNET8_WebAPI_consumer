# How to create a .NET8 WebAPI for receiving messages from AWS SQS subscribed to SNS(Topic)

See the source code for this sample in this github repo: https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer

## 1. Create AWS SQS subscribed to SNS(topic)

We navigate to SQS service in AWS Console

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/a992bf86-adc7-4b91-a39c-381eeaa09913)

And we create a new SQS

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/7a1f863b-6bdd-45c9-9db1-91f10d761495)

We select the Standard queue and the queue name

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/7fe1b599-8a0d-4137-a9a5-68aa378ccbcc)

For this example we leave the rest of the default values and press the Create queue button

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/03c1fb15-6ce9-4fff-8209-a06e85ac8941)

We get this confirmation message after the queue was created

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/1fff7db5-2eb3-46c1-9d66-d5c4414b016a)

Now we press the **Subscribe to Amazon SNS Topic** button and we select the topic 

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/54e82b98-a735-473c-9e0e-f5039c93359d)

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/e549c39e-f555-48e9-916d-c2e9dc566b3b)

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/93035b82-f8c1-44fa-ae83-1bb6d13aca20)

## 2. Create a .NET8 WebAPI with VSCode

Creating a .NET 8 Web API using Visual Studio Code (VSCode) and the .NET CLI is a straightforward process

This guide assumes you have .NET 8 SDK, VSCode, and the C# extension for VSCode installed. If not, you'll need to install these first

**Step 1: Install .NET 8 SDK**

Ensure you have the .NET 8 SDK installed on your machine: https://dotnet.microsoft.com/es-es/download/dotnet/8.0

You can check your installed .NET versions by opening a terminal and running:

```
dotnet --list-sdks
```

If you don't have .NET 8 SDK installed, download and install it from the official .NET download page

**Step 2: Create a New Web API Project**

Open a terminal or command prompt

Navigate to the directory where you want to create your new project

Run the following command to create a new Web API project:

```
dotnet new webapi -n SNSReceiverApi
```

This command creates a new directory with the project name, sets up a basic Web API project structure, and restores any necessary packages

**Step 3: Open the Project in VSCode**

Once the project is created, you can open it in VSCode by navigating into the project directory and running:

```
code .
```

This command opens VSCode in the current directory, where . represents the current directory

## 3. Load project dependencies

We run this command to add the Azure Service Bus library

```
dotnet add package AWSSDK.SimpleNotificationService
```

and 

```
dotnet add package AWSSDK.SQS
```

We also have to add the Swagger and OpenAPI libraries to access the API Docs

This is the **csproj** file including the project dependencies

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/d4078842-d75c-43fb-b95e-aebdd6c6dc7d)

## 4. Create the project structure

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/414b4612-f636-4136-abed-e586778370a7)

## 5. Create the Controller

**ReceiverController.cs**

```csharp
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceBusReceiverApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SnsSqsController : ControllerBase
    {
        private static string awsAccessKeyId = "AKIA54SNDJKIETHXVI6S";
        private static string awsSecretAccessKey = "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";
        private static string sqsQueueUrl = "https://sqs.eu-west-3.amazonaws.com/954718177936/myqueue";

        private static AmazonSQSClient sqsClient = new AmazonSQSClient(awsAccessKeyId, awsSecretAccessKey, Amazon.RegionEndpoint.EUWest3);
        private static ConcurrentQueue<MessageDto> receivedMessages = new ConcurrentQueue<MessageDto>();

        [HttpGet("receive")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> ReceiveMessages(string? priorityFilter = null)
        {
            var receiveMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = sqsQueueUrl,
                MaxNumberOfMessages = 10 // Adjust based on your needs
            };

            var response = await sqsClient.ReceiveMessageAsync(receiveMessageRequest);
            foreach (var message in response.Messages)
            {
                string body = message.Body;
                // Assuming 'priority' is a property or data within the message. Rename this if it conflicts.
                string? messagePriority = priorityFilter; // Adjust this logic based on your message structure

                Console.WriteLine($"Received message: {body}, Priority: {messagePriority}");
                receivedMessages.Enqueue(new MessageDto { Body = body, Priority = messagePriority });

                // Optionally, delete the message from the queue if it's successfully processed
                await sqsClient.DeleteMessageAsync(sqsQueueUrl, message.ReceiptHandle);
            }

            if (string.IsNullOrEmpty(priorityFilter))
            {
                return receivedMessages.ToList();
            }
            else
            {
                return receivedMessages.Where(m => m.Priority == priorityFilter).ToList();
            }
        }

    }

    public class MessageDto
    {
        public string? Body { get; set; }
        public string? Priority { get; set; }
    }
}
```

## 6. Modify the application middleware(program.cs)

**Program.cs**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.OpenApi.Models;
using ServiceBusReceiverApi.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ServiceBusReceiverApi", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ServiceBusReceiverApi v1");
});

app.UseAuthorization();

app.MapControllers();

app.Run();
```

## 7. Run and Test the application

We execute this command to run the application

```
dotnet run
```

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/141e00ea-2e46-4bba-95cf-10a35a3121f8)

We navigate to the application endpoint: http://localhost:5031/swagger/index.html

```
curl -X 'GET' \
  'http://localhost:5051/api/SnsSqs/receive?priorityFilter=high' \
  -H 'accept: text/plain'
```

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/7bc21328-4edc-485b-ac96-3c3bff72c3c9)

After executing the above request we get this response

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/f01d1738-765c-4fc4-a8a2-ccb51081bc0f)

## 8. Creating a Hosted Service for continously processing the messages

In the context of AWS SNS (Simple Notification Service) and SQS (Simple Queue Service), there's no direct "start message processing" mechanism similar to what you might use with Azure Service Bus,

where a background process continuously pulls messages from a queue or subscription

Instead, message consumption from an SQS queue is typically done by polling the queue to retrieve messages

However, you can simulate a continuous processing mechanism in your application by creating a background service in your .NET application that polls the SQS queue for messages at regular intervals

This can be achieved using .NET's IHostedService interface, which allows you to run background tasks in a web application

We can create a new file **SqsMessageProcessor.cs** for defining the messages HostedService processor

**SqsMessageProcessor.cs**

```csharp
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
```

We also have to modify the application middleware for registering the background service

**Program.cs**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.OpenApi.Models;
using ServiceBusReceiverApi.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHostedService<SqsMessageProcessor>(); // Register the background service

// Add Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ServiceBusReceiverApi", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ServiceBusReceiverApi v1");
});

app.UseAuthorization();

app.MapControllers();

app.Run();
```

This is the final project structure

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/585654be-3967-48e5-b61d-935b9d6bc43e)

When we test the application we verify the output for the background hostedservice

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/030bca0a-fee4-408f-a843-90aece759056)

**IMPORTANT NOTE**: for running the HostedService please configure the AWS CLI running this command, and set the **aws_access_key_id** and the **aws_secret_access_key**

```
aws configure
```

**IMPORTANT NOTE**: also is very important to set the **Visibility timeout** to 1 second

![image](https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer/assets/32194879/83401435-0f8c-4881-b637-83433bb5db86)

**Visibility timeout**: when a message is received, SQS temporarily hides it from subsequent retrieve requests for a duration known as the visibility timeout

If you're not deleting messages after processing, ensure the visibility timeout is appropriately set for your use case to prevent immediate reprocessing by other consumers
