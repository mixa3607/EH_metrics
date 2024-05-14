using System.Reflection;

namespace ArkProjects.EHentai.MetricsCollector.Services;

public class AppVersionInfoService : IAppVersionInfoService
{
    private readonly ILogger<AppVersionInfoService> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public string? BuildDate { get; }
    public string? GitRefType { get; }
    public string? GitRef { get; }
    public string? GitCommitSha { get; }

    public string? Repo { get; }
    public string? RepoUrl { get; }
    public string? ProjectUrl { get; }

    public AppVersionInfoService(ILogger<AppVersionInfoService> logger, IWebHostEnvironment webHostEnvironment)
    {
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
        try
        {
            var attrs = Assembly.GetEntryAssembly()?.GetCustomAttributes<AssemblyMetadataAttribute>().ToArray();
            BuildDate = attrs?.FirstOrDefault(x => x.Key == "BUILD_DATE")?.Value;

            GitRefType = attrs?.FirstOrDefault(x => x.Key == "GIT_REF_TYPE")?.Value;
            GitRef = attrs?.FirstOrDefault(x => x.Key == "GIT_REF")?.Value;
            GitCommitSha = attrs?.FirstOrDefault(x => x.Key == "GIT_COMMIT_SHA")?.Value;

            ProjectUrl = attrs?.FirstOrDefault(x => x.Key == "PROJECT_URL")?.Value;
            RepoUrl = attrs?.FirstOrDefault(x => x.Key == "REPO_URL")?.Value;
            Repo = attrs?.FirstOrDefault(x => x.Key == "REPO")?.Value;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error on resolving app version");
        }
    }

    public void LogInfo()
    {
        _logger.LogInformation("Application: {app}", _webHostEnvironment.ApplicationName);
        _logger.LogInformation("Environment: {env}", _webHostEnvironment.EnvironmentName);
        _logger.LogInformation("ContentRoot: {env}", _webHostEnvironment.ContentRootPath);
        _logger.LogInformation("BuildDate: {build_date}", BuildDate);
        _logger.LogInformation("[GIT] RefType: {ref_type}, Ref: {ref}, Sha: {sha}", GitRefType, GitRef, GitCommitSha);
        _logger.LogInformation("[GIT] Repo: {repo}, Project: {project}", Repo, ProjectUrl);
    }
}