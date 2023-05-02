using ArkProjects.EHentai.MetricsCollector.Services;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Jobs;

[DisallowConcurrentExecution]
public class CollectHathSettingsMetricsJob : IJob
{
    private readonly ILogger<CollectHathSettingsMetricsJob> _logger;
    private readonly MetricsCollectorService _metricsCollector;
    private readonly EHentaiClientDi _client;

    public CollectHathSettingsMetricsJob(EHentaiClientDi client, ILogger<CollectHathSettingsMetricsJob> logger,
        MetricsCollectorService metricsCollector)
    {
        _client = client;
        _logger = logger;
        _metricsCollector = metricsCollector;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var clientId = context.MergedJobDataMap.GetLongValue("ClientId");
        if (clientId == default)
            throw new Exception("Job data ClientId(number) must be set");
        _logger.LogInformation("Begin collect hath settings metrics for id {clientId}", clientId);
        var resp = await _client.MyHome.GetHathSettingsAsync(clientId, context.CancellationToken);
        _logger.LogDebug("EH raw response: page {page}, html: {html}", "MyHome.HathSettings", resp.RawStringBody);
        _metricsCollector.SetClientSettings(resp.Body!);
    }
}