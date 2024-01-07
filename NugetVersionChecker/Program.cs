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
    [PathExists] string project) =>
{
    var packageReferences = new List<Models.PackageReference>();

    var msbuild2003 = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");

    logger.LogInformation("Analyzing project file {project}:", project);
    var projectFile = XDocument.Load(project);

    // // Detect SDK style project file
    // var packages = projectFile.Descendants("PackageReference");
    // foreach (var package in packages)
    // {
    //     var name = package.Attribute("Include")?.Value;
    //     var version = package.Attribute("Version")?.Value;
    //     logger.LogInformation("  {name} {version}", name, version);
    // }

    // // Detect legacy project file
    // var packagesConfig = projectFile.Descendants("Package");
    // foreach (var package in packagesConfig)
    // {
    //     var name = package.Attribute("id")?.Value;
    //     var version = package.Attribute("version")?.Value;
    //     logger.LogInformation("  {name} {version}", name, version);
    // }

    // Detect package references in project file
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
        packageReferences.Add(new Models.PackageReference(name!, version));
    }
    // Console.WriteLine(JsonSerializer.Serialize(packageReferences, jsonIndentOptions));
    Console.WriteLine(JsonSerializer.Serialize(
        packageReferences.Where(x => x.Version is not null).OrderBy(x => x.Name).ThenBy(x => x.Version), jsonIndentOptions));
});

app.Run();
