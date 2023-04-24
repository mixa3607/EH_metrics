using Serilog.Events;

namespace ArkProjects.EHentai.MetricsCollector.Options;

public class SerilogOptions
{
    public List<Uri> ElasticUris { get; set; } = new();
    public bool DisableElastic { get; set; }
    public string? ElasticIndexPrefix { get; set; }
    public SerilogOptionsLevelPresetType LevelPreset { get; set; } = SerilogOptionsLevelPresetType.Prod;
    public bool EnableRequestLogging { get; set; }
    public LogEventLevel RequestLogMessageLevel { get; set; } = LogEventLevel.Debug;

    public enum SerilogOptionsLevelPresetType : byte
    {
        Prod,
        Dev
    }
}