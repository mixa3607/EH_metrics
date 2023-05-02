using ArkProjects.EHentai.MetricsCollector.Services;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Jobs;

[DisallowConcurrentExecution]
public class CollectHathPerksMetricsJob : IJob
{
    private readonly ILogger<CollectHathPerksMetricsJob> _logger;
    private readonly MetricsCollectorService _metricsCollector;
    private readonly EHentaiClientDi _client;

    public CollectHathPerksMetricsJob(EHentaiClientDi client, ILogger<CollectHathPerksMetricsJob> logger,
        MetricsCollectorService metricsCollector)
    {
        _client = client;
        _logger = logger;
        _metricsCollector = metricsCollector;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Begin collect hath perks metrics");
        var resp = await _client.MyHome.GetHathPerksAsync(context.CancellationToken);
        _logger.LogDebug("EH raw response: page {page}, html: {html}", "MyHome.HathPerks", resp.RawStringBody);
        _metricsCollector.SetHathPerks(resp.Body!);
    }
}