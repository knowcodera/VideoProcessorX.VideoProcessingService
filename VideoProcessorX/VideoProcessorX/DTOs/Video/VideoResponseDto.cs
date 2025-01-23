namespace VideoProcessorX.WebApi.DTOs.Video
{
    public class VideoResponseDto
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; }
        public string Status { get; set; }
        public string ZipPath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
