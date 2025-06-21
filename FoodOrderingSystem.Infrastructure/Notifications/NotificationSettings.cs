namespace FoodOrderingSystem.Infrastructure.Notifications
{
    public class NotificationSettings
    {
        //public string WhatsAppApiKey { get; set; } = string.Empty;
        //public string WhatsAppApiUrl { get; set; } = string.Empty;
        //public string WhatsAppSenderId { get; set; } = string.Empty;
        public string EmailSmtpServer { get; set; } = string.Empty;
        public int EmailSmtpPort { get; set; }
        public string EmailUsername { get; set; } = string.Empty;
        public string EmailPassword { get; set; } = string.Empty;
        public string EmailSenderAddress { get; set; } = string.Empty;
    }
}