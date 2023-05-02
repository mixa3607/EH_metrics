namespace ArkProjects.EHentai.MetricsCollector.Quartz;

public class QuartzCronJobDefinition
{
    public bool Enable { get; set; }
    public bool TriggerOnStartup { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Group { get; set; }
    public string? JobType { get; set; }
    public string? CronExpression { get; set; }
    public bool ConcurrentExecutionDisallowed { get; set; }
    public Dictionary<string, string?> JobData { get; set; } = new();
}
