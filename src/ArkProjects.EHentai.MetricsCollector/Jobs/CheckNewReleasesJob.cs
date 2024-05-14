using System.Text.Json.Serialization;
using ArkProjects.EHentai.MetricsCollector.Services;
using Flurl.Http;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Jobs;

public class CheckNewReleasesJob : IJob
{
    private readonly ILogger<CheckNewReleasesJob> _logger;
    private readonly MetricsCollectorService _metricsCollector;
    private readonly IAppVersionInfoService _appVersionInfo;

    public CheckNewReleasesJob(ILogger<CheckNewReleasesJob> logger,
        MetricsCollectorService metricsCollector, IAppVersionInfoService appVersionInfo)
    {
        _logger = logger;
        _metricsCollector = metricsCollector;
        _appVersionInfo = appVersionInfo;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        context.MergedJobDataMap.TryGetBooleanValue("EnableBetaChannel", out var enableBetaChannel);

        if (_appVersionInfo.Repo == null)
        {
            _metricsCollector.SetVersion(0);
            _logger.LogInformation("Original project repo is unknown. Non CI build?");
            return;
        }

        var releases = await $"https://api.github.com/repos/{_appVersionInfo.Repo}/releases"
            .WithHeader("user-agent", "curl/7.81.0")
            .GetJsonAsync<IReadOnlyList<GithubReleaseModel>>();

        var latest = releases.FirstOrDefault(x => !x.Draft && (!x.Prerelease || enableBetaChannel));
        if (latest == null)
        {
            _metricsCollector.SetVersion(0);
            _logger.LogDebug("No releases");
            return;
        }

        if (_appVersionInfo.GitRef != null && latest.TagName == _appVersionInfo.GitRef)
        {
            _metricsCollector.SetVersion(1);
            _logger.LogDebug("Latest release installed");
            return;
        }

        _metricsCollector.SetVersion(2);
        _logger.LogWarning("Found new version {ver}. {name}. See: {link}", 
            latest.TagName, latest.Name, latest.HtmlUrl);
    }

    private class GithubReleaseModel
    {
        [JsonPropertyName("tag_name")]
        public required string TagName { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("html_url")]
        public required string HtmlUrl { get; set; }

        [JsonPropertyName("draft")]
        public required bool Draft { get; set; }

        [JsonPropertyName("prerelease")]
        public required bool Prerelease { get; set; }

        [JsonPropertyName("created_at")]
        public required DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("published_at")]
        public required DateTimeOffset PublishedAt { get; set; }
    }
}