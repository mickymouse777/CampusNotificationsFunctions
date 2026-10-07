using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using CampusNotificationsFunctions.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Azure;

namespace CampusNotificationsFunctions;

public class ProcessNotification
{
    private const string TableName = "Notifications";
    private readonly TableClient _tableClient;
    private readonly ILogger _logger;
    public ProcessNotification(ILogger<ProcessNotification> logger, IConfiguration configuration)
    {
        _logger = logger;
        var connectionString = configuration["AzureWebJobsStorage"];
        _tableClient = new TableClient(connectionString, TableName);
    }
    [Function("ProcessNotification")]
    public async Task Run([QueueTrigger("campus-notifications", Connection="AzureWebJobsStorage")] string queueMessage)
    {
        NotificationMessage? notification;

        try
        {
            notification = JsonSerializer.Deserialize<NotificationMessage>(queueMessage);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Queue message Json is cooked");
            throw;
        }
        if (notification is null ||
            string.IsNullOrWhiteSpace(notification.NotificationID) ||
            string.IsNullOrWhiteSpace(notification.StudentNumber) ||
            string.IsNullOrWhiteSpace(notification.NotificationType) ||
            string.IsNullOrWhiteSpace(notification.Message) ||
            notification.SubmissionDate == default
            )
        {
            throw new InvalidOperationException("Queue Message is missing some fields");
        }

        await _tableClient.CreateIfNotExistsAsync();

        var entity = new TableEntity
        {
            PartitionKey = "Notifications",
            RowKey = notification.NotificationID,
            ["StudentNumber"] = notification.StudentNumber,
            ["Message"] = notification.Message,
            ["SubmissionDate"] = notification.SubmissionDate,
            Timestamp = DateTimeOffset.UtcNow
        };
        try
        {
            await _tableClient.AddEntityAsync(entity);
            _logger.LogInformation("Notification {NotificationID} saved to {TableName}", notification.NotificationID, TableName);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            _logger.LogWarning($"notification {notification.NotificationID} already exists");
        }
    }
}