using ArkProjects.EHentai.Api.Client;
using ArkProjects.EHentai.Api.Models.Requests;
using ArkProjects.EHentai.MetricsCollector.Options;
using ArkProjects.EHentai.MetricsCollector.Services;
using Prometheus;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Jobs;

[DisallowConcurrentExecution]
public class CollectHomeOverviewMetricsJob : IJob
{
    private readonly ILogger<CollectHomeOverviewMetricsJob> _logger;
    private readonly EHentaiClientDi _client;
    private readonly CollectHomeOverviewMetricsJobOptions _options;

    public CollectHomeOverviewMetricsJob(EHentaiClientDi client, ILogger<CollectHomeOverviewMetricsJob> logger,
        IConfiguration cfg)
    {
        _client = client;
        _logger = logger;
        cfg.GetOptionsReflex(out _options);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Begin collect home overview metrics");
        var metricsFac = Metrics.WithManagedLifetime(_options.MetricLifetime);
        EHentaiClientResponse<OverviewResponse> resp;

        //api time
        var apiPerfHist = metricsFac
            .CreateHistogram("eh_api_perf_seconds", "E-Hentai api performance", new[] { "page" })
            .WithExtendLifetimeOnUse();
        using (apiPerfHist.WithLabels("home.overview").NewTimer())
            resp = await _client.MyHome.GetOverviewAsync(context.CancellationToken);


        //eht
        {
            metricsFac
                .CreateGauge("eh_eht_uploaded_mb", "E-Hentai EHTracker uploaded megabytes")
                .WithExtendLifetimeOnUse()
                .WithLabels().Set(resp.Body!.EhTracker.Uploaded);
            metricsFac
                .CreateGauge("eh_eht_downloaded_mb", "E-Hentai EHTracker downloaded megabytes")
                .WithExtendLifetimeOnUse()
                .WithLabels().Set(resp.Body!.EhTracker.Downloaded);
            metricsFac
                .CreateGauge("eh_eht_seed_minutes", "E-Hentai EHTracker seeding minutes")
                .WithExtendLifetimeOnUse()
                .WithLabels().Set(resp.Body!.EhTracker.SeedMinutes);
            metricsFac
                .CreateGauge("eh_eht_torrent_completes_number", "E-Hentai EHTracker torrent completes")
                .WithExtendLifetimeOnUse()
                .WithLabels().Set(resp.Body!.EhTracker.TorrentCompletes);
            metricsFac
                .CreateGauge("eh_eht_gallery_completes_number", "E-Hentai EHTracker gallery completes")
                .WithExtendLifetimeOnUse()
                .WithLabels().Set(resp.Body!.EhTracker.GalleryCompletes);
            metricsFac
                .CreateGauge("eh_eht_up_down_ratio", "E-Hentai EHTracker up/down ratio")
                .WithExtendLifetimeOnUse()
                .WithLabels().Set(resp.Body!.EhTracker.UpDownRatio);
        }

        //gp gained
        {
            metricsFac
                .CreateGauge("eh_gp_gallery_visits_total_number", "E-Hentai Total GP Gained from gallery visits")
                .WithExtendLifetimeOnUse()
                .WithLabels().Set(resp.Body!.TotalGpGained.FromGalleryVisits);
            metricsFac
                .CreateGauge("eh_gp_torrent_completions_total_number", "E-Hentai Total GP Gained from torrent completions")
                .WithExtendLifetimeOnUse()
                .WithLabels().Set(resp.Body!.TotalGpGained.FromTorrentCompletions);
            metricsFac
                .CreateGauge("eh_gp_archive_downloads_total_number", "E-Hentai Total GP Gained from archive downloads")
                .WithExtendLifetimeOnUse()
                .WithLabels().Set(resp.Body!.TotalGpGained.FromArchiveDownloads);
            metricsFac
                .CreateGauge("eh_gp_hath_total_number", "E-Hentai Total GP from Hentai@Home")
                .WithExtendLifetimeOnUse()
                .WithLabels().Set(resp.Body!.TotalGpGained.FromHath);
        }
    }
}