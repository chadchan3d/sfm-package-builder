using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Persistence;

public sealed class ProjectLoadResult
{
    private ProjectLoadResult(ProjectLoadStatus status, PackageProject? project, string? message)
    {
        Status = status;
        Project = project;
        Message = message;
    }

    public ProjectLoadStatus Status { get; }

    public PackageProject? Project { get; }

    public string? Message { get; }

    public bool IsSuccess => Status == ProjectLoadStatus.Success;

    public static ProjectLoadResult Success(PackageProject project) =>
        new(ProjectLoadStatus.Success, project, null);

    public static ProjectLoadResult Failure(ProjectLoadStatus status, string message) =>
        new(status, null, message);
}
