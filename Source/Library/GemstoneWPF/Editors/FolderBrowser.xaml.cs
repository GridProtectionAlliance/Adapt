//******************************************************************************************************
//  FolderBrowser.xaml.cs - Gbtc
//
//  Copyright © 2021, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  08/01/2013 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using Gemstone;
using Gemstone.StringExtensions;

namespace GemstoneWPF.Editors
{
    /// <summary>
    /// Interaction logic for FolderBrowser.xaml
    /// </summary>
    public partial class FolderBrowser : UserControl
    {
        #region [ Members ]

        // Fields
        private object m_currentValue;
        private string m_parameterName;
        private string m_connectionString;
        private Action<object> m_setValue;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="FolderBrowserEditor"/> class.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to be configured.</param>
        /// <param name="currentValue"> The current Value of the parameter.</param>
        /// <param name="completeAction"> The <see cref="Action{object}"/> to be called with the new Parameter.</param>
        public FolderBrowser(string parameterName, object currentValue, Action<object> completeAction)
            : this(parameterName, currentValue, completeAction, null)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FolderBrowserEditor"/> class.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to be configured.</param>
        /// <param name="currentValue"> The current Value of the parameter.</param>
        /// <param name="completeAction"> The <see cref="Action{object}"/> to be called with the new Parameter.</param>
        /// <param name="connectionString">Parameters for the folder browser.</param>
        public FolderBrowser(string parameterName, object currentValue, Action<object> completeAction, string connectionString)
        {
            InitializeComponent();
            m_setValue = completeAction;
            m_parameterName = parameterName;
            m_connectionString = connectionString;
            m_currentValue = currentValue;
        }

        #endregion

        #region [ Methods ]

        private void FolderBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser;

            Dictionary<string, string> settings;
            string setting;

            // Set up folder browser
            folderBrowser = new FolderBrowserDialog();
            folderBrowser.SelectedPath = m_currentValue?.ToString() ?? "";

            // Parse folder browser parameters if they have been defined
            if ((object)m_connectionString != null)
            {
                settings = m_connectionString.ParseKeyValuePairs();

                if (settings.TryGetValue("description", out setting))
                    folderBrowser.Description = setting;

                if (settings.TryGetValue("showNewFolderButton", out setting))
                    folderBrowser.ShowNewFolderButton = setting.ParseBoolean();
            }

            // Show the browser and update the parameter
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                m_setValue.Invoke(folderBrowser.SelectedPath);
          
        }

        #endregion
    }
}
