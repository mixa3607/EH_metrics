namespace ArkProjects.EHentai.MetricsCollector.Quartz;

public class AppQuartzOptions
{
    public Dictionary<string, QuartzCronJobDefinition> Jobs { get; set; } = new();
}