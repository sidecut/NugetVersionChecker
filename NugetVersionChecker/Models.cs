namespace NugetVersionChecker;

public class Models
{
    public record PackageReference(string Name, string? Version);

    public record PackageReferenceDifference(string Name, string? ProjectFileVersion, string? PackagesConfigVersion);
}
