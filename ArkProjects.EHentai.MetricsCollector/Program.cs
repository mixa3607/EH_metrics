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
using ArkProjects.EHentai.MetricsCollector.Services;
using System.Reflection;
using Microsoft.Extensions.Options;

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
    var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name!.ToLower();
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
app.MapMetrics();

//#########################################################################

_ = LogVersionAndEnvAsync(app);
_ = TriggerMarkedJobsAsync(app);
await app.RunAsync();

//#########################################################################

static void ConfigureScheduler(WebApplicationBuilder appBuilder)
{
    appBuilder.Services
        .AddAndGetOptionsReflex<AppQuartzOptions>(appBuilder.Configuration, out var options)
        .AddQuartzServer(x => x.WaitForJobsToComplete = true)
        .AddQuartz(x =>
        {
            x.UseMicrosoftDependencyInjectionJobFactory();
            foreach (var jobDef in options.Jobs.Values)
            {
                x.AddCronJob(jobDef);
            }
        });
}

static async Task TriggerMarkedJobsAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    try
    {
        var options = app.Services.GetRequiredService<IOptions<AppQuartzOptions>>().Value;
        var scheduler = await app.Services.GetRequiredService<ISchedulerFactory>().GetScheduler();

        var jTasks = new Dictionary<string, Task>();
        foreach (var (name, def) in options.Jobs.Where(x => x.Value.TriggerOnStartup))
        {
            logger.LogInformation("Trigger {name}({def_name}) job", name, def.Name);
            jTasks.Add(name, scheduler.TriggerJob(new JobKey(def.Name!, def.Group)));
        }

        await Task.WhenAll(jTasks.Values);
        foreach (var (name, task) in jTasks)
        {
            if (task.IsCompletedSuccessfully)
                logger.LogInformation("Task {name} completed successfully", name);
            else if (task.IsFaulted)
                logger.LogWarning(task.Exception, "Task {name} completed with exception", name);
            else if (task.IsCanceled)
                logger.LogWarning("Task {name} cancelled", name);
        }
    }
    catch (Exception e)
    {
        logger.LogError(e, "Error on triggering jobs on startup");
    }
}

static Task LogVersionAndEnvAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    try
    {
        var assembly = Assembly.GetEntryAssembly();
        var versionAttr = assembly?.GetCustomAttribute<AssemblyFileVersionAttribute>();
        var attrs = assembly?.GetCustomAttributes<AssemblyMetadataAttribute>().ToArray();
        var gitCommitSha = attrs?.FirstOrDefault(x => x.Key == "GIT_COMMIT_SHA")?.Value;
        var buildDate = attrs?.FirstOrDefault(x => x.Key == "BUILD_DATE")?.Value;
        var gitRefType = attrs?.FirstOrDefault(x => x.Key == "GIT_REF_TYPE")?.Value;
        var gitRef = attrs?.FirstOrDefault(x => x.Key == "GIT_REF")?.Value;

        logger.LogInformation("Application: {app}", app.Environment.ApplicationName);
        logger.LogInformation("Environment: {env}", app.Environment.EnvironmentName);
        logger.LogInformation("BuildDate: {build_date}", buildDate);
        logger.LogInformation("Version: {ver}", versionAttr?.Version);
        logger.LogInformation("[GIT] RefType: {ref_type}, Ref: {ref}, Sha: {sha}", gitRefType, gitRef, gitCommitSha);
    }
    catch (Exception e)
    {
        logger.LogError(e, "Error on resolving app version");
    }

    return Task.CompletedTask;
}