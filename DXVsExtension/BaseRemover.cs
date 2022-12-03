using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVsExtension {
    internal class BaseRemover {
        public static void DeleteDb(string dbName, AsyncPackage package) {
            DXVsExtensionPackage options = package as DXVsExtensionPackage;
            var deleteProcessPath = options.DeleteProgramFilePath;
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = deleteProcessPath;
            proc.StartInfo.Arguments = "-" + dbName;
            proc.Start();
        }
    }
}
