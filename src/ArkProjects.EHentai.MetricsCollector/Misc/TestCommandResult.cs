namespace ArkProjects.EHentai.MetricsCollector.Misc;

public class TestCommandResult
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public TimeSpan Elapsed { get; set; }
    public Exception? Exception { get; set; }
}