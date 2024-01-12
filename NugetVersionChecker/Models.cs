namespace NugetVersionChecker;

public class Models
{
    public record PackageReference(string Name, string? Version, string? TargetFramework = null);

    public record PackageReferenceDifference(string Name, string? ProjectFileVersion, string? PackagesConfigVersion, string? TargetFramework);
}
