using ArkProjects.EHentai.MetricsCollector.Services;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Jobs;

[DisallowConcurrentExecution]
public class CollectHomeOverviewMetricsJob : IJob
{
    private readonly ILogger<CollectHomeOverviewMetricsJob> _logger;
    private readonly MetricsCollectorService _metricsCollector;
    private readonly EHentaiClientDi _client;

    public CollectHomeOverviewMetricsJob(EHentaiClientDi client, ILogger<CollectHomeOverviewMetricsJob> logger,
        MetricsCollectorService metricsCollector)
    {
        _client = client;
        _logger = logger;
        _metricsCollector = metricsCollector;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Begin collect home overview metrics");
        var resp = await _client.MyHome.GetOverviewAsync(context.CancellationToken);
        _logger.LogDebug("EH raw response: page {page}, html: {html}", "MyHome.Overview", resp.RawStringBody);
        _metricsCollector.SetOverview(resp.Body!);
    }
}