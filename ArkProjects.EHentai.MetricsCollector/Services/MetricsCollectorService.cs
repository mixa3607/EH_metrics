using ArkProjects.EHentai.Api.Models.Requests;
using ArkProjects.EHentai.MetricsCollector.Options;
using Microsoft.Extensions.Options;
using Prometheus;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Services;

public class MetricsCollectorService
{
    private readonly MetricsCollectorOptions _options;
    private readonly ILogger<MetricsCollectorService> _logger;
    private readonly IMetricFactory _metricFactory;
    private readonly Dictionary<string, (MetricDef def, object? collector)> _collectors;

    private struct MetricDef
    {
        public string Name;
        public string Description;
        public string[] Labels;
        public Type Type;
    }

    public MetricsCollectorService(IOptions<MetricsCollectorOptions> options,
        IOptions<EHentaiClientOptionsDi> ehOptions, ILogger<MetricsCollectorService> logger)
    {
        _logger = logger;
        _options = options.Value;
        var ehOptions1 = ehOptions.Value;
        var defs = new Dictionary<string, MetricDef[]>()
        {
            {
                "Quartz", new MetricDef[]
                {
                    new()
                    {
                        Name = "eh_job_run_time_seconds", Labels = new[] { "name" },
                        Description = "Scheduled job execution run time in seconds",
                        Type = typeof(Histogram),
                    },
                    new()
                    {
                        Name = "eh_job_failures_number", Labels = new[] { "name" },
                        Description = "Scheduled job execution errors",
                        Type = typeof(Counter),
                    },
                }
            },
            {
                "HathPerks", new MetricDef[]
                {
                    new()
                    {
                        Name = "eh_hath_balance_number", Labels = Array.Empty<string>(),
                        Description = "E-Hentai current hath balance",
                        Type = typeof(Gauge),
                    },
                }
            },
            {
                "HomeOverview", new MetricDef[]
                {
                    new()
                    {
                        Name = "eh_eht_uploaded_mb", Labels = Array.Empty<string>(),
                        Description = "E-Hentai EHTracker uploaded megabytes",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_eht_downloaded_mb", Labels = Array.Empty<string>(),
                        Description = "E-Hentai EHTracker downloaded megabytes",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_eht_seed_minutes", Labels = Array.Empty<string>(),
                        Description = "E-Hentai EHTracker seeding minutes",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_eht_completes_number", Labels = new[] { "type" },
                        Description = "E-Hentai EHTracker completes",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_eht_up_down_ratio", Labels = Array.Empty<string>(),
                        Description = "E-Hentai EHTracker up/down ratio",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_gp_gained_number", Labels = new[] { "type" },
                        Description = "E-Hentai Total GP Gained from",
                        Type = typeof(Gauge),
                    },
                }
            },
            {
                "HathStatus", new MetricDef[]
                {
                    new()
                    {
                        Name = "eh_hath_regions_netload_mbps", Labels = new[] { "region" },
                        Description = "E-Hentai H@H current network load",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_hath_regions_miss_percent", Labels = new[] { "region" },
                        Description = "E-Hentai H@H current miss %",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_hath_regions_coverage_percent", Labels = new[] { "region" },
                        Description = "E-Hentai H@H coverage",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_hath_regions_hits_per_gb_ratio", Labels = new[] { "region" },
                        Description = "E-Hentai H@H Hits/GB ratio",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_hath_regions_quality_number", Labels = new[] { "region" },
                        Description = "E-Hentai H@H quality",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_hath_clients_files_served_number",
                        Labels = new[] { "client_name", "client_id", "client_ip" },
                        Description = "E-Hentai H@H client files served",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_hath_clients_max_speed_kbps",
                        Labels = new[] { "client_name", "client_id", "client_ip" },
                        Description = "E-Hentai H@H client max kb/s",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_hath_clients_trust_number",
                        Labels = new[] { "client_name", "client_id", "client_ip" },
                        Description = "E-Hentai H@H client trust",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_hath_clients_quality_number",
                        Labels = new[] { "client_name", "client_id", "client_ip" },
                        Description = "E-Hentai H@H client quality",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_hath_clients_hitrate_number",
                        Labels = new[] { "client_name", "client_id", "client_ip" },
                        Description = "E-Hentai H@H client hits per minute",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_hath_clients_hathrate_number",
                        Labels = new[] { "client_name", "client_id", "client_ip" },
                        Description = "E-Hentai H@H client hath per day",
                        Type = typeof(Gauge),
                    },
                    new()
                    {
                        Name = "eh_hath_clients_status_enum",
                        Labels = new[] { "client_name", "client_id", "client_ip" },
                        Description = "E-Hentai H@H client status",
                        Type = typeof(Gauge),
                    },
                }
            },
            {
                "ClientSettings", new MetricDef[]
                {
                    new()
                    {
                        Name = "eh_hath_clients_ranges_number", Labels = new[] { "client_id" },
                        Description = "E-Hentai H@H client static ranges",
                        Type = typeof(Gauge),
                    },
                }
            },
        };
        _collectors = defs.SelectMany(x => x.Value).ToDictionary(x => x.Name, x => (x, (object?)null));

        _metricFactory = Metrics.WithLabels(new Dictionary<string, string>()
        {
            { "user", ehOptions1.MemberId! }
        });
    }

