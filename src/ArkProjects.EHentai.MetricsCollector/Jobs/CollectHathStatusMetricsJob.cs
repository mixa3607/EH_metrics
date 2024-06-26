﻿using ArkProjects.EHentai.MetricsCollector.Services;
using Quartz;

namespace ArkProjects.EHentai.MetricsCollector.Jobs;

public class CollectHathStatusMetricsJob : IJob
{
    private readonly ILogger<CollectHathStatusMetricsJob> _logger;
    private readonly MetricsCollectorService _metricsCollector;
    private readonly EHentaiClientDi _client;

    public CollectHathStatusMetricsJob(EHentaiClientDi client, ILogger<CollectHathStatusMetricsJob> logger,
        MetricsCollectorService metricsCollector)
    {
        _client = client;
        _logger = logger;
        _metricsCollector = metricsCollector;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Begin collect hath status metrics");
        var resp = await _client.MyHome.GetHathStatusAsync(context.CancellationToken);
        _logger.LogTrace("EH raw response: page {page}, html: {html}", "MyHome.HathStatus", resp.RawStringBody);
        _metricsCollector.SetHathStatus(resp.Body!);
    }
}