namespace VideoProcessingService.Application.DTOs
{
    public class NotificationMessageDto
    {
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string AttachmentPath { get; set; }
        public bool IsProcessingUpdate { get; set; }
    }
}
