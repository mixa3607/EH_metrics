using ArkProjects.EHentai.Api.Client;
using ArkProjects.EHentai.Api.Models.Requests;
using ArkProjects.EHentai.MetricsCollector.Options;
using ArkProjects.EHentai.MetricsCollector.Services;
using Prometheus;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Jobs;

[DisallowConcurrentExecution]
public class CollectHathSettingsMetricsJob : IJob
{
    private readonly ILogger<CollectHathSettingsMetricsJob> _logger;
    private readonly EHentaiClientDi _client;
    private readonly CollectHathSettingsMetricsJobOptions _options;

    public CollectHathSettingsMetricsJob(EHentaiClientDi client, ILogger<CollectHathSettingsMetricsJob> logger,
        IConfiguration cfg)
    {
        _client = client;
        _logger = logger;
        cfg.GetOptionsReflex(out _options);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Begin collect hath settings metrics");

        var metricsFac = Metrics.WithManagedLifetime(_options.MetricLifetime);
        var apiPerfHist = metricsFac
            .CreateHistogram("eh_api_perf_seconds", "E-Hentai api performance", new[] { "page" })
            .WithExtendLifetimeOnUse();
        var clientRegionsGauge = Metrics.CreateGauge("eh_hath_clients_ranges_number",
            "E-Hentai H@H current static ranges by client", "id");

        foreach (var clientId in _options.ClientIds)
        {
            _logger.LogInformation("Begin collect hath settings metrics for id {clientId}", clientId);
            EHentaiClientResponse<HathSettingsResponse> resp;

            using (apiPerfHist.WithLabels("home.hath-settings").NewTimer())
                resp = await _client.MyHome.GetHathSettingsAsync(clientId, context.CancellationToken);

            clientRegionsGauge.WithLabels(resp.Body!.ClientId.ToString()).Set(resp.Body!.StaticRanges);
        }
    }
}