using System.Diagnostics;
using Flurl.Http;

namespace ArkProjects.EHentai.MetricsCollector.Misc;

public static class SpeedTestRunner
{
    private static readonly byte[] SharedBuffer = new byte[100 * 1024];

    public static async Task<TestCommandResult> MakeTestRequestAsync(IFlurlRequest request, int testSize,
        CancellationToken ct = default)
    {
        var result = new TestCommandResult();
        var sw = new Stopwatch();
        try
        {
            sw.Start();
            var resp = await request.GetAsync(HttpCompletionOption.ResponseHeadersRead, ct);

            result.StatusCode = resp.StatusCode;

            var stream = await resp.GetStreamAsync();
            var totalRead = 0L;
            while (true)
            {
                var read = await stream.ReadAsync(SharedBuffer, 0, SharedBuffer.Length, ct);
                if (read == 0)
                    break;
                totalRead += read;
            }

            if (totalRead != testSize)
                throw new Exception($"Receive {totalRead} bytes but expected {testSize}");

            result.Success = true;
        }
        catch (Exception e)
        {
            result.Success = false;
            result.Exception = e;
        }

        sw.Stop();

        result.Elapsed = sw.Elapsed;
        return result;
    }
}