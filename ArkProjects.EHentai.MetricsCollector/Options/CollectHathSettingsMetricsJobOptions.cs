namespace ArkProjects.EHentai.MetricsCollector.Options;

public class CollectHathSettingsMetricsJobOptions
{
    public long[] ClientIds { get; set; } = Array.Empty<long>();
    public TimeSpan MetricLifetime { get; set; } = TimeSpan.MaxValue;
}