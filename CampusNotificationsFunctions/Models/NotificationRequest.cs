using System;
using System.Collections.Generic;
using System.Text;

namespace CampusNotificationsFunctions.Models
{
    public class NotificationRequest
    {
        public string StudentNumber { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public string Message {  get; set; } = string.Empty;
    }
}
