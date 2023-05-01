using ArkProjects.EHentai.MetricsCollector.Options;
using ArkProjects.EHentai.MetricsCollector.Services;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Jobs;

[DisallowConcurrentExecution]
public class CollectHathPerksMetricsJob : IJob
{
    private readonly ILogger<CollectHathPerksMetricsJob> _logger;
    private readonly MetricsCollectorService _metricsCollector;
    private readonly EHentaiClientDi _client;
    private readonly CollectHathSettingsMetricsJobOptions _options;

    public CollectHathPerksMetricsJob(EHentaiClientDi client, ILogger<CollectHathPerksMetricsJob> logger,
        IConfiguration cfg, MetricsCollectorService metricsCollector)
    {
        _client = client;
        _logger = logger;
        _metricsCollector = metricsCollector;
        cfg.GetOptionsReflex(out _options);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Begin collect hath perks metrics");
        var resp = await _client.MyHome.GetHathPerksAsync(context.CancellationToken);
        _logger.LogDebug("EH raw response: page {page}, html: {html}", "MyHome.HathPerks", resp.Body);
        _metricsCollector.SetHathPerks(resp.Body!);
    }
}