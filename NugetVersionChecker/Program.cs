using System.Xml.Linq;
using Cocona;
using Microsoft.Extensions.Logging;
using NugetVersionChecker;

var builder = CoconaApp.CreateBuilder();
builder.Logging.AddDebug();

var app = builder.Build();
app.AddCommand((ILogger<Program> logger, [Option('p', Description = "Project file name, e.g. /path/to/project/myproject.csproj")][PathExists] string project) =>
{
    logger.LogInformation("Analyzing project file {project}:", project);
    var projectFile = XDocument.Load(project);
});

app.Run();
