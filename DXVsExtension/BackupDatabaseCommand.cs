using DXVSExtension;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace DXVsExtension {
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class BackupDatabaseCommand {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4129;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("9d1275d5-18bd-4ab6-bbcf-8c63b58333f9");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupDatabaseCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private BackupDatabaseCommand(AsyncPackage package, OleMenuCommandService commandService) {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static BackupDatabaseCommand Instance {
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
        public static async Task InitializeAsync(AsyncPackage package, string _solutionFullName) {
            // Switch to the main thread - the call to AddCommand in BackupDatabaseCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new BackupDatabaseCommand(package, commandService);
            Instance.solutionFullName = _solutionFullName;
        }
        string solutionFullName;
        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e) {
            var solutionData = new SolutionDataProvider();
            solutionData.GetDataFromSolution(solutionFullName);
            if(solutionData.DatabaseName != null) {
                backupDB(solutionData.DatabaseName, solutionData.SoluitonParentFolderName);
            } else {
                VsShellUtilities.ShowMessageBox(this.package,
                    "No database was found",
                    "Not found",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
        void backupDB(string dbName, string backupPath) {
            DXVsExtensionPackage options = package as DXVsExtensionPackage;
            var deleteProcessPath = options.BackupDBFilePath;
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = deleteProcessPath;
            backupPath = "\"" + backupPath + "\"";
            proc.StartInfo.Arguments = string.Join(" ", new string[] { dbName, backupPath });
            proc.Start();
        }
    }

}
