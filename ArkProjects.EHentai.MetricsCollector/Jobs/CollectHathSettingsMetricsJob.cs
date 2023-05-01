using ArkProjects.EHentai.MetricsCollector.Options;
using ArkProjects.EHentai.MetricsCollector.Services;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Jobs;

[DisallowConcurrentExecution]
public class CollectHathSettingsMetricsJob : IJob
{
    private readonly ILogger<CollectHathSettingsMetricsJob> _logger;
    private readonly MetricsCollectorService _metricsCollector;
    private readonly EHentaiClientDi _client;
    private readonly CollectHathSettingsMetricsJobOptions _options;

    public CollectHathSettingsMetricsJob(EHentaiClientDi client, ILogger<CollectHathSettingsMetricsJob> logger,
        IConfiguration cfg, MetricsCollectorService metricsCollector)
    {
        _client = client;
        _logger = logger;
        _metricsCollector = metricsCollector;
        cfg.GetOptionsReflex(out _options);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Begin collect hath settings metrics");
        foreach (var clientId in _options.ClientIds)
        {
            try
            {
                _logger.LogInformation("Begin collect hath settings metrics for id {clientId}", clientId);
                var resp = await _client.MyHome.GetHathSettingsAsync(clientId, context.CancellationToken);
                _logger.LogDebug("EH raw response: page {page}, html: {html}", "MyHome.Overview", resp.RawStringBody);
                _metricsCollector.SetClientSettings(resp.Body!);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Can't get settings for client {clientId}", clientId);
            }
        }
    }
}