using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace DXVSExtension {
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class DeleteBaseCommand {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("f232d43b-f5f2-4501-8213-74a88da4930a");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteBaseCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private DeleteBaseCommand(AsyncPackage package, OleMenuCommandService commandService) {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static DeleteBaseCommand Instance {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider {
            get {
                return this.package;
            }
        }



        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package) {
            // Switch to the main thread - the call to AddCommand in DeleteBaseCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new DeleteBaseCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void Execute(object sender, EventArgs e) {
            DTE dte = await package.GetServiceAsync(typeof(DTE)).ConfigureAwait(false) as DTE;
            var slnName = dte.Solution.FullName;
            var solutionFolderName = Path.GetDirectoryName(slnName);
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
                        DeleteDb(dbName);
                        return;
                    } else {
                        VsShellUtilities.ShowMessageBox(this.package,
                            "No database was found",
                            "Not found",
                            OLEMSGICON.OLEMSGICON_INFO,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                }
            }
        }

        public void DeleteDb(string dbName) {
            DeleteBaseCommandPackage options = package as DeleteBaseCommandPackage;
            var deleteProcessPath = options.DeleteProgramFilePath;
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = deleteProcessPath;
            proc.StartInfo.Arguments = "-" + dbName;
            proc.Start();
        }
    }
}
