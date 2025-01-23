namespace VideoProcessorX.Domain.Entities
{
    public class Video
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string OriginalFileName { get; set; }  
        public string FilePath { get; set; }          
        public string Status { get; set; }           
        public string ZipPath { get; set; }          

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }
}
