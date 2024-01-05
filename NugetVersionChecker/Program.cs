using System.Xml.Linq;
using Cocona;
using NugetVersionChecker;

CoconaApp.Run(([Option('p', Description = "Project file name, e.g. /path/to/project/myproject.csproj")][PathExists] string project) =>
{
    var projectFile = XDocument.Load(project);
});
