using ArkProjects.EHentai.Api.Client;
using Microsoft.Extensions.Options;

namespace ArkProjects.EHentai.MetricsCollector.Services;

public class EHentaiClientDi : EHentaiClient
{
    private readonly IOptions<EHentaiClientOptionsDi> _options;
    private readonly ILogger<EHentaiClientDi> _logger;

    public EHentaiClientDi(IOptions<EHentaiClientOptionsDi> options, ILogger<EHentaiClientDi> logger) : base(
        new EHentaiClientOptions()
        {
            MemberId = options.Value.MemberId,
            PassHash = options.Value.PassHash,
            SessionId = options.Value.SessionId,
            Igneous = options.Value.Igneous,
            Proxy = options.Value.Proxy,
            SiteType = EHentaiSiteType.EHentai,
        }, logger)
    {
        _options = options;
        _logger = logger;
    }
}