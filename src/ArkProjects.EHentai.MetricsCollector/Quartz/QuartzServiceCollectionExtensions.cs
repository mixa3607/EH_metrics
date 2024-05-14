using ArkProjects.EHentai.MetricsCollector.Options;
using Microsoft.Extensions.Options;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Quartz;

public class QuartzJobsInitializerService
{
    private readonly ISchedulerFactory _factory;
    private readonly AppQuartzOptions _options;

    public QuartzJobsInitializerService(ISchedulerFactory factory, IOptions<AppQuartzOptions> options)
    {
        _factory = factory;
        _options = options.Value;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var scheduler = await _factory.GetScheduler(ct);

        foreach (var (name, def) in _options.Jobs)
        {
            await AddCronJobAsync(def, name, scheduler, ct);
        }
    }

    public async Task<IScheduler> AddCronJobAsync(QuartzCronJobDefinition jobDefinition, string name, IScheduler scheduler,
        CancellationToken ct = default)
    {
        if (!jobDefinition.Enable)
            return scheduler;

        if (string.IsNullOrWhiteSpace(name))
            throw new Exception("Job name must be set");
        if (string.IsNullOrWhiteSpace(jobDefinition.CronExpression))
            throw new Exception("Cron expression must be set");
        if (jobDefinition.JobType == null)
            throw new Exception("Job type must be set");
        

        var jobType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(jobDefinition.JobType))
            .FirstOrDefault(tt => tt != null)!;
        jobType ??= AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.DefinedTypes)
            .First(tt => tt.Name == jobDefinition.JobType)!;

        var job = JobBuilder.Create()
            .WithIdentity(name, jobDefinition.Group ?? "DEFAULT")
            .OfType(jobType)
            .WithDescription(jobDefinition.Description)
            .DisallowConcurrentExecution(jobDefinition.ConcurrentExecutionDisallowed)
            .SetJobData(new JobDataMap(jobDefinition.JobData))
            .Build();
        var trigger = TriggerBuilder.Create()
            .ForJob(job.Key)
            .WithCronSchedule(jobDefinition.CronExpression, y => y.InTimeZone(TimeZoneInfo.Utc))
            .Build();
        await scheduler.ScheduleJob(job, trigger, ct);
        if (jobDefinition.TriggerOnStartup)
            await scheduler.TriggerJob(job.Key, ct);

        return scheduler;
    }
}