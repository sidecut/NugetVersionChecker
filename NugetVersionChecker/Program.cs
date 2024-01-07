using System.Text.Json;
using System.Xml.Linq;
using Cocona;
using Microsoft.Extensions.Logging;
using NugetVersionChecker;

var builder = CoconaApp.CreateBuilder();
builder.Logging.AddDebug();

var jsonIndentOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    // PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    // PropertyNameCaseInsensitive = true,
};

var app = builder.Build();
app.AddCommand((ILogger<Program> logger,
    [Option('p', Description = "Project file name, e.g. /path/to/project/myproject.csproj")]
    [PathExists] string project,
    [Option('j', Description = "JSON file to output results to")] string? jsonfile,
    [Option('c', Description = "CSV file to output results to")] string? csvfile
    ) =>
{
    var projectFilePackageReferences = new List<Models.PackageReference>();
    var packageConfigFileReferences = new List<Models.PackageReference>();

    var msbuild2003 = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");

    logger.LogInformation("Analyzing project file {project}:", project);
    var projectFile = XDocument.Load(project);

    //
    // Detect package references in project file
    //
    var references = projectFile.Descendants(msbuild2003 + "Reference");
    foreach (var packageReference in references)
    {
        var Include = packageReference.Attribute("Include")?.Value;
        var parts = Include?.Split(',');
        var name = parts?.First();
        var pairs = from part in parts?.Skip(1)
                    let kv = part.Split('=').Select(x => x.Trim()).ToArray()
                    select new KeyValuePair<string, string>(kv[0], kv[1]);
        var version = pairs.FirstOrDefault(x => x.Key.Equals("Version", StringComparison.CurrentCultureIgnoreCase)).Value;

        logger.LogDebug("  {name} {version}", name, version);
        projectFilePackageReferences.Add(new Models.PackageReference(name!, version));
    }
    logger.LogDebug("{projectFilePackageReferences}", JsonSerializer.Serialize(
        projectFilePackageReferences.Where(x => x.Version is not null).OrderBy(x => x.Name).ThenBy(x => x.Version), jsonIndentOptions));

    //
    // Detect package references in packages.config
    //
    var packagesConfig = Path.Combine(Path.GetDirectoryName(project)!, "packages.config");
    if (File.Exists(packagesConfig))
    {
        logger.LogInformation("Analyzing packages.config file {packagesConfig}:", packagesConfig);
        var packages = XDocument.Load(packagesConfig);
        var packageElements = packages.Descendants("package");
        foreach (var packageElement in packageElements)
        {
            var id = packageElement.Attribute("id")?.Value;
            var version = packageElement.Attribute("version")?.Value;
            logger.LogDebug("  {id} {version}", id, version);
            packageConfigFileReferences.Add(new Models.PackageReference(id!, version));
        }
        // Console.WriteLine(JsonSerializer.Serialize(packageReferences, jsonIndentOptions));
        logger.LogDebug("{packageConfigFileReferences}", JsonSerializer.Serialize(
            packageConfigFileReferences.Where(x => x.Version is not null).OrderBy(x => x.Name).ThenBy(x => x.Version), jsonIndentOptions));
    }

    //
    // Detect differences between project file and packages.config
    //
    var differences = new List<Models.PackageReferenceDifference>();
    foreach (var projectFilePackageReference in projectFilePackageReferences)
    {
        var packageConfigFilePackageReference = packageConfigFileReferences.FirstOrDefault(x => x.Name.Equals(projectFilePackageReference.Name, StringComparison.CurrentCultureIgnoreCase));
        if (packageConfigFilePackageReference is null)
        {
            // Add this difference if the package is not in packages.config
            // differences.Add(new Models.PackageReferenceDifference(projectFilePackageReference.Name, projectFilePackageReference.Version, null));
        }
        else if (projectFilePackageReference.Version != packageConfigFilePackageReference.Version)
        {
            // Add this difference if the package is in packages.config but the version is different
            differences.Add(new Models.PackageReferenceDifference(projectFilePackageReference.Name, projectFilePackageReference.Version, packageConfigFilePackageReference.Version));
        }
    }

    // Print differences
    if (csvfile is not null)
    {
        // Print CSV to file `csvfile`
        using var writer = csvfile == "-" ? Console.Out : new StreamWriter(csvfile);
        writer.WriteLine("Name,ProjectFileVersion,PackagesConfigVersion");
        foreach (var difference in differences)
        {
            writer.WriteLine($"{difference.Name},{difference.ProjectFileVersion},{difference.PackagesConfigVersion}");
        }
    }

    // print differences to JSON file `jsonfile`
    if (jsonfile is not null)
    {
        using var writer = jsonfile == "-" ? Console.Out : new StreamWriter(jsonfile);
        writer.WriteLine(JsonSerializer.Serialize(differences, jsonIndentOptions));
    }

    // If neither jsonfile nor csvfile is specified, print to console
    if (jsonfile is null && csvfile is null)
    {
        Console.WriteLine(JsonSerializer.Serialize(differences, jsonIndentOptions));
    }
});

app.Run();
