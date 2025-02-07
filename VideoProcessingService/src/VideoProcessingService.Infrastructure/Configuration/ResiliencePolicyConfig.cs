namespace VideoProcessingService.Infrastructure.Configuration
{
    public class ResiliencePolicyConfig
    {
        public int RetryCount { get; set; } = 3;
        public int RetryBaseDelaySeconds { get; set; } = 2;
        public int CircuitBreakerThreshold { get; set; } = 5;
        public int CircuitBreakerDurationSeconds { get; set; } = 30;
    }
}
