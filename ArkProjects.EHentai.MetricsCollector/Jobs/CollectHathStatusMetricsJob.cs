using ArkProjects.EHentai.Api.Client;
using ArkProjects.EHentai.Api.Models;
using ArkProjects.EHentai.Api.Models.Requests;
using ArkProjects.EHentai.MetricsCollector.Options;
using ArkProjects.EHentai.MetricsCollector.Services;
using Prometheus;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Jobs;

[DisallowConcurrentExecution]
public class CollectHathStatusMetricsJob : IJob
{
    private readonly ILogger<CollectHathStatusMetricsJob> _logger;
    private readonly EHentaiClientDi _client;
    private readonly CollectHathSettingsMetricsJobOptions _options;

    public CollectHathStatusMetricsJob(EHentaiClientDi client, ILogger<CollectHathStatusMetricsJob> logger,
        IConfiguration cfg)
    {
        _client = client;
        _logger = logger;
        cfg.GetOptionsReflex(out _options);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Begin collect hath status metrics");
        var metricsFac = Metrics.WithManagedLifetime(_options.MetricLifetime);
        EHentaiClientResponse<HathStatusResponse> resp;

        //api time
        var apiPerfHist = metricsFac
            .CreateHistogram("eh_api_perf_seconds", "E-Hentai api performance", new[] { "page" })
            .WithExtendLifetimeOnUse();
        using (apiPerfHist.WithLabels("home.hath-status").NewTimer())
            resp = await _client.MyHome.GetHathStatusAsync(context.CancellationToken);


        //regions
        {
            var labelNames = new[] { "region" };
            var netLoadGauge = metricsFac
                .CreateGauge("eh_hath_regions_netload_mbps", "E-Hentai H@H current network load", labelNames)
                .WithExtendLifetimeOnUse();
            var missGauge = metricsFac
                .CreateGauge("eh_hath_regions_miss_percent", "E-Hentai H@H current miss %", labelNames)
                .WithExtendLifetimeOnUse();
            var coverageGauge = metricsFac
                .CreateGauge("eh_hath_regions_coverage_percent", "E-Hentai H@H coverage", labelNames)
                .WithExtendLifetimeOnUse();
            var hitsGauge = metricsFac
                .CreateGauge("eh_hath_regions_hits_per_gb_ratio", "E-Hentai H@H Hits/GB ratio", labelNames)
                .WithExtendLifetimeOnUse();
            var qualityGauge = metricsFac
                .CreateGauge("eh_hath_regions_quality_number", "E-Hentai H@H quality", labelNames)
                .WithExtendLifetimeOnUse();
            foreach (var region in resp.Body!.Regions)
            {
                var labels = new[] { region.Region.ToString() };
                netLoadGauge.WithLabels(labels).Set(region.NetLoad);
                missGauge.WithLabels(labels).Set(region.MissRate);
                coverageGauge.WithLabels(labels).Set(region.Coverage);
                hitsGauge.WithLabels(labels).Set(region.HitsPerGb);
                qualityGauge.WithLabels(labels).Set(region.Quality);
            }
        }

        //clients
        {
            var labelNames = new[] { "client_name", "id", "ip" };
            var statusGauge = metricsFac
                .CreateGauge("eh_hath_clients_status_enum", "E-Hentai H@H client status", labelNames)
                .WithExtendLifetimeOnUse();
            var filesServedGauge = metricsFac
                .CreateGauge("eh_hath_clients_files_served_number", "E-Hentai H@H client files served", labelNames)
                .WithExtendLifetimeOnUse();
            var maxSpeedGauge = metricsFac
                .CreateGauge("eh_hath_clients_max_speed_kbps", "E-Hentai H@H client max kilobytes per sec", labelNames)
                .WithExtendLifetimeOnUse();
            var trustGauge = metricsFac
                .CreateGauge("eh_hath_clients_trust_number", "E-Hentai H@H client trust", labelNames)
                .WithExtendLifetimeOnUse();
            var qualityGauge = metricsFac
                .CreateGauge("eh_hath_clients_quality_number", "E-Hentai H@H client quality", labelNames)
                .WithExtendLifetimeOnUse();
            var hitrateGauge = metricsFac
                .CreateGauge("eh_hath_clients_hitrate_number", "E-Hentai H@H client hits per minute", labelNames)
                .WithExtendLifetimeOnUse();
            var hathrateGauge = metricsFac
                .CreateGauge("eh_hath_clients_hathrate_number", "E-Hentai H@H client hath per day", labelNames)
                .WithExtendLifetimeOnUse();
            foreach (var client in resp.Body!.Clients)
            {
                statusGauge.WithLabels(client.Name, client.Id.ToString(), client.ClientIp).Set((byte)client.Status);
                filesServedGauge.WithLabels(client.Name, client.Id.ToString(), client.ClientIp).Set(client.FilesServed);
                maxSpeedGauge.WithLabels(client.Name, client.Id.ToString(), client.ClientIp).Set(client.MaxSpeed);
                trustGauge.WithLabels(client.Name, client.Id.ToString(), client.ClientIp).Set(client.Trust);
                qualityGauge.WithLabels(client.Name, client.Id.ToString(), client.ClientIp).Set(client.Quality);
                hitrateGauge.WithLabels(client.Name, client.Id.ToString(), client.ClientIp).Set(client.Hitrate);
                hathrateGauge.WithLabels(client.Name, client.Id.ToString(), client.ClientIp).Set(client.Hathrate);
            }

            var totalGauge = metricsFac
                .CreateGauge("eh_hath_clients_count_number", "E-Hentai H@H clients count", new[] { "state" })
                .WithExtendLifetimeOnUse();
            foreach (var value in Enum.GetValues<HathClientStatus>())
            {
                totalGauge.WithLabels(value.ToString()).Set(resp.Body!.Clients.Count(x => x.Status == value));
            }
        }
    }
}