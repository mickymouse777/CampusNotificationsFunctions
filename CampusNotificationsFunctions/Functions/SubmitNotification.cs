using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Azure.Storage.Queues;
//using Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using CampusNotificationsFunctions.Models;
using System.Net;

namespace CampusNotificationsFunctions.Functions;

public class SubmitNotification
{
    private readonly ILogger<SubmitNotification> _logger;
    private const string QueueName = "campus-notifications";
    private readonly QueueClient _queueClient;

    public SubmitNotification(ILogger<SubmitNotification> logger, IConfiguration configuration)
    {
        _logger = logger;
        var connectionString = configuration["AzureWebJobsStorage"];
        _queueClient = new QueueClient(connectionString, QueueName);
    }

    [Function("SubmitNotification")]
    public async Task<HttpResponseData> Run ([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "notifications")] HttpRequestData req)
    {
        var notificationRequest = await JsonSerializer.DeserializeAsync<NotificationRequest>
            (req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (notificationRequest == null)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid Notification Request");
            return badResponse;
        }

        var notification = new NotificationMessage
        {
            NotificationID = Guid.NewGuid().ToString(),
            StudentNumber = notificationRequest.StudentNumber,
            NotificationType = notificationRequest.NotificationType,
            Message = notificationRequest.Message,
            SubmissionDate = DateTimeOffset.UtcNow
        };

        var messageJson = JsonSerializer.Serialize(notification);
        string base64String = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(messageJson));
        await _queueClient.CreateIfNotExistsAsync();
        await _queueClient.SendMessageAsync(messageJson);


      _logger.LogInformation($"Notification received for student {notificationRequest.StudentNumber}", notificationRequest.StudentNumber);

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteStringAsync($"Notification {notification.NotificationID} queued");
        return response;
    }
}