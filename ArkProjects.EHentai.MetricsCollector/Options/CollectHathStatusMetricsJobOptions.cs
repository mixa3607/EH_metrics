namespace ArkProjects.EHentai.MetricsCollector.Options;

public class CollectHathStatusMetricsJobOptions
{
    public TimeSpan MetricLifetime { get; set; } = TimeSpan.MaxValue;
}