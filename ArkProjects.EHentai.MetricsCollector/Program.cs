
using System.Dynamic;
using Prometheus;
using Quartz;
using ArkProjects.EHentai.MetricsCollector;
using ArkProjects.EHentai.MetricsCollector.Quartz;
using ArkProjects.EHentai.MetricsCollector.Options;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using System.Buffers;
using System.Net.Mime;
using System.Text;
using ArkProjects.EHentai.MetricsCollector.Services;
using Newtonsoft.Json;

Metrics.SuppressDefaultMetrics();

//#########################################################################

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine($"Config: {builder.Environment.EnvironmentName}");

//#########################################################################

//logging
var serilogOptions = builder.Configuration.GetOptionsReflex<SerilogOptions>();
builder.Host.UseSerilog((ctx, l) =>
{
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    var assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name!.ToLower();
    l
        .Enrich.WithEnvironmentName()
        .Enrich.WithEnvironmentUserName()
        .Enrich.WithThreadId()
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProcessName()
        .Enrich.WithThreadName()
        .Enrich.WithProcessId()
        .Enrich.WithExceptionDetails()
        .Enrich.WithAssemblyName()
        .Enrich.WithAssemblyVersion()
        .Enrich.WithMemoryUsage()
        .Destructure.ByTransforming<ExpandoObject>(e => new Dictionary<string, object>(e));

    if (!serilogOptions.DisableElastic)
    {
        var indexFormat = $"serilogs-sink-8.4.1-{assemblyName}-{{0:yyyy.MM.dd}}";
        if (serilogOptions.ElasticIndexPrefix != null)
            indexFormat = $"{serilogOptions.ElasticIndexPrefix}-{indexFormat}";

        Console.WriteLine($"Elastic logging enabled. Index format: {indexFormat}");
        var options = new ElasticsearchSinkOptions(serilogOptions.ElasticUris)
        {
            AutoRegisterTemplate = true,
            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
            FailureCallback = e =>
                Console.WriteLine($"Unable to submit event template {e.MessageTemplate} with error {e.Exception}"),
            EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                               EmitEventFailureHandling.WriteToFailureSink |
                               EmitEventFailureHandling.RaiseCallback,
            RegisterTemplateFailure = RegisterTemplateRecovery.IndexToDeadletterIndex,
            NumberOfShards = 1,
            NumberOfReplicas = 0,
            IndexFormat = indexFormat,
            DeadLetterIndexName = "deadletter-" + indexFormat,
            BufferLogShippingInterval = TimeSpan.FromSeconds(5),
            TemplateCustomSettings = new Dictionary<string, string>()
            {
                { "index.mapping.total_fields.limit", "2000" }
            }
        };
        l.WriteTo.Elasticsearch(options);
    }

    l.WriteTo.Console();

    Console.WriteLine($"Used log level preset: {serilogOptions.LevelPreset}");
    if (serilogOptions.LevelPreset == SerilogOptions.SerilogOptionsLevelPresetType.Prod)
    {
        l.MinimumLevel.Information()
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Server.Kestrel", LogEventLevel.Fatal)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Verbose)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database", LogEventLevel.Fatal)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Query", LogEventLevel.Fatal)
            .MinimumLevel.Override("Serilog.AspNetCore.RequestLoggingMiddleware", LogEventLevel.Debug)
            ;
    }
    else if (serilogOptions.LevelPreset == SerilogOptions.SerilogOptionsLevelPresetType.Dev)
    {
        l.MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Verbose)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Verbose);
    }
});

//quartz
ConfigureScheduler(builder);

//e clients
builder.Services
    .AddAndGetOptionsReflex<EHentaiClientOptionsDi>(builder.Configuration, out _)
    .AddSingleton<EHentaiClientDi>()
    .AddAndGetOptionsReflex<ExHentaiClientOptionsDi>(builder.Configuration, out _)
    .AddSingleton<ExHentaiClientDi>()
    ;


//#########################################################################

var app = builder.Build();

if (serilogOptions.EnableRequestLogging)
{
    const int inMemBuffLen = 50 * 1024;
    const int maxLoggingBodyLen = 100 * 1024;

    Console.WriteLine($"Request logging enabled with level {serilogOptions.RequestLogMessageLevel}");
    app.Use((context, next) =>
    {
        context.Request.EnableBuffering(inMemBuffLen);
        return next();
    });

    app.UseSerilogRequestLogging(o =>
    {
        o.GetLevel = (httpContext, elapsed, ex) => serilogOptions.RequestLogMessageLevel;
        o.EnrichDiagnosticContext = (diagnosticContext, context) =>
        {
            var bodyStr = (string?)null;
            var body = (dynamic?)null;
            if (context.Request.ContentType == MediaTypeNames.Application.Json)
            {
                var blob = MemoryPool<byte>.Shared.Rent(maxLoggingBodyLen).Memory[..(maxLoggingBodyLen)];

                var origPos = context.Request.Body.Position;
                context.Request.Body.Position = 0;
                var read = context.Request.Body.ReadAsync(blob, CancellationToken.None).Result;
                bodyStr = Encoding.UTF8.GetString(blob[..read].Span);
                context.Request.Body.Position = origPos;
                if (context.Request.Body.Length > maxLoggingBodyLen)
                {
                    body = $"Body len more than {maxLoggingBodyLen}. Cant deserialize";
                }
                else
                {
                    try
                    {
                        body = JsonConvert.DeserializeObject<ExpandoObject?>(bodyStr);
                    }
                    catch (Exception e)
                    {
                        body = e.ToString();
                    }
                }
            }

            var request = new
            {
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                Host = context.Request.Host.ToString(),
                Path = context.Request.Path.ToString(),
                IsHttps = context.Request.IsHttps,
                Scheme = context.Request.Scheme,
                Method = context.Request.Method,
                ContentType = context.Request.ContentType,
                Protocol = context.Request.Protocol,
                QueryString = context.Request.QueryString.ToString(),
                Query = context.Request.Query.ToDictionary(x => x.Key, y => y.Value.ToString()),
                Headers = context.Request.Headers.ToDictionary(x => x.Key, y => y.Value.ToString()),
                Cookies = context.Request.Cookies.ToDictionary(x => x.Key, y => y.Value.ToString()),
                BodyString = bodyStr,
                Body = body,
            };
            diagnosticContext.Set("Request", request, true);
        };
    });
}
else
{
    Console.WriteLine($"Request logging disabled");
}


app.MapMetrics();

//#########################################################################

app.Services.GetRequiredService<ISchedulerFactory>().GetScheduler().Result
    .TriggerJob(new JobKey("home_overview_metrics", "metrics")).Wait();

//var r = app.Services.GetRequiredService<EHentaiClientDi>().MyHome.GetOverviewAsync().Result;
await app.RunAsync();

//#########################################################################

void ConfigureScheduler(WebApplicationBuilder appBuilder)
{
    appBuilder.Configuration.GetOptionsReflex<ArkProjects.EHentai.MetricsCollector.Options.QuartzOptions>(out var options);

    appBuilder.Services.AddQuartz(x =>
    {
        x.UseMicrosoftDependencyInjectionJobFactory();
        foreach (var jobDef in options.Jobs.Values)
        {
            x.AddCronJob(jobDef);
        }
    });
    appBuilder.Services.AddQuartzServer(x => x.WaitForJobsToComplete = true);
}