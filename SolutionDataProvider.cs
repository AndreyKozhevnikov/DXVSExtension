using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DXVSExtension {
    public class SolutionDataProvider {
        public string DatabaseName{ get; set; }
        public SolutionDataProvider(string solutionFullName) {
            var solutionFolderName = Path.GetDirectoryName(solutionFullName);
            List<string> configFiles = new List<string>();
            configFiles.AddRange(Directory.GetFiles(solutionFolderName, "app.config", SearchOption.AllDirectories));
            configFiles.AddRange(Directory.GetFiles(solutionFolderName, "web.config", SearchOption.AllDirectories));
            // var dbNamePattern = @"<add name=""ConnectionString"" connectionString=""Integrated Security=SSPI;Pooling=false;Data Source=\(localdb\)\\mssqllocaldb;Initial Catalog=(?<dbname>.*)""";
            foreach(var confFile in configFiles) {
                using(var sw = new StreamReader(confFile)) {
                    var xDocument = XDocument.Load(confFile);
                    var el = xDocument.Root;
                    var el2 = xDocument.Root.Elements();
                    var configNode = xDocument.Root.Elements().Where(x => x.Name.LocalName == "connectionStrings").First();
                    var configs = configNode.Elements();
                    var nameXName = XName.Get("name", configNode.Name.Namespace.NamespaceName);

                    var realConfig = configs.Where(x => x.Attribute(nameXName).Value == "ConnectionString").FirstOrDefault();

                    if(realConfig != null) {
                        var nameXConnectionString = XName.Get("connectionString", configNode.Name.Namespace.NamespaceName);
                        var connectionString = realConfig.Attribute(nameXConnectionString).Value;
                        var dbNamePattern = @"Initial Catalog=(?<dbname>.*)";
                        var dbNameRegex = new Regex(dbNamePattern);
                        Match dbNameMatch = dbNameRegex.Match(connectionString);
                        var dbName = dbNameMatch.Groups["dbname"].Value;
                        DatabaseName = dbName;
                        return;
                    }
                }
            }
        }
    } 
}
