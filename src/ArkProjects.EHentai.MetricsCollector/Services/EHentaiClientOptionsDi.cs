using ArkProjects.EHentai.Api.Proxy;

namespace ArkProjects.EHentai.MetricsCollector.Services;

public class EHentaiClientOptionsDi
{
    public string? SessionId { get; set; }
    public string? PassHash { get; set; }
    public string? MemberId { get; set; }
    public string? Igneous { get; set; }
    public WebProxyOptions Proxy { get; set; } = new();
}