using System.ComponentModel;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Cocona;

CoconaApp.Run(([Option('p', Description = "Project file name, e.g. /path/to/project/myproject.csproj")] string project) =>
{
    var projectFile = XDocument.Load(project);
});
