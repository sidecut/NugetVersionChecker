namespace NugetVersionChecker;

public class Models
{
    public record PackageReference(string Name, string? Version);
}
