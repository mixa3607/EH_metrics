using ArkProjects.EHentai.Api.Client;
using ArkProjects.EHentai.Api.Models.Requests;
using ArkProjects.EHentai.MetricsCollector.Options;
using ArkProjects.EHentai.MetricsCollector.Services;
using Prometheus;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Jobs;

[DisallowConcurrentExecution]
public class CollectHathPerksMetricsJob : IJob
{
    private readonly ILogger<CollectHathPerksMetricsJob> _logger;
    private readonly EHentaiClientDi _client;
    private readonly CollectHathSettingsMetricsJobOptions _options;

    public CollectHathPerksMetricsJob(EHentaiClientDi client, ILogger<CollectHathPerksMetricsJob> logger,
        IConfiguration cfg)
    {
        _client = client;
        _logger = logger;
        cfg.GetOptionsReflex(out _options);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Begin collect hath perks metrics");
        var metricsFac = Metrics.WithManagedLifetime(_options.MetricLifetime);
        EHentaiClientResponse<HathPerksResponse> resp;

        //api time
        var apiPerfHist = metricsFac
            .CreateHistogram("eh_api_perf_seconds", "E-Hentai api performance", new[] { "page" })
            .WithExtendLifetimeOnUse();
        using (apiPerfHist.WithLabels("home.hathperks").NewTimer())
            resp = await _client.MyHome.GetHathPerksAsync(context.CancellationToken);

        metricsFac
            .CreateGauge("eh_hath_balance_number", "E-Hentai current hath balance")
            .WithExtendLifetimeOnUse()
            .WithLabels().Set(resp.Body!.Hath);
    }
}