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
        private static string awsAccessKeyId = "AKIA54SNDJKIJ5AGIBWQ";
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
