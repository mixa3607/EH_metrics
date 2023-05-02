using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Quartz;

public static class QuartzServiceCollectionExtensions
{
    public static IServiceCollectionQuartzConfigurator AddCronJob(this IServiceCollectionQuartzConfigurator quartzConfigurator, QuartzCronJobDefinition jobDefinition)
    {
        if (!jobDefinition.Enable)
            return quartzConfigurator;

        if (string.IsNullOrWhiteSpace(jobDefinition.Name))
            throw new Exception("Job name must be set");
        if (string.IsNullOrWhiteSpace(jobDefinition.CronExpression))
            throw new Exception("Cron expression must be set");
        if (jobDefinition.JobType == null)
            throw new Exception("Job type must be set");

        var jobKey = new JobKey(jobDefinition.Name, jobDefinition.Group);

        var jobType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(jobDefinition.JobType))
            .FirstOrDefault(tt => tt != null)!;
        jobType ??= AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.DefinedTypes)
            .First(tt => tt.Name == jobDefinition.JobType)!;

        quartzConfigurator.AddJob(jobType, jobKey, c=>c
            .WithDescription(jobDefinition.Description)
            .DisallowConcurrentExecution(jobDefinition.ConcurrentExecutionDisallowed)
            .SetJobData(new JobDataMap(jobDefinition.JobData))
        );
        quartzConfigurator.AddTrigger(c => c
            .ForJob(jobKey)
            .WithCronSchedule(jobDefinition.CronExpression, y => y.InTimeZone(TimeZoneInfo.Utc))
        );
        return quartzConfigurator;
    }
}