using System.Dynamic;
using Prometheus;
using Quartz;
using ArkProjects.EHentai.MetricsCollector.Quartz;
using Serilog;
using Serilog.Exceptions;
using ArkProjects.EHentai.MetricsCollector.Services;
using Quartz.Impl.Matchers;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Application: {builder.Environment.ApplicationName}");
Console.WriteLine($"ContentRoot: {builder.Environment.ContentRootPath}");

//logging
builder.Host.UseSerilog((ctx, s, l) =>
{
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    l
        .ReadFrom.Configuration(s.GetRequiredService<IConfiguration>())
        .ReadFrom.Services(s)
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
        .Destructure.ByTransforming<ExpandoObject>(e => new Dictionary<string, object>(e))
        .Destructure.ToMaximumDepth(5);
});

//metrics
Metrics.SuppressDefaultMetrics();
builder.Services
    .AddSingleton<MetricsCollectorService>()
    ;

//version
builder.Services.AddSingleton<IAppVersionInfoService, AppVersionInfoService>();

//quartz
builder.Services
    .Configure<AppQuartzOptions>(builder.Configuration.GetSection("AppQuartz"))
    .AddSingleton<QuartzJobsInitializerService>()
    .AddQuartzHostedService(x =>
    {
        x.WaitForJobsToComplete = true;
        x.AwaitApplicationStarted = true;
    })
    .AddQuartz(x => { x.AddJobListener<QuartzLoggingJobListener>(GroupMatcher<JobKey>.AnyGroup()); });

//e clients
builder.Services
    .Configure<EHentaiClientOptionsDi>(builder.Configuration.GetSection("EhClient"))
    .AddSingleton<EHentaiClientDi>()
    .AddSingleton<ExHentaiClientDi>()
    ;


//#########################################################################


var app = builder.Build();
app.MapMetrics();


//#########################################################################

app.Services.GetRequiredService<IAppVersionInfoService>().LogInfo();
await app.Services.GetRequiredService<QuartzJobsInitializerService>().InitializeAsync();
await app.RunAsync();