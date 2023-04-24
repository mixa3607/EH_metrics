namespace ArkProjects.EHentai.MetricsCollector.Options;

public class CollectHomeOverviewMetricsJobOptions
{
    public TimeSpan MetricLifetime { get; set; } = TimeSpan.MaxValue;
}