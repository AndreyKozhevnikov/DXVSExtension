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
        public string DatabaseName { get; set; }
        public string SoluitonParentFolderName { get; set; }
        public void GetDataFromSolution(string solutionFullName) {
            var solutionFolderName = Path.GetDirectoryName(solutionFullName);
            SoluitonParentFolderName = Directory.GetParent(solutionFolderName).FullName;
            List<string> configFiles = new List<string>();
            configFiles.AddRange(Directory.GetFiles(solutionFolderName, "app.config", SearchOption.AllDirectories));
            configFiles.AddRange(Directory.GetFiles(solutionFolderName, "web.config", SearchOption.AllDirectories));
            // var dbNamePattern = @"<add name=""ConnectionString"" connectionString=""Integrated Security=SSPI;Pooling=false;Data Source=\(localdb\)\\mssqllocaldb;Initial Catalog=(?<dbname>.*)""";
            foreach(var confFile in configFiles) {
                using(var sw = new StreamReader(confFile)) {
                    var xDocument = XDocument.Load(confFile);
                    DatabaseName = GetDBName(xDocument);
                    if(!string.IsNullOrEmpty(DatabaseName)) {
                        break;
                    }
                }
            }
        }

        internal string GetDBName(XDocument xDocument) {
            var el = xDocument.Root;
            var el2 = xDocument.Root.Elements();
            var configNode = xDocument.Root.Elements().Where(x => x.Name.LocalName == "connectionStrings").FirstOrDefault();
            if(configNode == null)
                return null;
            var configs = configNode.Elements();
            var nameXName = XName.Get("name", configNode.Name.Namespace.NamespaceName);
            var realConfig = configs.Where(x => x.Attribute(nameXName).Value == "ConnectionString").FirstOrDefault();

            if(realConfig != null) {
                return GetDbNameFromConnectionStringLine(realConfig, configNode.Name.Namespace.NamespaceName);
            } else {
                if(configs.Count() == 1) {
                    var possibleConfig = configs.First();
                    return GetDbNameFromConnectionStringLine(possibleConfig, configNode.Name.Namespace.NamespaceName);
                }
                return null;
            }
        }

        public string GetDbNameFromConnectionStringLine(XElement connectionStringLine, string nameSpace) {
            var nameXConnectionString = XName.Get("connectionString", nameSpace);
            var connectionString = connectionStringLine.Attribute(nameXConnectionString).Value;
            var dbNamePattern = @"Initial Catalog=(?<dbname>.*)";
            var dbNameRegex = new Regex(dbNamePattern,RegexOptions.IgnoreCase);
            Match dbNameMatch = dbNameRegex.Match(connectionString);
            var dbName = dbNameMatch.Groups["dbname"].Value;
            return dbName;

        }
    }
}
