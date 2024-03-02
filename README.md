# How to create a .NET8 WebAPI for receiving messages from AWS SQS subscribed to SNS(Topic)

See the source code for this sample in this github repo: https://github.com/luiscoco/AWS_SNS_with_dotNET8_WebAPI_consumer

## 1. Create AWS SQS subscribed to SNS(topic)



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
dotnet new webapi -n SNSSenderApi
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


