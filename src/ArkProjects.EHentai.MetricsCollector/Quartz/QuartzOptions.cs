using ArkProjects.EHentai.MetricsCollector.Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Options;

public class AppQuartzOptions
{
    public Dictionary<string, QuartzCronJobDefinition> Jobs { get; set; } = new();
}