﻿using System.Text.Json;
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
    [Option('j', Description = "JSON file to output results to, or - for stdout and . for using same name as project file")] string? jsonfile,
    [Option('c', Description = "CSV file to output results to, or - for stdout and . for using same name as project file")] string? csvfile
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

        logger.LogTrace("  {name} {version}", name, version);
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
            var targetFramework = packageElement.Attribute("targetFramework")?.Value;
            logger.LogTrace("  {id} {version} {targetFramework}", id, version, targetFramework);
            packageConfigFileReferences.Add(new Models.PackageReference(id!, version, targetFramework));
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
            differences.Add(new Models.PackageReferenceDifference(projectFilePackageReference.Name, projectFilePackageReference.Version,
                 packageConfigFilePackageReference.Version, packageConfigFilePackageReference.TargetFramework));
        }
    }

    // Write number of differences
    logger.LogInformation("Found {differences} differences", differences.Count);

    var sortedDifferences = differences.OrderBy(x => x.Name).ThenBy(x => x.ProjectFileVersion).ThenBy(x => x.PackagesConfigVersion).ThenBy(x => x.TargetFramework);

    // Print sorted differences to CSV file `csvfile`
    if (csvfile is not null)
    {
        // If filename is `.`, make it base filename plus .csv
        if (csvfile == ".")
        {
            csvfile = Path.GetFileNameWithoutExtension(project) + ".csv";
        }
        else if (csvfile != "-" && !csvfile.EndsWith(".csv"))
        {
            // If no CSV extension, add it
            csvfile += ".csv";
        }

        logger.LogInformation("Writing CSV to {csvfile}", csvfile == "-" ? "console:" : csvfile);

        using var writer = csvfile == "-" ? Console.Out : new StreamWriter(csvfile);
        writer.WriteLine("Name,ProjectFileVersion,PackagesConfigVersion,TargetFramework");
        foreach (var difference in sortedDifferences)
        {
            writer.WriteLine($"{difference.Name},{difference.ProjectFileVersion},{difference.PackagesConfigVersion},{difference.TargetFramework}");
        }
    }

    // Print sorted differences to JSON file `jsonfile`
    if (jsonfile is not null)
    {
        // If filename is `.`, make it base filename plus .json
        if (jsonfile == ".")
        {
            jsonfile = Path.GetFileNameWithoutExtension(project) + ".json";
        }
        else if (jsonfile != "-" && !jsonfile.EndsWith(".json"))
        {
            // if no JSON extension, add it
            jsonfile += ".json";
        }

        logger.LogInformation("Writing JSON to {jsonfile}", jsonfile == "-" ? "console:" : jsonfile);
        using var writer = jsonfile == "-" ? Console.Out : new StreamWriter(jsonfile);
        writer.WriteLine(JsonSerializer.Serialize(sortedDifferences, jsonIndentOptions));
    }

    // If neither jsonfile nor csvfile is specified, print to console
    if (jsonfile is null && csvfile is null)
    {
        logger.LogInformation("Writing JSON to console");
        Console.WriteLine(JsonSerializer.Serialize(sortedDifferences, jsonIndentOptions));
    }
});

app.Run();
