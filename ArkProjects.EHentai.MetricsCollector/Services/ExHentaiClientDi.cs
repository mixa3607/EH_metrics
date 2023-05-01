using ArkProjects.EHentai.Api.Client;
using Microsoft.Extensions.Options;

namespace ArkProjects.EHentai.MetricsCollector.Services;

public class ExHentaiClientDi : EHentaiClient
{
    private readonly IOptions<EHentaiClientOptionsDi> _options;
    private readonly ILogger<ExHentaiClientDi> _logger;

    public ExHentaiClientDi(IOptions<EHentaiClientOptionsDi> options, ILogger<ExHentaiClientDi> logger) : base(
        new EHentaiClientOptions()
        {
            MemberId = options.Value.MemberId,
            PassHash = options.Value.PassHash,
            SessionId = options.Value.SessionId,
            Igneous = options.Value.Igneous,
            Proxy = options.Value.Proxy,
            SiteType = EHentaiSiteType.ExHentai,
        }, logger)
    {
        _options = options;
        _logger = logger;
    }
}