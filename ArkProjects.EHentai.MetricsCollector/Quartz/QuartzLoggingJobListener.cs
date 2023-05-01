using ArkProjects.EHentai.MetricsCollector.Services;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Quartz;

public class QuartzLoggingJobListener : IJobListener
{
    private readonly MetricsCollectorService _metricsCollector;

    public string Name => "JobLogging";

    public QuartzLoggingJobListener(MetricsCollectorService metricsCollector)
    {
        _metricsCollector = metricsCollector;
    }


    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken ct = new())
    {
        return Task.CompletedTask;
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken ct = new())
    {
        return Task.CompletedTask;
    }

    public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException,
        CancellationToken ct = new())
    {
        _metricsCollector.SetJobTime(context, jobException);
        return Task.CompletedTask;
    }
}