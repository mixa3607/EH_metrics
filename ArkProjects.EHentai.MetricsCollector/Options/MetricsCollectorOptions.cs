namespace ArkProjects.EHentai.MetricsCollector.Options;

public class MetricsCollectorOptions
{
    public TimeSpan HathPerksMetricsLifetime { get; set; } = TimeSpan.MaxValue;
    public TimeSpan SettingsMetricsLifetime { get; set; } = TimeSpan.MaxValue;
    public TimeSpan HathStatusMetricsLifetime { get; set; } = TimeSpan.MaxValue;
    public TimeSpan HomeOverviewMetricsLifetime { get; set; } = TimeSpan.MaxValue;
}