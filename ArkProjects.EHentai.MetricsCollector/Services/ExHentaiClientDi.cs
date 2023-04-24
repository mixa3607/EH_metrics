using ArkProjects.EHentai.Api.Client;
using Microsoft.Extensions.Options;

namespace ArkProjects.EHentai.MetricsCollector.Services;

public class ExHentaiClientDi : EHentaiClient
{
    private readonly IOptions<ExHentaiClientOptionsDi> _options;
    private readonly ILogger<ExHentaiClientDi> _logger;

    public ExHentaiClientDi(IOptions<ExHentaiClientOptionsDi> options, ILogger<ExHentaiClientDi> logger) : base(options.Value, logger)
    {
        _options = options;
        _logger = logger;
    }
}