    public void SetJobTime(IJobExecutionContext jobContext, Exception? ex)
    {
        var key = jobContext.JobDetail.Key;
        var jobFullName = $"{key.Group}.{key.Name}";

        _logger.LogInformation(ex, "Job {fullName} finished. Duration: {duration}, Refire: {refireCount}",
            jobFullName, jobContext.JobRunTime, jobContext.RefireCount);

        GetHistogram("eh_job_run_time_seconds").WithLabels(jobFullName).Observe(jobContext.JobRunTime.Seconds);
        GetCounter("eh_job_failures_number").WithLabels(jobFullName).Inc(ex == null ? 0 : 1);
    }


    public void SetHathPerks(HathPerksResponse resp)
    {
        GetGauge("eh_hath_balance_number").WithLabels().Set(resp.Hath);
    }

    public void SetOverview(OverviewResponse resp)
    {
        void Set(string name, double v, params string[] l) =>
            GetGauge(name).WithLabels(l).Set(v);

        Set("eh_eht_uploaded_mb", resp.EhTracker.Uploaded);
        Set("eh_eht_downloaded_mb", resp.EhTracker.Downloaded);
        Set("eh_eht_seed_minutes", resp.EhTracker.SeedMinutes);
        Set("eh_eht_uploaded_mb", resp.EhTracker.Uploaded);
        Set("eh_eht_uploaded_mb", resp.EhTracker.Uploaded);
        Set("eh_eht_completes_number", resp.EhTracker.GalleryCompletes, "gallery");
        Set("eh_eht_completes_number", resp.EhTracker.TorrentCompletes, "torrent");

        Set("eh_gp_gained_number", resp.TotalGpGained.FromGalleryVisits, "gallery_visits");
        Set("eh_gp_gained_number", resp.TotalGpGained.FromTorrentCompletions, "torrent_completions");
        Set("eh_gp_gained_number", resp.TotalGpGained.FromArchiveDownloads, "archive_downloads");
        Set("eh_gp_gained_number", resp.TotalGpGained.FromHath, "hath");
    }

    public void SetClientSettings(HathSettingsResponse resp)
    {
        void Set(string name, double v, params string[] l) =>
            GetGauge(name).WithLabels(l).Set(v);

        Set("eh_hath_clients_ranges_number", resp.StaticRanges, resp.ClientId.ToString());
    }

    public void SetHathStatus(HathStatusResponse resp)
    {
        void Set(string name, double v, params string[] l) =>
            GetGauge(name).WithLabels(l).Set(v);

        foreach (var hathRegionInfo in resp.Regions)
        {
            var region = hathRegionInfo.Region.ToString();
            Set("eh_hath_regions_netload_mbps", hathRegionInfo.NetLoad, region);
            Set("eh_hath_regions_miss_percent", hathRegionInfo.MissRate, region);
            Set("eh_hath_regions_coverage_percent", hathRegionInfo.Coverage, region);
            Set("eh_hath_regions_hits_per_gb_ratio", hathRegionInfo.HitsPerGb, region);
            Set("eh_hath_regions_quality_number", hathRegionInfo.Quality, region);
        }

        foreach (var clientInfo in resp.Clients)
        {
            var labels = new[] { clientInfo.Name!, clientInfo.Id.ToString(), clientInfo.ClientIp! };
            Set("eh_hath_clients_files_served_number", clientInfo.FilesServed, labels);
            Set("eh_hath_clients_max_speed_kbps", clientInfo.MaxSpeed, labels);
            Set("eh_hath_clients_trust_number", clientInfo.Trust, labels);
            Set("eh_hath_clients_quality_number", clientInfo.Quality, labels);
            Set("eh_hath_clients_hitrate_number", clientInfo.Hitrate, labels);
            Set("eh_hath_clients_hathrate_number", clientInfo.Hathrate, labels);
            Set("eh_hath_clients_status_enum", (byte)clientInfo.Status, labels);
        }
    }

    private Gauge GetGauge(string name)
    {
        lock (this)
        {
            if (!_collectors.TryGetValue(name, out var c))
                throw new Exception($"{name} collector not defined");
            if (c.def.Type != typeof(Gauge))
                throw new Exception($"{name} collector must be {c.def.Type.FullName}");
            c.collector ??= _metricFactory.CreateGauge(c.def.Name, c.def.Description, c.def.Labels);
            return (Gauge)c.collector;
        }
    }

    private Histogram GetHistogram(string name)
    {
        lock (this)
        {
            if (!_collectors.TryGetValue(name, out var c))
                throw new Exception($"{name} collector not defined");
            if (c.def.Type != typeof(Histogram))
                throw new Exception($"{name} collector must be {c.def.Type.FullName}");
            c.collector ??= _metricFactory.CreateHistogram(c.def.Name, c.def.Description, c.def.Labels, new HistogramConfiguration()
            {
                SuppressInitialValue = false
            });
            return (Histogram)c.collector;
        }
    }

    private Counter GetCounter(string name)
    {
        lock (this)
        {
            if (!_collectors.TryGetValue(name, out var c))
                throw new Exception($"{name} collector not defined");
            if (c.def.Type != typeof(Counter))
                throw new Exception($"{name} collector must be {c.def.Type.FullName}");
            c.collector ??= _metricFactory.CreateCounter(c.def.Name, c.def.Description, c.def.Labels);
            return (Counter)c.collector;
        }
    }
}