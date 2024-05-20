using System.Net.Security;
using System.Security.Cryptography;
using System.Text;
using Flurl.Http;

namespace ArkProjects.EHentai.MetricsCollector.Misc;

public static class HathWorkerApi
{
    public static readonly IFlurlClient Client = new FlurlClient(new HttpClient(new HttpClientHandler()
    {
        ServerCertificateCustomValidationCallback = (_, _, _, state) =>
            state is SslPolicyErrors.None or SslPolicyErrors.RemoteCertificateNameMismatch,
    }));

    public static IFlurlRequest GetServerCmdRequest(string host,
        string unixTime, string command, string additional,
        long clientId, string clientKey)
    {
        var signature = $"hentai@home-servercmd-{command}-{additional}-{clientId}-{unixTime}-{clientKey}"
            .GetSha1AsStr();
        return Client.Request($"{host}/servercmd/{command}/{additional}/{unixTime}/{signature}");
    }

    public static IFlurlRequest GetServerSpeedTestRequest(string host,
        string unixTime, int testSize, long clientId, string clientKey)
    {
        var signature = $"hentai@home-speedtest-{testSize}-{unixTime}-{clientId}-{clientKey}"
            .GetSha1AsStr();
        return Client.Request($"{host}/t/{testSize}/{unixTime}/{signature}");
    }

    public static string GetSha1AsStr(this string src)
    {
        var srcBytes = Encoding.UTF8.GetBytes(src);
        return srcBytes.GetSha1AsStr();
    }

    public static byte[] GetSha1(this byte[] src)
    {
        return SHA1.HashData(src);
    }

    public static string GetSha1AsStr(this byte[] src)
    {
        return BytesToHex(src.GetSha1());
    }

    public static string BytesToHex(this byte[] data)
    {
        var sb = new StringBuilder(data.Length * 2);
        foreach (var b in data)
            sb.Append(b.ToString("X2"));
        return sb.ToString().ToLower();
    }
}