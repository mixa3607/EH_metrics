public interface IAppVersionInfoService
{
    string? BuildDate { get; }
    string? GitRefType { get; }
    string? GitRef { get; }
    string? GitCommitSha { get; }

    public string? Repo { get; }
    public string? RepoUrl { get; }
    public string? ProjectUrl { get; }

    void LogInfo();
}