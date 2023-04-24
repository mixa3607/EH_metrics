using ArkProjects.EHentai.Api.Client;
using Microsoft.Extensions.Options;

namespace ArkProjects.EHentai.MetricsCollector.Services;

public class EHentaiClientDi : EHentaiClient
{
    private readonly IOptions<EHentaiClientOptionsDi> _options;
    private readonly ILogger<EHentaiClientDi> _logger;

    public EHentaiClientDi(IOptions<EHentaiClientOptionsDi> options, ILogger<EHentaiClientDi> logger): base(options.Value, logger)
    {
        _options = options;
        _logger = logger;
    }
}