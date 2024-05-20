using ArkProjects.EHentai.MetricsCollector.Misc;
using ArkProjects.EHentai.MetricsCollector.Services;
using Flurl.Http;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Jobs;

public class ClientDirectCmdSpeedTestJob : IJob
{
    private readonly ILogger<CollectHomeOverviewMetricsJob> _logger;
    private readonly MetricsCollectorService _metricsCollector;

    public ClientDirectCmdSpeedTestJob(ILogger<CollectHomeOverviewMetricsJob> logger,
        MetricsCollectorService metricsCollector)
    {
        _logger = logger;
        _metricsCollector = metricsCollector;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        // required
        var clientId = context.MergedJobDataMap.GetLongValue("ClientId");
        if (clientId == default)
            throw new Exception("Job data ClientId(string) must be set");
        var clientKey = context.MergedJobDataMap.GetString("ClientKey");
        if (string.IsNullOrWhiteSpace(clientKey))
            throw new Exception("Job data ClientKey(string) must be set");
        var host = context.MergedJobDataMap.GetString("ClientHost");
        if (string.IsNullOrWhiteSpace(host))
            throw new Exception("Job data ClientHost(string) must be set");

        // optional
        var clientName = context.MergedJobDataMap.GetString("ClientName");
        if (string.IsNullOrWhiteSpace(clientName))
            clientName = "";
        var size = context.MergedJobDataMap.GetIntValue("SizeBytes");
        if (size == default)
            size = 1024 * 1024;
        var threads = context.MergedJobDataMap.GetIntValue("ThreadsCount");
        if (threads == default)
            threads = 5;

        _logger.LogInformation("Begin speed test ({threads} with {bytes}) of {clientId} client",
            threads, size, clientId);

        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var cmd = "speed_test";
        var add = $"testsize={size}";
        var tasks = Enumerable.Range(0, threads).Select(async _ =>
        {
            var request = HathWorkerApi
                .GetServerCmdRequest(host, unixTime, cmd, add, clientId, clientKey)
                .WithTimeout(10_000);
            return await SpeedTestRunner.MakeTestRequestAsync(request, size, ct);
        }).ToArray();
        var results = await Task.WhenAll(tasks);
        var successCompleted = results.Count(x => x.Success);
        if (successCompleted != threads)
        {
            var speed = 0;
            _logger.LogWarning("Speed test failed. Success {s}/{t}",
                successCompleted, threads);
            _metricsCollector.SetDirectSpeedTest(clientName, clientId, host, speed, true);
        }
        else
        {
            var elapsed = results
                .Select(x => x.Elapsed)
                .Aggregate(TimeSpan.Zero, (c, p) => c + p);
            var kbps = size * threads / (int)elapsed.TotalSeconds / 1024;
            _logger.LogInformation("Speed test success. Success {s}/{t}. Speed {speed} kb/s",
                successCompleted, threads, kbps);
            _metricsCollector.SetDirectSpeedTest(clientName, clientId, host, kbps, true);
        }
    }
}