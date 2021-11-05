using GemstoneCommon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adapt.DataSources
{
    /// <summary>
    /// The Settings for a <see cref="JsisCsvImport"/> Data Source
    /// </summary>
    public class JsisCsvSettings
    {
        [DefaultValue("C:\\Users\\wang690\\Desktop\\Projects\\ArchiveWalker\\DataJSISsubset\\DataJSIS")]
        [CustomConfigurationEditor("GemstoneWPF.dll", "GemstoneWPF.Editors.FolderBrowser", "showNewFolderButton=true; description=Select Root Folder")]
        public string RootFolder { get; set; }
    }
}
